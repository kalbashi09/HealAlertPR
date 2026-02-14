using HeatAlert;

var db = new DatabaseManager();
var bot = new BotAlertSender("8439622862:AAGCRTIItpNNK3UUNT8pUMRwd5WlywyRh1M", db); //API TOKEN, DO NOT SHARE THIS WITH ANYONE ELSE
var simulator = new HeatSimulator(); 

bot.StartBot();
Console.WriteLine("🚀 Monitoring system active. Simulation loop starting...");

while (true)
{
    // Ensure the path correctly points to your sharedresource folder
    var alert = simulator.GenerateAlert("../sharedresource/talisaycitycebu.json");
    string level = simulator.GetDangerLevel(alert.HeatIndex);

    // Only broadcast if it's a "danger" spike
    if (alert.HeatIndex >= 39)
    {
        // 1. Build the human-readable message with directions and coordinates

        string message =$"{level}\n" +
                        $"🌡️ Temp: {alert.HeatIndex}°C\n" +
                        $"📍 Location: {alert.RelativeLocation}\n" +
                        $"🌐 Coord: {alert.Lat:F4}, {alert.Lng:F4}";

                var subscribers = await db.GetAllSubscriberIds();
                await bot.BroadcastAlert(message, subscribers);
                
                Console.WriteLine($"[BROADCAST] Sent {level} to {subscribers.Count} users for {alert.BarangayName}.");
        
        // 4. Save to DB for the Team's Map
        // NO DB FOR THE MAP, just log the alert for now (or you can implement a method to save it if needed)
    }
    else if (alert.HeatIndex < 30) // Also log cool/normal temps for transparency, but no broadcast
    {
        // 1. Build the human-readable message with directions and coordinates

        string message =$"{level}\n" +
                        $"🌡️ Temp: {alert.HeatIndex}°C\n" +
                        $"📍 Location: {alert.RelativeLocation}\n" +
                        $"🌐 Coord: {alert.Lat:F4}, {alert.Lng:F4}";

                var subscribers = await db.GetAllSubscriberIds();
                await bot.BroadcastAlert(message, subscribers);
                
                Console.WriteLine($"[BROADCAST] Sent {level} to {subscribers.Count} users for {alert.BarangayName}.");
        
        // 4. Save to DB for the Team's Map
        // NO DB FOR THE MAP, just log the alert for now (or you can implement a method to save it if needed)
    }
    else
    {
        // --- LOGIC: Print to console only for Normal/Cool temps ---
        Console.WriteLine($"[STABLE] {alert.BarangayName}: {alert.HeatIndex}°C ({level}). No alert sent.");
    }

    await Task.Delay(30000); 
}