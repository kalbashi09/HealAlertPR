using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.DependencyInjection;
using HeatAlert;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();
app.UseCors("AllowAll");

// --- OBJECTS ---
var db = new DatabaseManager();
var bot = new BotAlertSender("8439622862:AAGCRTIItpNNK3UUNT8pUMRwd5WlywyRh1M", db); 
RealTimeAlert? latestRealTimeAlert = null; 

// API ENDPOINT 
app.MapGet("/api/current-alert", () => {
    return latestRealTimeAlert != null ? Results.Ok(latestRealTimeAlert) : Results.NotFound("No data yet.");
});

// MAIN MONITORING LOOP
_ = Task.Run(async () => {
    bot.StartBot();
    var weatherService = new WeatherService(); 
    Console.WriteLine("🚀 Monitoring system active. Real-time OpenWeather loop starting...");

    while (true)
    {
        try 
        {
            var weatherData = await weatherService.GetRealTimeTalisayData();
            var peakAlert = weatherData.OrderByDescending(w => w.Temp).FirstOrDefault();

            if (peakAlert != null)
            {
                latestRealTimeAlert = new RealTimeAlert {
                    BarangayName = peakAlert.Name,
                    HeatIndex = peakAlert.Temp, 
                    Lat = peakAlert.Lat,
                    Lng = peakAlert.Lng,
                    RelativeLocation = "Live Satellite Data"
                };

                // NEW: Use the local helper method instead of 'simulator'
                string level = CalculateDangerLevel(peakAlert.Temp);

                // Alert Thresholds (Heat Index focus)
                if (peakAlert.Temp >= 39) 
                {
                    string message = $"⚠️ *HEAT ALERT: {level}*\n\n" +
                                     $"📍 *Location:* {peakAlert.Name}, Talisay City\n" +
                                     $"🌡️ *Heat Index:* {peakAlert.Temp}°C\n" +
                                     $"🌐 *Coords:* {peakAlert.Lat:F4}, {peakAlert.Lng:F4}";

                    var subscribers = await db.GetAllSubscriberIds();
                    await bot.BroadcastAlert(message, peakAlert.Lat, peakAlert.Lng, subscribers);
                    
                    Console.WriteLine($"[BROADCAST] Alert sent for {peakAlert.Name} ({peakAlert.Temp}°C)");
                }
                else
                {
                    Console.WriteLine($"[STABLE] {peakAlert.Name}: {peakAlert.Temp}°C ({level}).");
                }
            }
        }
        catch (Exception ex) { Console.WriteLine($"[ERROR] {ex.Message}"); }

        await Task.Delay(10000); 
    }
});

app.Run();

// --- HELPER METHODS ---
static string CalculateDangerLevel(double temp)
{
    if (temp >= 52) return "EXTREME DANGER";
    if (temp >= 42) return "DANGER";
    if (temp >= 33) return "EXTREME CAUTION";
    return "CAUTION";
}

public class RealTimeAlert
{
    public string BarangayName { get; set; } = string.Empty;
    public double HeatIndex { get; set; } 
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string RelativeLocation { get; set; } = string.Empty;
}