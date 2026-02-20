using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace HeatAlert
{
    public class BotAlertSender
    {
        private readonly TelegramBotClient _botClient;
        private readonly DatabaseManager _db;

        public BotAlertSender(string token, DatabaseManager db)
        {
            _botClient = new TelegramBotClient(token);
            _db = db;
        }

        public void StartBot()
        {
            using var cts = new CancellationTokenSource();

            // ReceiveOptions to ensure we get messages
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() 
            };

            _botClient.StartReceiving
            (
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync, // "pollingErrorHandler" becomes "errorHandler"
                receiverOptions: receiverOptions
            );

            Console.WriteLine("🤖 Bot is now listening for subscribers & commands...");
        }

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            // Only handle text messages
            if (update.Message is not { Text: { } messageText } message) return;

            long chatId = message.Chat.Id;
            string text = messageText.ToLower().Trim();
            string username = message.From?.Username ?? "User";

            // 1. Subscription Logic
            if (text == "/subscribeservice")
            {
                await _db.SaveSubscriber(chatId, username);
                await bot.SendMessage(chatId, "✅ *Subscribed!* You will now receive live heat alerts for Talisay City.", parseMode: ParseMode.Markdown);
            }
            else if (text == "/unsubscribeservice")
            {
                await _db.RemoveSubscriber(chatId);
                await bot.SendMessage(chatId, "👋 *Unsubscribed.* You will no longer receive updates.", parseMode: ParseMode.Markdown);
            }
            
            // 2. Weather Command
            else if (text == "/weather") 
            {
                await bot.SendMessage(chatId, "📡 *Fetching latest satellite data...*", parseMode: ParseMode.Markdown);
                
                var weatherService = new WeatherService();
                var data = await weatherService.GetRealTimeTalisayData();
                
                if (data != null && data.Any())
                {
                    var hottest = data.OrderByDescending(w => w.Temp).First();
                    var avgTemp = data.Average(w => w.Temp);

                    string response = $"🌦️ *Talisay City Live Status*\n\n" +
                                     $"City Average: *{avgTemp:F1}°C*\n" +
                                     $"Hottest Area: *{hottest.Name}* ({hottest.Temp}°C)\n\n" +
                                     $"_Source: Tomorrow.io API_";

                    await bot.SendMessage(chatId, response, parseMode: ParseMode.Markdown);
                }
                else
                {
                    await bot.SendMessage(chatId, "⚠️ Could not reach weather servers. Try again later.");
                }
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
        {
            Console.WriteLine($"[BOT ERROR]: {ex.Message}");
            return Task.CompletedTask;
        }

        public async Task BroadcastAlert(string alertMsg, double lat, double lng, List<long> subscriberIds)
        {
            int sentCount = 0;
            foreach (var id in subscriberIds)
            {
                try 
                {
                    // Send the text alert
                    await _botClient.SendMessage(chatId: id, text: alertMsg, parseMode: ParseMode.Markdown);
                    
                    // Send the map location
                    await _botClient.SendLocation(chatId: id, latitude: lat, longitude: lng);
                    
                    sentCount++;
                } 
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ {id} blocked the bot: {ex.Message}");
                }
            }
            Console.WriteLine($"📢 Broadcast: Sent to {sentCount} active subscribers.");
        }
    }
}