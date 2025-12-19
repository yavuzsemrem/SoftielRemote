using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using Serilog;
using SoftielRemote.Backend.Data;
using SoftielRemote.Backend.Hubs;
using SoftielRemote.Backend.Repositories;
using SoftielRemote.Backend.Services;
using StackExchange.Redis;
using System.Linq;
using System.Text;
using System.Threading.RateLimiting;

// Serilog yapılandırması
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/softielremote-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Serilog'u kullan
builder.Host.UseSerilog();

// Port yapılandırması (Azure App Service otomatik PORT environment variable'ı set eder)
// Azure'da PORT genellikle 8080, local'de 5000 kullanılır
var port = Environment.GetEnvironmentVariable("PORT") ?? builder.Configuration["Port"] ?? "5000";
// ASPNETCORE_URLS environment variable varsa onu kullan, yoksa appsettings.json'dan, yoksa otomatik port ile
var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") 
    ?? builder.Configuration["Urls"] 
    ?? $"http://0.0.0.0:{port}";
builder.WebHost.UseUrls(urls);

// PostgreSQL DbContext yapılandırması (opsiyonel - InMemory fallback var)
var postgresConnectionString = builder.Configuration.GetConnectionString("PostgreSQLConnection");
var usePostgreSQL = !string.IsNullOrEmpty(postgresConnectionString);

