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
            _httpClient.BaseAddress = new Uri("https://api.trongrid.io"); //https://nile.trongrid.io ,https://api.trongrid.io
            _hubContext = hubContext; // SignalR Hub context
        }

        public async Task MonitorAndTransferTrxAsync()
        {
            var privateKey = "";//private key
            var fromAddress = ""; // Dinlenen cüzdan
            var toAddress = ""; // Transfer yapılacak cüzdan

            try
            {
                while (true) // Sonsuz döngü
                {
                    decimal previousBalance = await GetTrxBalanceAsync(fromAddress); // Mevcut bakiyeyi al
                    Console.WriteLine($"Dinleme başladı, mevcut bakiye: {previousBalance} TRX");

                    // Bakiyeyi periyodik olarak kontrol et
                    while (true)
                    {
                        await Task.Delay(1000); // 1 saniye bekle

                        decimal currentBalance = await GetTrxBalanceAsync(fromAddress);

                        if (currentBalance > previousBalance) // Yeni para girişi kontrolü
                        {
                            decimal newAmount = currentBalance - previousBalance;
                            Console.WriteLine($"Yeni TRX girişi tespit edildi: {newAmount} TRX");

                            try
                            {
                                // Mikro-TRX'e dönüştür
                                long microAmount = (long)(newAmount * 1_000_000);

                                // Transfer işlemini başlat
                                var transactionClient = _tronClient.GetTransaction();
                                var transactionExtension = await transactionClient.CreateTransactionAsync(fromAddress, toAddress, microAmount);

                                // İşlemi imzala
                                var transactionSigned = transactionClient.GetTransactionSign(transactionExtension.Transaction, privateKey);

                                // İmzalanmış işlemi yayınla
                                var result = await transactionClient.BroadcastTransactionAsync(transactionSigned);

                                if (result.Result) // Transfer başarılı mı kontrol et
                                {
                                    Console.WriteLine("TRX transfer işlemi başarılı.");
                                    previousBalance = currentBalance; // Başarılı transfer sonrası bakiyeyi güncelle
                                    break; // Transfer sonrası döngüyü kır ve baştan başla
                                }
                                else
                                {
                                    Console.WriteLine($"TRX transfer işlemi başarısız. Hata: {result.Message}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Transfer sırasında hata oluştu: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Bir hata oluştu: {ex.Message}");
                Console.WriteLine("Dinleme döngüsü yeniden başlatılıyor...");
                await MonitorAndTransferTrxAsync(); // Hata durumunda tekrar başlat
            }
        }

        public async Task<decimal> GetTrxBalanceAsync(string address)
        {
            var response = await _httpClient.GetAsync($"/v1/accounts/{address}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                dynamic data = JsonConvert.DeserializeObject(json);

                long balanceInMicroTrx = data.data[0].balance;
                decimal balanceInTrx = balanceInMicroTrx / 1_000_000m;

                return balanceInTrx;
            }
            else
            {
                throw new Exception("TRX bakiyesi alınamadı.");
            }
        }

    }
}




        