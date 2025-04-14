using ChessServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Net;
using System;

var builder = WebApplication.CreateBuilder(args);

// Thêm hỗ trợ gRPC với các tùy chọn mở rộng
builder.Services.AddGrpc(options => {
    options.EnableDetailedErrors = true; // Bật lỗi chi tiết để debug
    options.MaxReceiveMessageSize = 16 * 1024 * 1024; // 16 MB
    options.MaxSendMessageSize = 16 * 1024 * 1024; // 16 MB
});

// Cấu hình Kestrel để lắng nghe tất cả địa chỉ IP
builder.WebHost.ConfigureKestrel(options =>
{
    // Lắng nghe ở tất cả địa chỉ IP trên cổng 5038
    options.Listen(IPAddress.Any, 5038, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

// Đăng ký ChessServiceImpl với lifetime Singleton
builder.Services.AddSingleton<ChessServiceImpl>();

var app = builder.Build();

// Hiển thị thông tin mạng khi khởi động
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("=== Xiangqi Chess Server ===");
Console.WriteLine("Server đã khởi động ở cổng 5038");
Console.WriteLine("Địa chỉ IP để kết nối:");

// Hiển thị IP một cách rõ ràng và dễ đọc
foreach (var ip in Dns.GetHostAddresses(Dns.GetHostName())
    .Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
{
    // Hiển thị IP nội bộ rõ ràng hơn
    if (ip.ToString().StartsWith("192.168.") || 
        ip.ToString().StartsWith("10.") || 
        ip.ToString().StartsWith("172."))
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"* {ip} (sử dụng IP này để kết nối trong mạng nội bộ)");
    }
    else
    {
        Console.WriteLine($"- {ip}");
    }
}
Console.ResetColor();

Console.WriteLine("\nHướng dẫn:");
Console.WriteLine("1. Ghi lại một trong các địa chỉ IP bên trên");
Console.WriteLine("2. Trên máy khách, nhập địa chỉ IP này vào ô nhập và nhấn Connect");
Console.WriteLine("3. Đảm bảo tường lửa cho phép kết nối đến cổng 5038");

// Cấu hình pipeline
app.UseRouting();
app.MapGrpcService<ChessServiceImpl>();
app.MapGet("/", () => "Máy chủ cờ tướng đang chạy.");

await app.RunAsync();