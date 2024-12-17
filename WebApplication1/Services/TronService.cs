using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TronNet;
using WebApplication1.Hubs;

namespace WebApplication1.Services
{
    public class TronService
    {
        private readonly ITransactionClient _transactionClient;
        private readonly ITronClient _tronClient;
        private readonly IOptions<TronNetOptions> _options;
        private readonly HttpClient _httpClient;
        private readonly IWalletClient _walletClient;
        private readonly IHubContext<TrxTransactionHub> _hubContext;
        public TronService(ITransactionClient transactionClient, IOptions<TronNetOptions> options, ITronClient tronClient, IWalletClient walletClient, IHubContext<TrxTransactionHub> hubContext)
        {
            _tronClient = tronClient;
            _options = options;
            _transactionClient = transactionClient;
            _httpClient = new HttpClient();
            _walletClient = walletClient;
            _httpClient.BaseAddress = new Uri("https://nile.trongrid.io");
            _hubContext = hubContext; // SignalR Hub context
        }

        public async Task MonitorAndTransferTrxAsync()
        {
            var privateKey = "0107932b30922231adff71b4b7c0b05bc948632f56c2b62f98bd18fefeae8a9e";
            var ecKey = new TronECKey(privateKey, _options.Value.Network);
            var fromAddress = "TEWJWLwFL3dbMjXtj2smNfto9sXdWquF4N"; // Dinlenecek cüzdan
            var toAddress = "TTZQBBNCwd3BLR1GH99rxwBC3RSzqXbRgq"; // Transfer yapılacak cüzdan

            decimal previousBalance = await GetTrxBalanceAsync(fromAddress);
            Console.WriteLine($"Başlangıç bakiyesi: {previousBalance} TRX");

            while (true)
            {
                await Task.Delay(1); // 1 saniye bekle

                try
                {
                    decimal currentBalance = await GetTrxBalanceAsync(fromAddress);

                    // Eğer bakiyede artış olduysa
                    if (currentBalance > previousBalance)
                    {
                        decimal newAmount = currentBalance - previousBalance;
                        Console.WriteLine($"Yeni TRX girişi tespit edildi: {newAmount} TRX");

                        // SignalR ile tüm bağlı istemcilere bildirim gönder
                         await _hubContext.Clients.All.SendAsync("ReceiveMessage", $"Yeni TRX girişi tespit edildi: {newAmount} TRX");
                        // Transfer işlemini başlat
                        var transactionExtension = await _transactionClient.CreateTransactionAsync(fromAddress, toAddress, (long)newAmount); // Mikro-TRX cinsine çeviriyoruz


                        var transactionSigned = _transactionClient.GetTransactionSign(transactionExtension.Transaction, privateKey);
                        var result = await _transactionClient.BroadcastTransactionAsync(transactionSigned);

                        if (result.Result)
                        {
                            Console.WriteLine("TRX transfer işlemi başarılı.");

                            // Transfer başarılıysa bakiyeyi güncelle
                            previousBalance = currentBalance;

                            // Başarılı işlem sonrası döngüyü sıfırlayıp baştan başlatıyoruz
                            Console.WriteLine("İşlem tamamlandı. Uygulama baştan başlayacak.");
                            break; // Döngüyü kırarak başa sarıyoruz
                        }
                        else
                        {
                            Console.WriteLine("TRX transfer işlemi başarısız. Hata detayları: " + result.Message);
                            throw new Exception("Transfer işlemi başarısız. Yeniden başlatılıyor.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Yeni TRX girişi bulunamadı.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Bir hata oluştu: {ex.Message}");

                    // Hata durumunda başa sarma
                    Console.WriteLine("Hata alındı. Uygulama baştan başlatılıyor.");
                    break; // Döngüyü kırarak başa sarıyoruz
                }
            }

            // Uygulamayı baştan başlatma işlemi için tekrar çağırabiliriz.
            Console.WriteLine("Başlatılıyor...");
            await MonitorAndTransferTrxAsync(); // Yine aynı işlemi başlatıyoruz.
        }

        public async Task<decimal> GetTrxBalanceAsync(string address)
        {
            var response = await _httpClient.GetAsync($"/v1/accounts/{address}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                dynamic data = JsonConvert.DeserializeObject(json);

                // TRX bakiyesi genellikle 'balance' içinde mikro-TRX (1 TRX = 1,000,000 mikro-TRX)
                long balanceInMicoTrx = data.data[0].balance;
                decimal balanceInTrx = balanceInMicoTrx / 1000000m; // Mikro-TRX'i TRX'e çeviriyoruz

                return balanceInTrx;
            }
            else
            {
                throw new Exception("TRX bakiyesi alınamadı.");
            }
        }

    }
}



        