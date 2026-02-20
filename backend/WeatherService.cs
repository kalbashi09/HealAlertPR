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
        private readonly string myKey = "roy47bRvumwAsn0y6ymAgAkPMTAgPQhY"; // Tomorrow.io API Key
        private readonly HttpClient _http = new HttpClient();
        private readonly Random _rng = new Random();

        // Reference center for Talisay City
        private const double CityLat = 10.2447;
        private const double CityLng = 123.8494;

       public async Task<List<BarangayWeather>> GetRealTimeTalisayData()
        {
            // 1. Tomorrow.io Realtime Endpoint
            // location: lat,lng | apikey: your key
        string url = $"https://api.tomorrow.io/v4/weather/realtime?location={CityLat},{CityLng}&apikey={myKey}";

            try
            {
                string jsonResponse = await _http.GetStringAsync(url);
                var json = JObject.Parse(jsonResponse);

                // 2. PATH FIX: Tomorrow.io uses data -> values -> temperatureApparent
                var values = json["data"]?["values"];
                double realHeatIndex = values?["temperatureApparent"]?.Value<double>() ?? 0;
                
                // Optional: Grab humidity for your bot
                double humidity = values?["humidity"]?.Value<double>() ?? 0;

                var barangays = new List<string> { "Biasong", "Bulacao", "Cadulawan", "Camp IV", "Cansojong", 
                    "Dumlog", "Jaclupan", "Lagtang", "Lawaan I", "Lawaan II", 
                    "Lawaan III", "Linao", "Maghaway", "Manipis", "Mohon", 
                    "Poblacion", "Pooc", "San Isidro", "San Roque", "Tabunok", 
                    "Tangke", "Tapul" };
                var weatherList = new List<BarangayWeather>();

                foreach (var name in barangays)
                {
                    // Tomorrow.io is very precise, so use a smaller random jitter (0.4 range)
                    double variety = (_rng.NextDouble() * 0.4) - 0.2;

                    weatherList.Add(new BarangayWeather
                    {
                        Name = name,
                        Temp = Math.Round(realHeatIndex + variety, 1),
                        Lat = CityLat,
                        Lng = CityLng
                    });
                }
                return weatherList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Tomorrow.io API failed: {ex.Message}");
                return new List<BarangayWeather>();
            }
        }
    }
}