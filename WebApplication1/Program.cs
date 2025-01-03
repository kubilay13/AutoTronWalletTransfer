using TronNet;
using WebApplication1.Hubs;
using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

// SignalR'� ve di�er servisleri ekleyin
builder.Services.AddSignalR();
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<TronService>();

// TRON a�� ayarlar�
builder.Services.AddTronNet(x =>
{
    x.Network = TronNetwork.MainNet;
    x.Channel = new GrpcChannelOption { Host = "grpc.main.trongrid.io", Port = 50051 };
    x.SolidityChannel = new GrpcChannelOption { Host = "grpc.main.trongrid.io", Port = 50052 };
    x.ApiKey = "3d251bb6-e560-43d0-8893-ca8fe58d4eee";
});

var app = builder.Build();

// Swagger ve di�er yap�land�rmalar
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// SignalR Hub'� buraya ekliyoruz
app.MapHub<TrxTransactionHub>("/trxTransactionHub");

app.Run();
