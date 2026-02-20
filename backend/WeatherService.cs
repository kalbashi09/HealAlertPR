using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace HeatAlert
{

    public class BarangayWeather
    {
        public string Name { get; set; } = string.Empty;
        public double Temp { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
    }

    public class WeatherService
    {
        private readonly string _apiKey = "d0f2e9c7093dd235555aa2956db0546f";
        private readonly HttpClient _http = new HttpClient();
        private readonly Random _rng = new Random();

        // Coordinates for the center of Talisay City
        private const double CityLat = 10.2447;
        private const double CityLng = 123.8494;

        public async Task<List<BarangayWeather>> GetRealTimeTalisayData()
        {
            try
            {
                // 1. Get Real Data from OpenWeather
                string url = $"https://api.openweathermap.org/data/2.5/weather?lat={CityLat}&lon={CityLng}&appid={_apiKey}&units=metric";
                string jsonResponse = await _http.GetStringAsync(url);
                var data = JObject.Parse(jsonResponse);

                // OpenWeather "feels_like" is the most accurate for Heat Index
                double realTemp = data["main"]?["feels_like"]?.Value<double>() ?? 0;

                // 2. Map this to your GeoJSON Barangay Names
                var barangays = new List<string> { 
                    "Biasong",
                    "Bulacao",
                    "Cadulawan",
                    "Camp IV",
                    "Cansojong",
                    "Dumlog",
                    "Jaclupan",
                    "Lagtang",
                    "Lawaan I",
                    "Lawaan II",
                    "Lawaan III",
                    "Linao",
                    "Maghaway",
                    "Manipis",
                    "Mohon",
                    "Poblacion",
                    "Pooc",
                    "San Isidro",
                    "San Roque",
                    "Tabunok",
                    "Tangke",
                    "Tapul" 
                    };
                var weatherList = new List<BarangayWeather>();

                foreach (var name in barangays)
                {
                    weatherList.Add(new BarangayWeather
                    {
                        Name = name,
                        // Add a tiny random shift (+/- 0.3) so the map looks alive
                        Temp = Math.Round(realTemp + (_rng.NextDouble() * 0.6 - 0.3), 1),
                        Lat = CityLat,
                        Lng = CityLng
                    });
                }

                return weatherList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to fetch weather: {ex.Message}");
                return new List<BarangayWeather>();
            }
        }
    }
}