// PostgreSQL bağlantısını test et
if (usePostgreSQL)
{
    try
    {
        var connectionBuilder = new Npgsql.NpgsqlConnectionStringBuilder(postgresConnectionString);
    var originalHost = connectionBuilder.Host;
    var originalPort = connectionBuilder.Port;
    
    // SSL ayarlarını açıkça belirt (Supabase için gerekli)
    connectionBuilder.SslMode = Npgsql.SslMode.Require;
    connectionBuilder.Timeout = 60;
    connectionBuilder.CommandTimeout = 120;
    
    Log.Information("PostgreSQL bağlantısı test ediliyor... Host={Host}, Port={Port}", originalHost, originalPort);
    
    // IPv6 adresini manuel olarak çözümle (Windows'ta DNS çözümleme sorunu olabilir)
    string? resolvedIpv6Address = null;
    if (!string.IsNullOrWhiteSpace(originalHost))
    {
        try
        {
            // Önce GetHostAddresses ile dene
            var hostAddresses = System.Net.Dns.GetHostAddresses(originalHost);
            var ipv6Address = hostAddresses.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);
            if (ipv6Address != null)
            {
                resolvedIpv6Address = $"[{ipv6Address}]"; // IPv6 adresleri köşeli parantez içinde olmalı
                Log.Information("IPv6 adresi çözümlendi: {Host} -> {IpAddress}", originalHost, ipv6Address);
            }
        }
        catch (Exception dnsEx1)
        {
            // GetHostAddresses başarısız olursa GetHostEntry ile dene
            try
            {
                var hostEntry = System.Net.Dns.GetHostEntry(originalHost);
                var ipv6Address = hostEntry.AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);
                if (ipv6Address != null)
                {
                    resolvedIpv6Address = $"[{ipv6Address}]";
                    Log.Information("IPv6 adresi GetHostEntry ile çözümlendi: {Host} -> {IpAddress}", originalHost, ipv6Address);
                }
            }
            catch (Exception dnsEx2)
            {
                Log.Warning("IPv6 adresi çözümlenemedi (GetHostAddresses: {Ex1}, GetHostEntry: {Ex2}). Hostname ile devam edilecek.", 
                    dnsEx1.Message, dnsEx2.Message);
            }
        }
    }
    
    // Önce mevcut port ile dene, başarısız olursa alternatif port'u dene
    var portsToTry = new[] { originalPort, 5432, 6543 }.Distinct().ToArray();
    bool connectionSuccessful = false;
    
    foreach (var portValue in portsToTry)
    {
        if (connectionSuccessful) break;
        
        var currentPort = portValue; // Closure sorununu önlemek için yerel değişkene kopyala
        
        // Önce IPv6 adresi ile dene (eğer varsa), başarısız olursa hostname ile dene
        var hostsToTry = new List<string>();
        if (!string.IsNullOrEmpty(resolvedIpv6Address))
        {
            hostsToTry.Add(resolvedIpv6Address);
            Log.Information("Port {Port} ile IPv6 adresi [{Ipv6}] deneniyor...", currentPort, resolvedIpv6Address.Trim('[', ']'));
        }
        if (!string.IsNullOrWhiteSpace(originalHost))
        {
            hostsToTry.Add(originalHost); // Hostname'i fallback olarak ekle
        }
        
        bool portConnectionSuccessful = false;
        foreach (var hostToTry in hostsToTry)
        {
            if (portConnectionSuccessful) break;
            
            // hostDisplay'i try bloğunun dışında tanımla (catch bloklarında kullanılacak)
            var hostDisplay = hostToTry.StartsWith("[") ? $"IPv6 {hostToTry}" : $"hostname {hostToTry}";
            
            try
            {
                connectionBuilder.Host = hostToTry;
                connectionBuilder.Port = currentPort;
                Log.Information("Port {Port} ile {HostDisplay} deneniyor...", currentPort, hostDisplay);
                
                using var testConnection = new Npgsql.NpgsqlConnection(connectionBuilder.ConnectionString);
                
                // Bağlantıyı aç (timeout ile)
                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                testConnection.OpenAsync(cancellationTokenSource.Token).Wait(cancellationTokenSource.Token);
                
                // Bağlantı başarılı, test query çalıştır
                using var testCommand = new Npgsql.NpgsqlCommand("SELECT version();", testConnection);
                var version = testCommand.ExecuteScalarAsync().Result;
                Log.Information("✅ PostgreSQL bağlantısı başarılı! Host={Host}, Port={Port}, Version={Version}", hostDisplay, currentPort, version);
                
                testConnection.CloseAsync().Wait();
                
                // Başarılı connection string'i kaydet
                postgresConnectionString = connectionBuilder.ConnectionString;
                connectionSuccessful = true;
                portConnectionSuccessful = true;
                break;
            }
            catch (System.AggregateException aggEx)
            {
                var innerEx = aggEx.GetBaseException();
                var isNetworkError = false;
                string errorMessage = aggEx.Message;
                
                if (innerEx is System.Net.Sockets.SocketException socketEx)
                {
                    isNetworkError = socketEx.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound ||
                                    socketEx.SocketErrorCode == System.Net.Sockets.SocketError.NoData ||
                                    socketEx.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut;
                    errorMessage = socketEx.Message;
                }
                else if (innerEx is System.AggregateException nestedAggEx)
                {
                    var nestedInner = nestedAggEx.GetBaseException();
                    if (nestedInner is System.Net.Sockets.SocketException nestedSocketEx)
                    {
                        isNetworkError = nestedSocketEx.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound ||
                                        nestedSocketEx.SocketErrorCode == System.Net.Sockets.SocketError.NoData ||
                                        nestedSocketEx.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut;
                        errorMessage = nestedSocketEx.Message;
                    }
                }
                
                if (isNetworkError && hostToTry != hostsToTry.Last())
                {
                    Log.Information("❌ {HostDisplay} ile bağlantı başarısız: {Message}. Bir sonraki host deneniyor...", hostDisplay, errorMessage);
                    continue; // Bir sonraki host'u dene
                }
                else if (isNetworkError)
                {
                    // Son host ve network hatası - port döngüsüne devam et
                    Log.Information("❌ {HostDisplay} ile bağlantı başarısız: {Message}. Bir sonraki port deneniyor...", hostDisplay, errorMessage);
                    break; // Port döngüsüne devam et
                }
                else
                {
                    // Network hatası değil (örn. authentication) - port döngüsüne devam et
                    Log.Warning("❌ {HostDisplay} ile bağlantı başarısız: {Message}. Bir sonraki port deneniyor...", hostDisplay, errorMessage);
                    break;
                }
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                var isNetworkError = ex.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound ||
                                    ex.SocketErrorCode == System.Net.Sockets.SocketError.NoData ||
                                    ex.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut;
                
                if (isNetworkError && hostToTry != hostsToTry.Last())
                {
                    Log.Information("❌ {HostDisplay} ile bağlantı başarısız: {Message}. Bir sonraki host deneniyor...", hostDisplay, ex.Message);
                    continue; // Bir sonraki host'u dene
                }
                else if (isNetworkError)
                {
                    Log.Information("❌ {HostDisplay} ile bağlantı başarısız: {Message}. Bir sonraki port deneniyor...", hostDisplay, ex.Message);
                    break; // Port döngüsüne devam et
                }
                else
                {
                    Log.Warning("❌ {HostDisplay} ile bağlantı başarısız: {Message}. Bir sonraki port deneniyor...", hostDisplay, ex.Message);
                    break;
                }
            }
            catch (Exception ex) when (ex is not System.Net.Sockets.SocketException && ex is not System.AggregateException)
            {
                Log.Warning("❌ {HostDisplay} ile bağlantı başarısız: {Message}. Bir sonraki port deneniyor...", hostDisplay, ex.Message);
                break; // Port döngüsüne devam et
            }
        }
        
        if (connectionSuccessful) break;
    }
    
    if (!connectionSuccessful)
    {
        throw new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.HostNotFound);
    }
    }
    catch (System.AggregateException aggEx)
    {
        var innerEx = aggEx.GetBaseException();
        var socketEx = innerEx as System.Net.Sockets.SocketException;
        var errorMessage = socketEx?.Message ?? aggEx.Message;
        
        Log.Warning(aggEx, "PostgreSQL bağlantısı başarısız (Network hatası): {Message}. InMemory repository kullanılacak", errorMessage);
        Log.Warning("Tüm portlar ve host'lar denenmiş. Olası nedenler: Internet bağlantısı sorunu, firewall engellemesi, IPv6 bağlantı sorunu.");
        Log.Warning("Supabase projenin aktif olduğunu ve connection string'in doğru olduğunu kontrol et.");
        usePostgreSQL = false;
    }
    catch (System.Net.Sockets.SocketException ex)
    {
        Log.Warning(ex, "PostgreSQL bağlantısı başarısız (Network hatası): {Message}. InMemory repository kullanılacak", ex.Message);
        Log.Warning("Tüm portlar ve host'lar denenmiş. Olası nedenler: Internet bağlantısı sorunu, firewall engellemesi, IPv6 bağlantı sorunu.");
        Log.Warning("Supabase projenin aktif olduğunu ve connection string'in doğru olduğunu kontrol et.");
        usePostgreSQL = false;
    }
    catch (Npgsql.NpgsqlException ex)
    {
        Log.Warning(ex, "PostgreSQL bağlantısı başarısız (PostgreSQL hatası): {Message}. InMemory repository kullanılacak", ex.Message);
        Log.Warning("Şifre veya SSL ayarlarını kontrol et.");
        usePostgreSQL = false;
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "PostgreSQL bağlantısı başarısız: {Message}. InMemory repository kullanılacak", ex.Message);
        usePostgreSQL = false;
    }
}

