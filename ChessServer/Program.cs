using ChessServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Thêm hỗ trợ gRPC
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true; // Bật lỗi chi tiết để debug
});

// Đăng ký ChessServiceImpl với lifetime Singleton
builder.Services.AddSingleton<ChessServiceImpl>();

// Lắng nghe trên mọi giao diện mạng, cổng 5038
builder.WebHost.UseUrls("http://0.0.0.0:5038");

var app = builder.Build();

// Cấu hình pipeline
app.UseRouting();
app.MapGrpcService<ChessServiceImpl>();
app.MapGet("/", () => "gRPC server is running for Chinese Chess.");

await app.RunAsync();