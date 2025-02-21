using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.WebSockets;

namespace NetworkDiscovery
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSingleton<NetworkService>();

            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            // Configure middleware
            app.UseCors("AllowAll");

            // Configure WebSocket options
            var webSocketOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromMinutes(2)
            };
            app.UseWebSockets(webSocketOptions);

            app.UseRouting();

            // Map endpoints
            app.MapGet("/interfaces", async (NetworkService service) =>
            {
                try
                {
                    var interfaces = NetworkService.GetNetworkInterfaces();
                    return Results.Ok(interfaces);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            app.MapPost("/scan/start", async (NetworkService service, string interfaceName) =>
            {
                try
                {
                    await service.StartScanningAsync(interfaceName);
                    return Results.Ok("Scanning started");
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            app.MapPost("/scan/stop", async (NetworkService service) =>
            {
                try
                {
                    await service.StopScanningAsync();
                    return Results.Ok("Scanning stopped");
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            // WebSocket endpoint
            app.MapGet("/ws", async (HttpContext context, NetworkService service) =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    try
                    {
                        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await service.HandleWebSocketConnection(webSocket);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"WebSocket error: {ex.Message}");
                    }
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                }
            });

            // Start the server
            app.Run("http://localhost:35123");
        }
    }
}
