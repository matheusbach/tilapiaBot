using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Net;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;

namespace Tilapia
{
    internal static class Program
    {
        public static CultureInfo ci = new CultureInfo("en-US");

        public static readonly TelegramBotClient botClient = new TelegramBotClient(System.IO.File.ReadAllText("telegramTokenAPI").Trim('\r', '\n'));
        public static dynamic coinList;

        private static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
            CultureInfo.DefaultThreadCurrentCulture = ci;
            CultureInfo.DefaultThreadCurrentUICulture = ci;
            CultureInfo.CurrentCulture = ci;
            CultureInfo.CurrentUICulture = ci;

            Console.WriteLine("Tilápia Bot Iniciado\n");

            try { Console.Write("Obtendo lista de moedas... "); coinList = JsonConvert.DeserializeObject(new WebClient().DownloadString("https://api.coinpaprika.com/v1/coins/")); Console.WriteLine("feito"); } catch (Exception) { Console.WriteLine("falhou"); }

            User me = botClient.GetMeAsync().Result;

            using var cts = new CancellationTokenSource();

            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            ReceiverOptions receiverOptions = new() { AllowedUpdates = { } };
            botClient.StartReceiving(TelegramHandlers.HandleUpdateAsync,
                               TelegramHandlers.HandleErrorAsync,
                               receiverOptions,
                               cts.Token);

            Console.WriteLine($"Telegram Bot start listening for @{me.Username}");

            while (true)
            {
                try
                {
                    Thread.Sleep(8640000);
                    coinList = JsonConvert.DeserializeObject(new WebClient().DownloadString("https://api.coinpaprika.com/v1/coins/"));
                }
                catch { }
            }
        }
    }
}