if (usePostgreSQL)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseNpgsql(postgresConnectionString, npgsqlOptions =>
        {
            // Retry strategy: Transient hatalarda otomatik retry (3 retry, Supabase pooler için)
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(2),
                errorCodesToAdd: null);
            
            // Command timeout'u artır (120 saniye - Supabase pooler ve network latency için)
            npgsqlOptions.CommandTimeout(120);
        });
        
        // Connection pooling ayarları
        options.EnableSensitiveDataLogging(false);
        options.EnableServiceProviderCaching();
        
        // Connection pooling optimize et - Supabase pooler için
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
    });
}
else
{
    // InMemory database kullan (DbContext hala gerekli olabilir)
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("SoftielRemoteInMemory"));
    Log.Information("InMemory database kullanılıyor");
}

// Redis yapılandırması (opsiyonel - Redis yoksa uygulama çalışmaya devam eder)
IConnectionMultiplexer? redisConnection = null;
var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    try
    {
        // Redis bağlantısını opsiyonel yap: abortConnect=false ile Redis yoksa exception fırlatma
        var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);
        configurationOptions.AbortOnConnectFail = false; // Redis yoksa exception fırlatma, arka planda retry yap
        configurationOptions.ConnectRetry = 2; // 2 kez deneme (daha hızlı)
        configurationOptions.ConnectTimeout = 2000; // 2 saniye timeout (daha hızlı)
        configurationOptions.AsyncTimeout = 2000; // Async işlemler için timeout
        configurationOptions.SyncTimeout = 2000; // Sync işlemler için timeout
        configurationOptions.AllowAdmin = false; // Admin komutlarına izin verme
        
        // Bağlantıyı kur (test komutu göndermeden, sadece bağlantıyı hazırla)
        // AbortOnConnectFail=false olduğu için Redis yoksa exception fırlatmaz
        redisConnection = ConnectionMultiplexer.Connect(configurationOptions);
        
        // Bağlantıyı servislere kaydet (Redis yoksa RedisStateService fallback kullanır)
        builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
        });
        
        // Redis bağlantısı kuruldu (gerçek bağlantı testi RedisStateService'de yapılacak)
        Log.Information("Redis bağlantısı yapılandırıldı (lazy connection - gerçek bağlantı ilk kullanımda test edilecek)");
    }
    catch (Exception ex)
    {
        // Redis bağlantısı kurulamadı, ama uygulama çalışmaya devam edecek
        Log.Warning(ex, "Redis bağlantısı kurulamadı, distributed cache kullanılmayacak (PostgreSQL fallback kullanılacak)");
        redisConnection?.Dispose();
        redisConnection = null;
    }
}
else
{
    Log.Information("Redis connection string bulunamadı, distributed cache kullanılmayacak");
}

