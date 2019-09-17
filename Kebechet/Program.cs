using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Telegram.Bot;

namespace Kebechet
{
    class Program
    {
        static string trend;
        static long lastTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        static TelegramBotClient botClient = new TelegramBotClient("");
        static dynamic anubisTrendAPIdata = buscarInformacoes();

        static void Main(string[] args)
        {
            Console.WriteLine("Kebechet Bot Iniciado " + UnixTimeStampToDateTime(lastTimestamp) + " (UTC)\n");
            botClient.OnMessage += botClient_OnMessage;
            botClient.StartReceiving();

            while (true)
            {
                try { buscarInformacoes(); } catch { }
                Thread.Sleep(30000);
            }
        }

        static void botClient_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            if (e.Message.Type == Telegram.Bot.Types.Enums.MessageType.Text)
            {
                if (e.Message.Text.StartsWith("/tendencia"))
                {
                    Console.WriteLine(e.Message.Text);
                    if (e.Message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private)
                    {
                        telegramEnviarMensagem(e.Message.Chat.Id, gerarMensagemTendencia(anubisTrendAPIdata, false), getChatNome(e.Message.Chat.Id));
                    }
                }

                if (e.Message.Text.StartsWith("/start"))
                {
                    Console.WriteLine(e.Message.Text);
                    if (e.Message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private)
                    {
                        telegramEnviarMensagem(e.Message.Chat.Id, @"Use /tendencia para verificar a tendência de preços do Bitcoin. *Os dados podem não ser precisos*", getChatNome(e.Message.Chat.Id));
                    }
                    else
                    {
                        telegramEnviarMensagem(e.Message.Chat.Id, @"Use /tendencia *no privado* para verificar a tendência de preços do Bitcoin. *Os dados podem não ser precisos*", getChatNome(e.Message.Chat.Id));
                    }
                }

                if (e.Message.Text.StartsWith("/valor"))
                {
                    Console.WriteLine(e.Message.Text);
                    WebClient getBitcoinPrice = new WebClient();
                    StringBuilder mensagemPrice = new StringBuilder();
                    dynamic ticker = JsonConvert.DeserializeObject(getBitcoinPrice.DownloadString("https://blockchain.info/ticker"));
                    mensagemPrice.AppendLine("Hoje, " + DateTime.UtcNow.Date.ToShortDateString() + ", " + DateTime.UtcNow.Hour.ToString().PadLeft(2, '0') + ':' + DateTime.UtcNow.Minute.ToString().PadLeft(2, '0') + "(UTC)");
                    mensagemPrice.AppendLine("*Um bitcoin* vale *R$ " + ticker.BRL.last + '*');
                    mensagemPrice.AppendLine("*Um bitcoin* vale *U$ " + ticker.USD.last + '*');
                    mensagemPrice.AppendLine("*1 real* vale só *BTC " + Math.Round(1 / Convert.ToDouble(ticker.BRL.last), 8).ToString("0" + '.' + "###############") + '*');

                    telegramEnviarMensagem(e.Message.Chat.Id, mensagemPrice.ToString(), getChatNome(e.Message.Chat.Id));
                }
            }
        }

        static dynamic buscarInformacoes()
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

