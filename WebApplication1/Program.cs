using Microsoft.AspNetCore.SignalR;
using TronNet;
using WebApplication1.Hubs;
using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

// SignalR'ý ve diðer servisleri ekleyin
builder.Services.AddSignalR();
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<TronService>();

// TRON aðý ayarlarý
builder.Services.AddTronNet(x =>
{
    x.Network = TronNetwork.TestNet;
    x.Channel = new GrpcChannelOption { Host = "grpc.nile.trongrid.io", Port = 50051 };
    x.SolidityChannel = new GrpcChannelOption { Host = "grpc.nile.trongrid.io", Port = 50052 };
    x.ApiKey = "bbf6d1c9-daf4-49d9-a088-df29f664bac9";
});

var app = builder.Build();

// Swagger ve diðer yapýlandýrmalar
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// SignalR Hub'ý buraya ekliyoruz
app.MapHub<TrxTransactionHub>("/trxTransactionHub");

app.Run();