// Redis State Service (Redis yoksa null olarak kaydedilir, fallback PostgreSQL kullanılır)
builder.Services.AddSingleton<IRedisStateService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RedisStateService>>();
    var redis = sp.GetService<IConnectionMultiplexer>();
    return new RedisStateService(redis, logger);
});

// JWT Authentication yapılandırması
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] 
    ?? builder.Configuration["Jwt:Key"]
    ?? "YourSecretKeyHere-MustBeAtLeast32CharactersLongForHS256";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SoftielRemote";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SoftielRemote";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
    };
    
    // SignalR WebSocket bağlantıları için JWT token'ı query string'den de oku
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            // SignalR hub path'i için token'ı query string'den al
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            
            return Task.CompletedTask;
        }
    };
});

// Add services to the container.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Model validation hatalarını otomatik olarak 400 Bad Request olarak döndür
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => new
                {
                    Field = x.Key,
                    Message = e.ErrorMessage
                }))
                .ToList();

            // Validation hatalarını logla
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("❌ Model validation hatası: Endpoint={Endpoint}, Method={Method}, Errors={Errors}",
                context.HttpContext.Request.Path,
                context.HttpContext.Request.Method,
                string.Join(", ", errors.Select(e => $"{e.Field}: {e.Message}")));

            return new BadRequestObjectResult(new
            {
                Success = false,
                ErrorMessage = "Validation failed",
                Errors = errors
            });
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "SoftielRemote API", 
        Version = "v1",
        Description = "SoftielRemote - Uzaktan Erişim (AnyDesk / TeamViewer Alternatifi) Backend API",
        Contact = new OpenApiContact
        {
            Name = "SoftielRemote",
            Email = "support@softielremote.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT License"
        }
    });
    
    // XML comments dosyasını ekle (eğer varsa)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
    
    // JWT için Swagger yapılandırması
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    
    // Enum'ları string olarak göster
    c.UseInlineDefinitionsForEnums();
});

// SignalR ekle - CORS ve authentication ayarları ile
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true; // Development için detaylı hata mesajları
});

// Health Checks
var healthChecksBuilder = builder.Services.AddHealthChecks();
if (usePostgreSQL)
{
    healthChecksBuilder.AddDbContextCheck<ApplicationDbContext>(name: "postgresql");
}
// Redis health check kaldırıldı - Redis opsiyonel olduğu için health check'i etkilememeli
// Redis yoksa veya timeout olursa uygulama çalışmaya devam eder

// CORS ekle (Agent ve App'in Backend'e bağlanabilmesi için)
// SignalR WebSocket bağlantıları için AllowCredentials() kullanılamaz, AllowAnyOrigin() kullanılmalı
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Dependency Injection - PostgreSQL veya InMemory repository seçimi
if (usePostgreSQL)
{
    builder.Services.AddScoped<IAgentRepository, PostgreSqlAgentRepository>();
    builder.Services.AddScoped<IConnectionRequestRepository, PostgreSqlConnectionRequestRepository>();
    Log.Information("PostgreSQL repository'leri kullanılıyor");
}
else
{
    builder.Services.AddSingleton<IAgentRepository, InMemoryAgentRepository>();
    builder.Services.AddSingleton<IConnectionRequestRepository, InMemoryConnectionRequestRepository>();
    Log.Information("InMemory repository'leri kullanılıyor");
}

