using Challenge2_Group16_GUI_WebAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Challenge2_Group16_GUI_WebAPI.Middlewares
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly WebSocketManagerService _webSocketManagerService;

        public WebSocketMiddleware(RequestDelegate next,
            WebSocketManagerService webSocketManagerService)
        {
            _next = next;
            _webSocketManagerService = webSocketManagerService;
        }

        public async Task Invoke(HttpContext httpContext, WebSocketHandlerService webSocketService)
        {
            Console.WriteLine("Request");
            if (httpContext.Request.Path == "/ws")
            {
                if (httpContext.WebSockets.IsWebSocketRequest)
                {
                    var socketId = Guid.NewGuid().ToString();
                    WebSocket webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
                    try
                    {
                        Console.WriteLine("WebSocket connected");
                        _webSocketManagerService.AddSocket(socketId, webSocket);
                        Console.WriteLine($"Added socket {socketId} to list of connected socets");

                        var handler = webSocketService.HandleAsync(webSocket, socketId);
                        var ping = webSocketService.SendPing(webSocket);

                        await Task.WhenAny(handler, ping);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during WebSocket handling {ex.Message}");
                    }
                    finally
                    {
                        Console.WriteLine("Websocket terminating...");
                        await _webSocketManagerService.RemoveSocketAsync(socketId);
                        webSocket.Dispose();
                    }

                    return;
                }   
                else
                {
                    Console.WriteLine("Not a WebSocket request");
                    httpContext.Response.StatusCode = 400;
                    return;
                }
            }
            else
            {
                Console.WriteLine("Request path is not /ws");
            }

            await _next(httpContext);
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class WebSocketMiddlewareExtensions
    {
        public static IApplicationBuilder UseWebSocketMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<WebSocketMiddleware>();
        }
    }
}
