using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly TronService _tronService;
        public WeatherForecastController(ILogger<WeatherForecastController> logger,TronService tronService)
        {
            _tronService = tronService;
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpPost("transfer")]
        public async Task<IActionResult> TransferAsync()
        {
            try
            {
                // Transfer i�lemini ba�lat�yoruz
                var result =  _tronService.MonitorAndTransferTrxAsync();

                // Ba�ar�yla transfer tamamlanm��sa, ba�ar�l� mesaj� d�nd�r�yoruz
                return Ok(new { message = "Transfer i�lemi ba�ar�l�.", result });
            }
            catch (Exception ex)
            {
                // Hata durumunda hata mesaj�n� d�nd�r�yoruz
                return BadRequest(new { message = "Transfer i�lemi ba�ar�s�z.", error = ex.Message });
            }
        }

    }
}
