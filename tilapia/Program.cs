using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Telegram.Bot;

namespace Tilápia
{
    internal static class Program
    {
        static string trend;
        static long lastTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        static TelegramBotClient botClient = new TelegramBotClient(System.IO.File.ReadAllText(@"telegramTokenAPI"));
        static dynamic anubisTrendAPIdata;
        static dynamic coinList;

        static void Main(string[] args)
        {
            Console.WriteLine("Tilápia Bot Iniciado (UTC)\n");
            botClient.OnMessage += botClient_OnMessage;
            botClient.StartReceiving();

            while (true)
            {
                coinList = JsonConvert.DeserializeObject(new WebClient().DownloadString("https://api.coinpaprika.com/v1/coins/"));
                Thread.Sleep(300000);
            }
        }

        static void botClient_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            if (e.Message.Type == Telegram.Bot.Types.Enums.MessageType.Text)
            {
                if (e.Message.Text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("\n" + e.Message.Text);
                    if (e.Message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private)
                    {
                        telegramEnviarMensagem(e.Message.Chat.Id, @"Necessário colocar uma mensagem aqui");
                    }
                    else
                    {
                        telegramEnviarMensagem(e.Message.Chat.Id, @"Necessário colocar aqui uma mensagem ainda");
                    }
                }

                if (e.Message.Text.StartsWith("/valor", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("\n" + e.Message.Text);
                    WebClient getBitcoinPrice = new WebClient();
                    StringBuilder mensagemPrice = new StringBuilder();
                    DateTimeOffset agoraBrasilia = DateTime.UtcNow;
                    dynamic ticker = JsonConvert.DeserializeObject(getBitcoinPrice.DownloadString("https://blockchain.info/ticker"));
                    mensagemPrice.AppendLine("Hoje, " + agoraBrasilia.Date.ToShortDateString() + ", " + agoraBrasilia.Hour.ToString().PadLeft(2, '0') + ':' + agoraBrasilia.Minute.ToString().PadLeft(2, '0') + " (UTC)");
                    mensagemPrice.AppendLine("*Um bitcoin* vale *R$ " + ticker.BRL.last + '*');
                    mensagemPrice.AppendLine("*Um bitcoin* vale *U$ " + ticker.USD.last + '*');
                    mensagemPrice.AppendLine("*1 real* vale só *BTC " + Math.Round(1 / Convert.ToDouble(ticker.BRL.last), 8).ToString("0" + '.' + "###############") + '*');

                    telegramEnviarMensagem(e.Message.Chat.Id, mensagemPrice.ToString());
                }

                if (e.Message.Text.StartsWith("/medoeganancia", StringComparison.OrdinalIgnoreCase) || (e.Message.Text.StartsWith("/fg", StringComparison.OrdinalIgnoreCase)) || (e.Message.Text.StartsWith("/sentimento", StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine("\n" + e.Message.Text);
                    telegramEnviarMensagem(e.Message.Chat.Id, "Medo e ganância do cryptomercado[⠀](https://alternative.me/crypto/fear-and-greed-index.png)");
                }

                if (e.Message.Text.StartsWith("/global", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("\n" + e.Message.Text);
                    StringBuilder mensagem = new StringBuilder();
                    DateTimeOffset agoraUTC = DateTime.UtcNow;
                    dynamic ticker = JsonConvert.DeserializeObject(new WebClient().DownloadString("https://api.coinpaprika.com/v1/global"));

                    mensagem.AppendLine("*Dados Globais Criptomercado*").AppendLine();
                    mensagem.AppendLine("`" + agoraUTC.Date.ToShortDateString() + ", " + agoraUTC.Hour.ToString("##").PadLeft(2, '0') + ':' + agoraUTC.Minute.ToString("##").PadLeft(2, '0') + " (UTC)`");
                    mensagem.AppendLine("*Marketcap:* U$ `" + ticker.market_cap_usd + ".00`");
                    mensagem.AppendLine("*Volume 1D:* U$ `" + ticker.volume_24h_usd + ".00`");
                    mensagem.AppendLine("*Dominance:* " + ticker.bitcoin_dominance_percentage + " %");
                    mensagem.AppendLine("*Moedas Catalogadas:* " + ticker.cryptocurrencies_number);
                    mensagem.AppendLine("*Mudança Marketcap 24h:* " + ticker.market_cap_change_24h + '%');
                    mensagem.AppendLine("*Mudança Volume em 24h:* " + ticker.volume_24h_change_24h + '%');


                    telegramEnviarMensagem(e.Message.Chat.Id, mensagem.ToString());
                }
            }
        }


        static string getCoinID(string busca, dynamic coinList)
        {
            for (int i = 0; busca[i] < ((JArray)busca).Count;)
            {
                if (String.Equals(coinList[i].id, busca, StringComparison.OrdinalIgnoreCase) | String.Equals(coinList[i].name, busca, StringComparison.OrdinalIgnoreCase) | String.Equals(coinList[i].symbol, busca, StringComparison.OrdinalIgnoreCase))
                {
                    return coinList[i].id;
                }
            }
            return null;
        }


        public static void telegramEnviarMensagem(Telegram.Bot.Types.ChatId chatID, string mensagem)
        {
            botClient.SendTextMessageAsync(chatID, mensagem, Telegram.Bot.Types.Enums.ParseMode.Markdown);

            Console.WriteLine("Mensagem enviada para " + getChatNome(chatID) + " (" + chatID + ')');
        }

        static string getChatNome(Telegram.Bot.Types.ChatId chatID)
        {
            string nomeChat = null;

            try
            {
                Telegram.Bot.Types.Chat chat = botClient.GetChatAsync(chatID).Result;

                if (chat.Type == Telegram.Bot.Types.Enums.ChatType.Channel || chat.Type == Telegram.Bot.Types.Enums.ChatType.Group || chat.Type == Telegram.Bot.Types.Enums.ChatType.Supergroup && chat.Title.ToString().Length > 0)
                {
                    nomeChat += chat.Title;
                }
                if (chat.Type == Telegram.Bot.Types.Enums.ChatType.Private && chat.FirstName.Length > 0)
                {
                    if (!String.IsNullOrEmpty(nomeChat)) { nomeChat += ' '; }
                    nomeChat += chat.FirstName;
                }
                if (chat.Type == Telegram.Bot.Types.Enums.ChatType.Private && chat.LastName.Length > 0)
                {
                    if (!String.IsNullOrEmpty(nomeChat)) { nomeChat += ' '; }
                    nomeChat += chat.LastName;
                }
            }
            catch
            {
                nomeChat = "indefinido";
            }

            return nomeChat;
        }

        public static DateTimeOffset UnixTimeStampToDateTime(long unixTimeStamp)
        {
            System.DateTimeOffset dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp);
            return dtDateTime;
        }

    }
}