            return anubisTrendAPIdata;
        }

        static void verificarReversao()
        {
            if (Convert.ToInt64(anubisTrendAPIdata.data[0].timestamp) > lastTimestamp && anubisTrendAPIdata.data[0].trend != anubisTrendAPIdata.data[1].trend | anubisTrendAPIdata.data[0].trend == trend)
            {
                Console.WriteLine("Reversão de tendência detectada");

                Telegram.Bot.Types.ChatId IDgrupoCafeESHA256 = -1001250570722;

                telegramEnviarMensagem(IDgrupoCafeESHA256, gerarMensagemTendencia(anubisTrendAPIdata, true).ToString(), getChatNome(IDgrupoCafeESHA256));

                lastTimestamp = Convert.ToInt64(anubisTrendAPIdata.data[0].timestamp);
            }
        }

        static string gerarMensagemTendencia(dynamic anubisAPIdata, bool reversao)
        {
            string tendenciaString = "ERRO";
            if (anubisAPIdata.data[0].trend == "LONG") { tendenciaString = "Alta"; } else if (anubisAPIdata.data[0].trend == "SHORT") { tendenciaString = "Baixa"; } else if (anubisAPIdata.data[0].trend == "NOTHING") { tendenciaString = "Incerta"; }

            StringBuilder mensagem = new StringBuilder();

            DateTimeOffset ultimaReversao = new DateTime();
            DateTimeOffset ultimaTrend = new DateTime();
            string ultimareversaoTexto = null;

            int dadosAnubisQtd = ((JArray)anubisAPIdata["data"]).Count;
            for (int i = 0; ((JArray)anubisAPIdata["data"]).Count > i && anubisAPIdata.data[0].trend == anubisAPIdata.data[i].trend; i++)
            {
                if ((i - 1) == ((JArray)anubisAPIdata["data"]).Count)
                {
                    ultimareversaoTexto = "Antes de ";
                }
                else
                {
                    ultimareversaoTexto = null;
                }

                ultimaReversao = UnixTimeStampToDateTime(Convert.ToInt64(anubisAPIdata.data[i].timestamp));
                ultimareversaoTexto = "Última reversão: " + ultimaReversao.Date.ToShortDateString() + ", " + ultimaReversao.Hour.ToString().PadLeft(2, '0') + ':' + ultimaReversao.Minute.ToString().PadLeft(2, '0') + " (UTC)";

            }

            ultimaTrend = UnixTimeStampToDateTime(Convert.ToInt64(anubisAPIdata.data[0].timestamp));

            if (reversao)
            {
                mensagem.AppendLine("*Reversão de Tendência*"); mensagem.AppendLine();
                mensagem.AppendLine("Horário: " + ultimaTrend.Date.ToShortDateString() + ", " + ultimaTrend.Hour.ToString().PadLeft(2, '0') + ':' + ultimaTrend.Minute.ToString().PadLeft(2, '0') + " (UTC)");
            }
            else
            {
                mensagem.AppendLine(ultimareversaoTexto);
            }

            mensagem.AppendLine("Par: BTC/USD");
            mensagem.AppendLine("Preço recente: " + anubisAPIdata.data[0].price);
            mensagem.AppendLine();
            mensagem.AppendLine("Tendência: *" + tendenciaString + "* (" + anubisAPIdata.data[0].trend + ")");

            return mensagem.ToString();
        }

        public static void telegramEnviarMensagem(Telegram.Bot.Types.ChatId chatID, string mensagem, string log)
        {
            Console.WriteLine("\nEnviando mensagem para " + log + " (" + chatID + ')');

            botClient.SendTextMessageAsync(chatID, mensagem, Telegram.Bot.Types.Enums.ParseMode.Markdown);
        }

        static string getChatNome(Telegram.Bot.Types.ChatId chatID)
        {
            Telegram.Bot.Types.Chat chat = botClient.GetChatAsync(chatID).Result;

            string nomeChat = null;

            if (chat.Type == Telegram.Bot.Types.Enums.ChatType.Channel || chat.Type == Telegram.Bot.Types.Enums.ChatType.Group || chat.Type == Telegram.Bot.Types.Enums.ChatType.Supergroup && chat.Title.ToString().Length > 0)
            {
                nomeChat += chat.Title;
            }
            if (chat.Type == Telegram.Bot.Types.Enums.ChatType.Private && chat.FirstName.ToString().Length > 0)
            {
                if (!String.IsNullOrEmpty(nomeChat)) { nomeChat += ' '; }
                nomeChat += chat.FirstName;
            }
            if (chat.Type == Telegram.Bot.Types.Enums.ChatType.Private && chat.LastName.ToString().Length > 0)
            {
                if (!String.IsNullOrEmpty(nomeChat)) { nomeChat += ' '; }
                nomeChat += ' ' + chat.LastName;
            }

            return nomeChat;
        }

        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp);
            return dtDateTime;
        }

    }
}
