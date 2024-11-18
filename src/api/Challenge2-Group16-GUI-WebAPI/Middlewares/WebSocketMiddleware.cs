using Challenge2_Group16_GUI_WebAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Challenge2_Group16_GUI_WebAPI.Middlewares
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;

        public WebSocketMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext, WebSocketHandlerService webSocketService)
        {
            Console.WriteLine("Request");
            if (httpContext.Request.Path == "/ws")
            {
                if (httpContext.WebSockets.IsWebSocketRequest)
                {
                    WebSocket webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
                    Console.WriteLine("WebSocket connected");
                    await webSocketService.HandleAsync(webSocket);
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
