using Telegram.Bot;
using Telegram.Bot.Polling; // Needed for StartReceiving
using Telegram.Bot.Types;

namespace HeatAlert
{
    public class BotAlertSender
    {
        private readonly TelegramBotClient _botClient;
        private readonly DatabaseManager _db; // Link to your SQL manager

        public BotAlertSender(string token, DatabaseManager db)
        {
            _botClient = new TelegramBotClient(token);
            _db = db;
        }

        // --- THE LISTENER ---
        public void StartBot()
        {
            var receiverOptions = new ReceiverOptions { AllowedUpdates = { } }; // Receive all update types
            _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions);
            Console.WriteLine("🤖 Bot is now listening for subscribers...");
        }

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            if (update.Message is not { Text: not null } message) return;

            long chatId = message.Chat.Id;
            string text = message.Text.ToLower();
            string username = message.From?.Username ?? "UnknownUser";

            // 1. Unified Subscription Logic (Removed the double-check you had)
            if (text == "/subscribeservice")
            {
                await _db.SaveSubscriber(chatId, username);
                await bot.SendMessage(chatId, "✅ *Subscribed\\!* You will now receive live heat alerts for Talisay City\\.", parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2);
            }
            else if (text == "/unsubscribeservice")
            {
                await _db.RemoveSubscriber(chatId);
                await bot.SendMessage(chatId, "👋 *Unsubscribed\\.* You will no longer receive heat signature updates\\.", parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2);
            }
            // 2. Enhanced Weather Check
            else if (text == "/weather") 
            {
                var weatherService = new WeatherService();
                var data = await weatherService.GetRealTimeTalisayData();
                
                if (data.Any())
                {
                    var hottest = data.OrderByDescending(w => w.Temp).First();
                    var avgTemp = data.Average(w => w.Temp);

                    string response = $"🌦️ *Talisay City Live Status*\n\n" +
                                    $"City Average: *{avgTemp:F1}°C*\n" +
                                    $"Hottest Area: *{hottest.Name}* \\({hottest.Temp}°C\\)\n\n" +
                                    $"_Source: OpenWeather API_";

                    await bot.SendMessage(chatId, response, parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2);
                }
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
        {
            Console.WriteLine($"Bot Error: {ex.Message}");
            return Task.CompletedTask;
        }

        // --- THE BROADCASTER ---
        // Instead of sending to ONE hardcoded ID, send to the whole list from DB
        // --- THE BROADCASTER ---
        // Refactored to accept a pre-formatted string instead of raw doubles
                // Change 'public async Task BroadcastAlert(string alertMsg, List<long> subscriberIds)'
        // to this:
        public async Task BroadcastAlert(string alertMsg, double lat, double lng, List<long> subscriberIds)
        {
            int sentCount = 0;
            foreach (var id in subscriberIds)
            {
                try 
                {
                    await _botClient.SendMessage(chatId: id, text: alertMsg, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    
                    // This is the extra line that uses the 'lat' and 'lng' we added
                    await _botClient.SendLocation(chatId: id, latitude: lat, longitude: lng);
                    
                    sentCount++;
                } 
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ {id} blocked the bot: {ex.Message}");
                }
            }
            Console.WriteLine($"📢 Sent to {sentCount} subscribers.");
            }
        }
}