using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SoftielRemote.Backend.Api.Data;
using SoftielRemote.Backend.Api.Options;
using SoftielRemote.Backend.Api.Services;
using System.Text;
using Dapper;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Port (istersen --urls ile override edersin)
builder.WebHost.UseUrls("http://127.0.0.1:5099");

// CORS (dev için açık)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Options
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// DI
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<AuthRepository>();
builder.Services.AddScoped<JwtTokenService>();

// Auth
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key missing");

builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
      options.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuer = true,
          ValidIssuer = jwtSection["Issuer"],
          ValidateAudience = true,
          ValidAudience = jwtSection["Audience"],
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
          ValidateLifetime = true,
          ClockSkew = TimeSpan.FromSeconds(30)
      };
  });

builder.Services.AddAuthorization();

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "SoftielRemote API", 
        Version = "v1" 
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header. Örnek: Bearer {token}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});




var app = builder.Build();


// Middleware order (CORS preflight için önce CORS iyi olur)
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Basic endpoints
app.MapGet("/", () => "SoftielRemote Backend API");

// Health (DB test: Dapper factory üzerinden)
app.MapGet("/health", async (IDbConnectionFactory dbFactory) =>
{
    try
    {
        using var conn = dbFactory.Create();
        conn.Open();
        var result = await Dapper.SqlMapper.QuerySingleAsync<int>(conn, "SELECT 1;");

        return Results.Json(new { status = "ok", db = "ok" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "ok", db = "not_available", error = ex.Message });
    }
});

app.MapControllers();

app.Run();
