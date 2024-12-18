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
            _httpClient.BaseAddress = new Uri("https://api.trongrid.io");
            _hubContext = hubContext; // SignalR Hub context
        }

        public async Task MonitorAndTransferTrxAsync()
        {
            var privateKey = "e95295df4634a8e08e2a505b89339757ccebc4ea5e87b567140dc9aa09530f83";
            var ecKey = new TronECKey(privateKey, _options.Value.Network);
            var fromAddress = "TXNcyg5JxoW2NhHB9oQ7HZGHAs1gRdHbRr"; // Dinlenecek cüzdan
            var toAddress = "TTZQBBNCwd3BLR1GH99rxwBC3RSzqXbRgq"; // Transfer yapılacak cüzdan

            decimal previousBalance = await GetTrxBalanceAsync(fromAddress);
            Console.WriteLine($"Başlangıç bakiyesi: {previousBalance} TRX");

            while (true)
            {
                await Task.Delay(100); // 1 saniye bekle

                try
                {
                    decimal currentBalance = await GetTrxBalanceAsync(fromAddress);

                    if (currentBalance > previousBalance)
                    {
                        decimal newAmount = currentBalance - previousBalance;
                        Console.WriteLine($"Yeni TRX girişi tespit edildi: {newAmount} TRX");

                        // SignalR ile tüm bağlı istemcilere bildirim gönder
                        await _hubContext.Clients.All.SendAsync("ReceiveMessage", $"Yeni TRX girişi tespit edildi: {newAmount} TRX");

                        try
                        {
                            // Mikro-TRX'e dönüştür (1 TRX = 1,000,000 mikro-TRX)
                            long microAmount = (long)(newAmount * 1_000_000);

                            // Transfer işlemini başlat
                            var transactionClient = _tronClient.GetTransaction();
                            var transactionExtension = await transactionClient.CreateTransactionAsync(fromAddress, toAddress, microAmount);

                            // İşlemi imzala
                            var transactionSigned = transactionClient.GetTransactionSign(transactionExtension.Transaction, privateKey);

                            // İmzalanmış işlemi yayınla
                            var result = await transactionClient.BroadcastTransactionAsync(transactionSigned);

                            // Sonuç kontrolü
                            if (result.Result)
                            {
                                Console.WriteLine("TRX transfer işlemi başarılı.");
                          

                                // Transfer başarılıysa bakiyeyi güncelle
                                previousBalance = currentBalance;

                                // Döngüyü sıfırlamaya gerek yoksa buradan devam edin
                                Console.WriteLine("İşlem tamamlandı.");
                            }
                            else
                            {
                                Console.WriteLine($"TRX transfer işlemi başarısız. Hata detayları: {result.Message}");
                                throw new Exception("Transfer işlemi başarısız.");
                            }
                        }
                        catch (Exception ex)
                        {
                            // Hata durumunda loglama ve yeniden başlatma
                            Console.WriteLine($"Hata oluştu: {ex.Message}");
                            throw; // Hatanın üst seviyeye taşınması
                        }
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
        //public string GetTransactionHash( signedTransaction)
        //{
        //    using (var sha256 = System.Security.Cryptography.SHA256.Create())
        //    {
        //        var hash = sha256.ComputeHash(signedTransaction.RawData.ToByteArray());
        //        return BitConverter.ToString(hash).Replace("-", "").ToLower();
        //    }
        //}

    }
}



        