builder.Services.AddScoped<IAgentService, AgentService>();

// Rate Limiting yapılandırması
builder.Services.AddRateLimiter(options =>
{
    // Global rate limiting policy (tüm endpoint'ler için)
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // IP bazlı rate limiting
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ipAddress,
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100, // 100 request per window
                Window = TimeSpan.FromMinutes(1) // 1 dakika
            });
    });

    // Endpoint bazlı rate limiting policies
    options.AddPolicy("AgentRegisterPolicy", context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"agent_register_{ipAddress}",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10, // 10 kayıt per window
                Window = TimeSpan.FromMinutes(1) // 1 dakika
            });
    });

    options.AddPolicy("ConnectionRequestPolicy", context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"connection_request_{ipAddress}",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5, // 5 bağlantı isteği per window
                Window = TimeSpan.FromMinutes(1) // 1 dakika
            });
    });

    options.AddPolicy("SignalRPolicy", context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"signalr_{ipAddress}",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 30, // 30 SignalR mesajı per window
                Window = TimeSpan.FromMinutes(1) // 1 dakika
            });
    });

    // Rate limit aşıldığında döndürülecek response
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync(
            "Rate limit exceeded. Please try again later.",
            cancellationToken);
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// Swagger'ı hem Development hem Production'da aç (Azure'da test için)
// Production'da güvenlik için kapatmak istersen, environment variable kullan: ENABLE_SWAGGER=false
var enableSwagger = Environment.GetEnvironmentVariable("ENABLE_SWAGGER") != "false";
if (app.Environment.IsDevelopment() || enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS redirection sadece HTTPS dinleniyorsa kullanılmalı
// Development modunda HTTP üzerinde çalıştığımız için devre dışı bırakıyoruz
// Production'da HTTPS kullanılıyorsa bu satırı aktif edin
// app.UseHttpsRedirection();

// CORS kullan
app.UseCors("AllowAll");

// Rate Limiting (Authentication'dan önce)
// Geçici olarak devre dışı (BadImageFormatException hatası nedeniyle)
// app.UseRateLimiter();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Controllers
app.MapControllers();

// SignalR Hub (rate limiting geçici olarak devre dışı)
app.MapHub<ConnectionHub>("/hubs/connection");
    // .RequireRateLimiting("SignalRPolicy");

// Health Checks
app.MapHealthChecks("/health");

// Database migration'ları otomatik çalıştır (sadece development ve PostgreSQL kullanılıyorsa)
// Migration'ları async olarak çalıştır ki Backend'in başlatılmasını engellemesin
if (app.Environment.IsDevelopment() && usePostgreSQL)
{
    _ = Task.Run(async () =>
    {
        try
        {
            await Task.Delay(2000); // Backend'in başlamasını bekle
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Migration'ı timeout ile çalıştır (120 saniye)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            await dbContext.Database.MigrateAsync(cts.Token);
            Log.Information("Database migrations applied successfully.");
        }
        catch (OperationCanceledException)
        {
            Log.Warning("Database migration timeout oldu. Migration'lar manuel olarak çalıştırılmalı.");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Database migration failed. Make sure PostgreSQL is running and connection string is correct.");
        }
    });
}

// Backend'i Supabase'e kaydet (farklı network'lerdeki Agent/App'lerin bulması için)
if (usePostgreSQL)
{
    _ = Task.Run(async () =>
    {
        try
        {
            await Task.Delay(2000); // Backend'in tamamen başlamasını bekle
            
            // Backend'in public URL'ini bul - otomatik tespit
            var publicUrl = Environment.GetEnvironmentVariable("SOFTIELREMOTE_BACKEND_PUBLIC_URL");
            // URL'in sonundaki '/' karakterini kaldır
            if (!string.IsNullOrWhiteSpace(publicUrl))
            {
                publicUrl = publicUrl.TrimEnd('/');
            }
            string? localIp = null;
            
            // Azure App Service'de çalışıyorsa, Azure URL'ini kullan
            var azureWebsiteNameForUrl = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
            if (string.IsNullOrWhiteSpace(publicUrl) && !string.IsNullOrWhiteSpace(azureWebsiteNameForUrl))
            {
                // Azure App Service URL formatı: https://{site-name}.azurewebsites.net
                publicUrl = $"https://{azureWebsiteNameForUrl}.azurewebsites.net";
                Log.Information("🔵 Azure App Service URL tespit edildi: {PublicUrl}", publicUrl);
            }
            
            // Local IP'yi bul (aynı network içinde kullanım için)
            localIp = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                .Where(ni => ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
                .Where(addr => addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .Where(addr => !addr.Address.ToString().StartsWith("169.254.")) // APIPA adresleri hariç
                .Select(addr => addr.Address.ToString())
                .FirstOrDefault();
            
            if (string.IsNullOrWhiteSpace(publicUrl))
            {
                // Azure App Service'de çalışıyorsa, Azure URL'ini kullan
                var azureWebsiteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
                var azureRegion = Environment.GetEnvironmentVariable("WEBSITE_RESOURCE_GROUP");
                if (!string.IsNullOrWhiteSpace(azureWebsiteName))
                {
                    // Azure App Service URL formatı: https://{site-name}.azurewebsites.net
                    // Region bilgisini de kontrol et
                    var azureUrl = $"https://{azureWebsiteName}.azurewebsites.net";
                    
                    // Azure URL'inin erişilebilir olup olmadığını test et
                    var isAccessible = await SoftielRemote.Core.Utils.BackendUrlAccessibilityTester.TestAccessibilityAsync(azureUrl, timeoutSeconds: 3);
                    if (isAccessible)
                    {
                        publicUrl = azureUrl;
                        Log.Information("✅ Backend Public URL Azure App Service'den tespit edildi: {PublicUrl}", publicUrl);
                    }
                    else
                    {
                        Log.Warning("⚠️ Azure URL tespit edildi ancak erişilebilir değil: {AzureUrl}", azureUrl);
                    }
                }
                
                // Azure URL bulunamadıysa, public IP'yi otomatik tespit etmeyi dene
                if (string.IsNullOrWhiteSpace(publicUrl))
                {
                    string? publicIp = null;
                    try
                    {
                        // Public IP'yi tespit etmek için bir servis kullan (ipify.org - ücretsiz)
                        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                        publicIp = await httpClient.GetStringAsync("https://api.ipify.org");
                        if (!string.IsNullOrWhiteSpace(publicIp))
                        {
                            publicUrl = $"http://{publicIp}:{port}";
                            
                            // Public URL'in gerçekten erişilebilir olup olmadığını test et
                            var isAccessible = await SoftielRemote.Core.Utils.BackendUrlAccessibilityTester.TestAccessibilityAsync(publicUrl, timeoutSeconds: 3);
                            if (!isAccessible)
                            {
                                Log.Warning("⚠️ Public URL tespit edildi ancak erişilebilir değil (port forwarding gerekebilir): {PublicUrl}", publicUrl);
                                // Public URL erişilebilir değilse, local IP kullan
                                publicUrl = null;
                            }
                            else
                            {
                                Log.Information("✅ Backend Public URL otomatik tespit edildi ve erişilebilir: {PublicUrl} (Public IP)", publicUrl);
                            }
                        }
                    }
                    catch (Exception ipEx)
                    {
                        Log.Warning(ipEx, "⚠️ Public IP tespit edilemedi, local IP kullanılacak");
                    }
                }
                
                // Public IP tespit edilemediyse veya erişilebilir değilse, local IP kullan
                if (string.IsNullOrWhiteSpace(publicUrl))
                {
                    if (!string.IsNullOrEmpty(localIp))
                    {
                        // Local IP bulundu - aynı network içinde kullanılabilir
                        publicUrl = $"http://{localIp}:{port}";
                        Log.Information("✅ Backend URL otomatik tespit edildi: {PublicUrl} (Local IP - sadece aynı network'ten erişilebilir)", publicUrl);
                        Log.Warning("⚠️ Public URL tespit edilemedi veya erişilebilir değil. Farklı network'lerden erişim için SOFTIELREMOTE_BACKEND_PUBLIC_URL environment variable'ını ayarlayın.");
                    }
                    else
                    {
                        // Local IP bulunamadı, localhost kullan
                        publicUrl = $"http://localhost:{port}";
                        Log.Warning("⚠️ Local IP tespit edilemedi, localhost kullanılıyor: {PublicUrl}. Sadece aynı bilgisayardan erişilebilir.", publicUrl);
                        Log.Warning("⚠️ Farklı network'lerden erişim için SOFTIELREMOTE_BACKEND_PUBLIC_URL environment variable'ını ayarlayın.");
                    }
                }
            }
            else
            {
                // Environment variable'dan alınan URL - Azure App Service'de kendi kendini test edemez
                // Bu yüzden erişilebilirlik testini atla (Azure internal network kısıtlaması)
                var isAzure = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));
                if (isAzure)
                {
                    Log.Information("✅ Backend URL environment variable'dan alındı (Azure App Service - erişilebilirlik testi atlandı): {PublicUrl}", publicUrl);
                }
                else
                {
                    // Azure değilse, erişilebilirlik testini yap
                    var isAccessible = await SoftielRemote.Core.Utils.BackendUrlAccessibilityTester.TestAccessibilityAsync(publicUrl, timeoutSeconds: 3);
                    if (isAccessible)
                    {
                        Log.Information("✅ Backend URL environment variable'dan alındı ve erişilebilir: {PublicUrl}", publicUrl);
                    }
                    else
                    {
                        Log.Warning("⚠️ Backend URL environment variable'dan alındı ancak erişilebilir değil: {PublicUrl}", publicUrl);
                    }
                }
            }

            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Backend'i kaydet - önce mevcut kaydı kontrol et (duplicate key hatasını önlemek için)
            var backendId = Environment.GetEnvironmentVariable("SOFTIELREMOTE_BACKEND_ID") 
                ?? $"{Environment.MachineName}_{DateTime.UtcNow.Ticks}";
            
            // Önce mevcut kaydı kontrol et (aynı makine adı ile)
            var existingBackend = await dbContext.BackendRegistry
                .FirstOrDefaultAsync(b => b.BackendId == backendId);
            
            if (existingBackend != null)
            {
                // Mevcut kaydı güncelle
                existingBackend.PublicUrl = publicUrl;
                existingBackend.LocalIp = localIp;
                existingBackend.LastSeen = DateTime.UtcNow;
                existingBackend.IsActive = true;
            }
            else
            {
                // Yeni kayıt oluştur
                var newBackend = new SoftielRemote.Backend.Models.BackendRegistryEntity
                {
                    BackendId = backendId,
                    PublicUrl = publicUrl,
                    LocalIp = localIp,
                    LastSeen = DateTime.UtcNow,
                    IsActive = true,
                    Description = $"Backend on {Environment.MachineName}"
                };
                dbContext.BackendRegistry.Add(newBackend);
            }
            
            // Retry logic ile kaydet (transient hatalar ve duplicate key için)
            var maxRetries = 3;
            var retryDelay = TimeSpan.FromSeconds(2);
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                        // Timeout ile kaydet (120 saniye - CommandTimeout ile uyumlu)
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
                        await dbContext.SaveChangesAsync(cts.Token);
                        Log.Information("✅ Backend Supabase'e kaydedildi: BackendId={BackendId}, PublicUrl={PublicUrl}", backendId, publicUrl);
                        break;
                }
                catch (DbUpdateException dbEx) when (dbEx.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
                {
                    // Duplicate key hatası - mevcut kaydı güncelle
                    Log.Warning("Backend kaydı duplicate key hatası (BackendId zaten mevcut), mevcut kayıt güncelleniyor: BackendId={BackendId}", backendId);
                    dbContext.ChangeTracker.Clear(); // Change tracker'ı temizle
                    
                    // Mevcut kaydı tekrar oku ve güncelle
                    var existingBackendToUpdate = await dbContext.BackendRegistry
                        .FirstOrDefaultAsync(b => b.BackendId == backendId);
                    
                    if (existingBackendToUpdate != null)
                    {
                        existingBackendToUpdate.PublicUrl = publicUrl;
                        existingBackendToUpdate.LocalIp = localIp;
                        existingBackendToUpdate.LastSeen = DateTime.UtcNow;
                        existingBackendToUpdate.IsActive = true;
                        
                        // Timeout ile kaydet (120 saniye)
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
                        await dbContext.SaveChangesAsync(cts.Token);
                        Log.Information("✅ Backend Supabase'e güncellendi: BackendId={BackendId}, PublicUrl={PublicUrl}", backendId, publicUrl);
                        break;
                    }
                    else
                    {
                        Log.Warning("Backend kaydı bulunamadı, yeni kayıt oluşturuluyor: BackendId={BackendId}", backendId);
                        // Yeni kayıt oluştur (BackendId'yi değiştir)
                        backendId = $"{Environment.MachineName}_{DateTime.UtcNow.Ticks}_{Guid.NewGuid():N}";
                        var newBackendRetry = new SoftielRemote.Backend.Models.BackendRegistryEntity
                        {
                            BackendId = backendId,
                            PublicUrl = publicUrl,
                            LocalIp = localIp,
                            LastSeen = DateTime.UtcNow,
                            IsActive = true,
                            Description = $"Backend on {Environment.MachineName}"
                        };
                        dbContext.BackendRegistry.Add(newBackendRetry);
                        // Retry için döngü devam edecek
                    }
                }
                catch (Exception saveEx) when (attempt < maxRetries && (saveEx is Npgsql.NpgsqlException || saveEx.InnerException is Npgsql.NpgsqlException))
                {
                    var npgsqlEx = saveEx as Npgsql.NpgsqlException ?? saveEx.InnerException as Npgsql.NpgsqlException;
                    if (npgsqlEx?.IsTransient == true)
                    {
                        Log.Warning("Backend kayıt denemesi {Attempt}/{MaxRetries} başarısız (transient hata), {Delay} saniye sonra tekrar denenecek: {Error}", 
                            attempt, maxRetries, retryDelay.TotalSeconds, npgsqlEx.Message);
                        await Task.Delay(retryDelay);
                        retryDelay = TimeSpan.FromSeconds(retryDelay.TotalSeconds * 2); // Exponential backoff
                    }
                    else
                    {
                        Log.Warning("Backend kaydı başarısız (devam edilecek): {Error}", saveEx.Message);
                        break; // Transient değilse veya son deneme ise dur
                    }
                }
                catch (OperationCanceledException)
                {
                    Log.Warning("Backend kayıt timeout oldu (deneme {Attempt}/{MaxRetries})", attempt, maxRetries);
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(retryDelay);
                        retryDelay = TimeSpan.FromSeconds(retryDelay.TotalSeconds * 2);
                    }
                    else
                    {
                        Log.Warning("Backend kaydı timeout nedeniyle başarısız (devam edilecek)");
                        break;
                    }
                }
            }
            
            // Periyodik heartbeat (her 5 dakikada bir)
            var heartbeatTimer = new System.Timers.Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
            heartbeatTimer.Elapsed += async (sender, e) =>
            {
                try
                {
                    using var heartbeatScope = app.Services.CreateScope();
                    var heartbeatDbContext = heartbeatScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var heartbeatBackend = await heartbeatDbContext.BackendRegistry
                        .FirstOrDefaultAsync(b => b.BackendId == backendId);
                    
                    if (heartbeatBackend != null)
                    {
                        heartbeatBackend.LastSeen = DateTime.UtcNow;
                        heartbeatBackend.IsActive = true;
                        
                        // Retry logic ile kaydet
                        var maxHeartbeatRetries = 2;
                        for (int attempt = 1; attempt <= maxHeartbeatRetries; attempt++)
                        {
                            try
                            {
                                await heartbeatDbContext.SaveChangesAsync();
                                break;
                            }
                            catch (Exception heartbeatEx) when (attempt < maxHeartbeatRetries && (heartbeatEx is Npgsql.NpgsqlException || heartbeatEx.InnerException is Npgsql.NpgsqlException))
                            {
                                var npgsqlEx = heartbeatEx as Npgsql.NpgsqlException ?? heartbeatEx.InnerException as Npgsql.NpgsqlException;
                                if (npgsqlEx?.IsTransient == true)
                                {
                                    await Task.Delay(TimeSpan.FromSeconds(1));
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Backend heartbeat hatası");
                }
            };
            heartbeatTimer.Start();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Backend kaydı başarısız (devam edilecek)");
        }
    });
}

try
{
    Log.Information("Starting SoftielRemote Backend...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
