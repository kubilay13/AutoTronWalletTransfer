using Microsoft.AspNetCore.SignalR;
using Nethereum.JsonRpc.Client;

namespace WebApplication1.Hubs
{
    public class TrxTransactionHub:Hub
    {
        public async Task NotifyTransaction(string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }
    }
}
