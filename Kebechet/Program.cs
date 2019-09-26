using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Telegram.Bot;

namespace Kebechet
{
    internal static class Program
    {
        static string trend;
        static long lastTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        static TelegramBotClient botClient = new TelegramBotClient("");
        static dynamic anubisTrendAPIdata;

        static void Main(string[] args)
        {
            buscarInformacoes();

            Console.WriteLine("Kebechet Bot Iniciado " + UnixTimeStampToDateTime(lastTimestamp) + " (UTC)\n");
            botClient.OnMessage += botClient_OnMessage;
            botClient.StartReceiving();

            while (true)
            {
                buscarInformacoes();
                Thread.Sleep(30000);
            }
        }

        static void botClient_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            if (e.Message.Type == Telegram.Bot.Types.Enums.MessageType.Text)
            {
                if (e.Message.Text.StartsWith("/tendencia", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("\n" + e.Message.Text);
                    // if (e.Message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private)
                    {
                        telegramEnviarMensagem(e.Message.Chat.Id, gerarMensagemTendencia(anubisTrendAPIdata, false));
                    }
                }

                if (e.Message.Text.StartsWith("/sr", StringComparison.OrdinalIgnoreCase) || (e.Message.Text.StartsWith("/suportetendencia", StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine("\n" + e.Message.Text);
                    // if (e.Message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private)
                    {
                        telegramEnviarMensagem(e.Message.Chat.Id, gerarMensagemSuporteResistencia());
                    }
                }

                if (e.Message.Text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("\n" + e.Message.Text);
                    if (e.Message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private)
                    {
                        telegramEnviarMensagem(e.Message.Chat.Id, @"Use /tendencia para verificar a tendência de preços do Bitcoin. *Os dados podem não ser precisos*");
                    }
                    else
                    {
                        telegramEnviarMensagem(e.Message.Chat.Id, @"Use /tendencia *no privado* para verificar a tendência de preços do Bitcoin. *Os dados podem não ser precisos*");
                    }
                }

                if (e.Message.Text.StartsWith("/valor", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("\n" + e.Message.Text);
                    WebClient getBitcoinPrice = new WebClient();
                    StringBuilder mensagemPrice = new StringBuilder();
                    DateTimeOffset agoraBrasilia = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));
                    dynamic ticker = JsonConvert.DeserializeObject(getBitcoinPrice.DownloadString("https://blockchain.info/ticker"));
                    mensagemPrice.AppendLine("Hoje, " + agoraBrasilia.Date.ToShortDateString() + ", " + agoraBrasilia.Hour.ToString().PadLeft(2, '0') + ':' + agoraBrasilia.Minute.ToString().PadLeft(2, '0') + " (GMT -3)");
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
            }
        }

        static void buscarInformacoes()
        {
            WebClient webClient = new WebClient();

            anubisTrendAPIdata = JsonConvert.DeserializeObject(webClient.DownloadString("https://anubis.website/api/anubis/trend/"));

            if (anubisTrendAPIdata.result == "success")
            {
                Console.Write(".");
            }
            else
            {
                Console.WriteLine("Problemas com API Anubis");
            }

            if (String.IsNullOrEmpty(trend)) { trend = anubisTrendAPIdata.data[0].trend; }

            verificarReversao(anubisTrendAPIdata);
        }

        static void verificarReversao(dynamic APIdata)
        {
            if (Convert.ToInt64(APIdata.data[0].timestamp) > lastTimestamp && APIdata.data[0].trend != APIdata.data[1].trend | APIdata.data[0].trend != trend)
            {
                Console.WriteLine("Reversão de tendência detectada");

                Telegram.Bot.Types.ChatId IDgrupoCafeESHA256 = -1001250570722;

                telegramEnviarMensagem(IDgrupoCafeESHA256, gerarMensagemTendencia(anubisTrendAPIdata, true).ToString());

                lastTimestamp = Convert.ToInt64(anubisTrendAPIdata.data[0].timestamp);
            }
        }

        static string gerarMensagemTendencia(dynamic anubisAPIdata, bool reversao)
        {
            string tendenciaString = "ERRO";
            if (anubisAPIdata.data[0].trend == "LONG") { tendenciaString = "🔺 Alta"; } else if (anubisAPIdata.data[0].trend == "SHORT") { tendenciaString = "🔻 Baixa"; } else if (anubisAPIdata.data[0].trend == "NOTHING") { tendenciaString = "Indefinida"; }

            StringBuilder mensagem = new StringBuilder();
            mensagem.AppendLine("Tendência: *" + tendenciaString + "* (" + anubisAPIdata.data[0].trend + ")");
            mensagem.AppendLine();

            DateTimeOffset ultimaReversao = new DateTimeOffset();
            DateTimeOffset ultimaTrend = new DateTimeOffset();
            string ultimareversaoTexto = null;

            int dadosAnubisQtd = ((JArray)anubisAPIdata["data"]).Count;
            for (int i = 0; dadosAnubisQtd > i && anubisAPIdata.data[0].trend == anubisAPIdata.data[i].trend; i++)
            {
                ultimareversaoTexto = null;
                if (i + 1 == dadosAnubisQtd)
                {
                    ultimareversaoTexto = "Desde: Antes de ";
                }
                else
                {
                    ultimareversaoTexto = "Desde: ";
                }

                ultimaReversao = UnixTimeStampToDateTime(Convert.ToInt64(anubisAPIdata.data[i].timestamp));
                ultimareversaoTexto += ultimaReversao.Date.ToShortDateString() + ", " + ultimaReversao.Hour.ToString().PadLeft(2, '0') + ':' + ultimaReversao.Minute.ToString().PadLeft(2, '0') + " (UTC)";
            }

            ultimaTrend = UnixTimeStampToDateTime(Convert.ToInt64(anubisAPIdata.data[0].timestamp));

            if (reversao)
            {
                mensagem.AppendLine("*Reversão de Tendência*"); mensagem.AppendLine();
                mensagem.AppendLine(ultimaTrend.Date.ToShortDateString() + ", " + ultimaTrend.Hour.ToString().PadLeft(2, '0') + ':' + ultimaTrend.Minute.ToString().PadLeft(2, '0') + " (UTC)");
            }
            else
            {
                mensagem.AppendLine(ultimareversaoTexto);
            }

            mensagem.AppendLine("Par: BTC/USD");
            mensagem.AppendLine("Preço recente: " + anubisAPIdata.data[0].price);

            return mensagem.ToString();
        }

        static string gerarMensagemSuporteResistencia()
        {
            WebClient webClient = new WebClient();
            dynamic APIsr = JsonConvert.DeserializeObject(webClient.DownloadString("https://anubis.website/api/anubis/sr/"));

            DateTimeOffset ultimoDado = UnixTimeStampToDateTime(Convert.ToInt64(APIsr.data[0].timestamp));

            StringBuilder mensagem = new StringBuilder();

            mensagem.AppendLine(ultimoDado.Date.ToShortDateString() + ", " + ultimoDado.Hour.ToString().PadLeft(2, '0') + ':' + ultimoDado.Minute.ToString().PadLeft(2, '0') + " (UTC)");
            mensagem.AppendLine("Par: *BTC/USD*");
            mensagem.AppendLine("Suporte: *" + Math.Round(Convert.ToDouble(APIsr.data[0].s), 2) + '*');
            mensagem.AppendLine("Resistência: *" + Math.Round(Convert.ToDouble(APIsr.data[0].r), 2) + '*');

            return mensagem.ToString();
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
