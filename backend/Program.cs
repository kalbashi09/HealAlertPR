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
var simulator = new HeatSimulator(); 

// RENAMED: to avoid conflict with AlertResult from HeatSimulator
RealTimeAlert? latestRealTimeAlert = null; 

// 3. API ENDPOINT 
app.MapGet("/api/current-alert", () => {
    return latestRealTimeAlert != null ? Results.Ok(latestRealTimeAlert) : Results.NotFound("No data yet.");
});

// 4. MAIN MONITORING LOOP
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
                // FIX: Use the new class and direct double-to-double assignment
                latestRealTimeAlert = new RealTimeAlert {
                    BarangayName = peakAlert.Name,
                    HeatIndex = peakAlert.Temp, // No more conversion error
                    Lat = peakAlert.Lat,
                    Lng = peakAlert.Lng,
                    RelativeLocation = "Live Satellite Data"
                };

                string level = simulator.GetDangerLevel((int)peakAlert.Temp);

                if (peakAlert.Temp >= 39 || peakAlert.Temp < 30)
                {
                    string message = $"⚠️ *HEAT ALERT: {level}*\n\n" +
                                     $"📍 *Location:* {peakAlert.Name}, Talisay City\n" +
                                     $"🌡️ *Temp:* {peakAlert.Temp}°C\n" +
                                     $"🌐 *Coords:* {peakAlert.Lat:F4}, {peakAlert.Lng:F4}";

                    var subscribers = await db.GetAllSubscriberIds();
                    
                    // Passing Lat/Lng for the new Map Pin feature in BotAlertSender
                    await bot.BroadcastAlert(message, peakAlert.Lat, peakAlert.Lng, subscribers);
                    
                    Console.WriteLine($"[BROADCAST] Alert sent for {peakAlert.Name} ({peakAlert.Temp}°C)");
                }
                else
                {
                    Console.WriteLine($"[STABLE] {peakAlert.Name}: {peakAlert.Temp}°C. No broadcast needed.");
                }
            }
        }
        catch (Exception ex) { Console.WriteLine($"[ERROR] {ex.Message}"); }

        await Task.Delay(30000); 
    }
});

app.Run();


public class RealTimeAlert
{
    public string BarangayName { get; set; } = string.Empty;
    public double HeatIndex { get; set; } 
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string RelativeLocation { get; set; } = string.Empty;
}
