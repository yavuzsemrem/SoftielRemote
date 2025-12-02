using SoftielRemote.Backend.Hubs;
using SoftielRemote.Backend.Repositories;
using SoftielRemote.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SignalR ekle
builder.Services.AddSignalR();

// CORS ekle (Agent ve App'in Backend'e bağlanabilmesi için)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Dependency Injection
builder.Services.AddSingleton<IAgentRepository, InMemoryAgentRepository>();
builder.Services.AddScoped<IAgentService, AgentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS kullan
app.UseCors("AllowAll");

// Controllers
app.MapControllers();

// SignalR Hub
app.MapHub<ConnectionHub>("/hubs/connection");

app.Run();
