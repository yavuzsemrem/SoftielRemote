using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.WriteLine(">>> PROGRAM STARTED <<<");

var builder = WebApplication.CreateBuilder(args);
Console.WriteLine(">>> BUILDER CREATED <<<");

// Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

var app = builder.Build();
Console.WriteLine(">>> APP BUILT <<<");

// Middleware
app.MapHealthChecks("/health");

Console.WriteLine(">>> MAPPING DONE <<<");

// ⚠️ BU SATIR YOKSA → SENİN SORUNUN OLUR
app.Run();

// Bu satırdan SONRASI ASLA ÇALIŞMAZ
