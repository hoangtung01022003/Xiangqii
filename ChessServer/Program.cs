using ChessServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

// Đăng ký ChessServiceImpl với lifetime Singleton
builder.Services.AddSingleton<ChessServiceImpl>();

// Nghe trên mọi giao diện mạng
builder.WebHost.UseUrls("http://0.0.0.0:5038");

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<ChessServiceImpl>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();