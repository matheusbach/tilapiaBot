using Newtonsoft.Json;
using System;
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
        static WebClient webClient = new WebClient();
        static TelegramBotClient botClient = new TelegramBotClient("");

        static void Main(string[] args)
        {
            Console.WriteLine("Kebechet Bot Iniciado " + UnixTimeStampToDateTime(lastTimestamp) + " (UTC)\n\n");
            botClient.OnMessage += botClient_OnMessage;
            botClient.StartReceiving();
            Console.WriteLine(botClient.MessageOffset);
           

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
                switch (e.Message.Text)
                {
                    case "/tendencia":
                        {
                            if (e.Message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private)
                            {
                                telegramEnviarMensagem(e.Message.Chat.Id, gerarMensagemTendencia(buscarInformacoes(), false));
                            }
                            Console.WriteLine(e.Message.Text);
                            break;
                        }

                    case "/start":
                        {
                            if (e.Message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private)
                            {
                                telegramEnviarMensagem(e.Message.Chat.Id, @"Use /tendencia para verificar a tendência de preços do Bitcoin. *Os dados podem não ser precisos*");
                            }
                            else
                            {
                                telegramEnviarMensagem(e.Message.Chat.Id, @"Use /tendencia *no privado* para verificar a tendência de preços do Bitcoin. *Os dados podem não ser precisos*");
                            }
                            Console.WriteLine(e.Message.Text);
                            break;
                        }
                }
            }
        }

        static dynamic buscarInformacoes()
        {
            dynamic anubisAPIdata = JsonConvert.DeserializeObject(webClient.DownloadString("https://anubis.website/api/anubis/trend/"));

            if (anubisAPIdata.result == "success")
            {
                Console.WriteLine("Dados de API obtidos");
            }
            else
            {
                Console.WriteLine("Problemas com API Anubis");
            }

            if (String.IsNullOrEmpty(trend)) { trend = anubisAPIdata.data[0].trend; }

            return anubisAPIdata;
        }

        static void verificarReversao()
        {
            dynamic anubisAPIdata = buscarInformacoes();

            if (Convert.ToInt64(anubisAPIdata.data[0].timestamp) > lastTimestamp && anubisAPIdata.data[0].trend != anubisAPIdata.data[1].trend | anubisAPIdata.data[0].trend == trend)
            {
                Console.WriteLine("Reversão de tendência detectada");

                telegramEnviarMensagem(-1001250570722, gerarMensagemTendencia(anubisAPIdata, true).ToString());

                lastTimestamp = Convert.ToInt64(anubisAPIdata.data[0].timestamp);
            }
        }

        static string gerarMensagemTendencia(dynamic anubisAPIdata, bool reversao)
        {
            string tendenciaString = "ERRO";
            if (anubisAPIdata.data[0].trend == "LONG") { tendenciaString = "Alta"; } else if (anubisAPIdata.data[0].trend == "SHORT") { tendenciaString = "Baixa"; } else if (anubisAPIdata.data[0].trend == "NOTHING") { tendenciaString = "Incerta"; }

            StringBuilder mensagem = new StringBuilder();

            if (reversao) { mensagem.AppendLine("*Reversão de Tendência*"); mensagem.AppendLine(); }

            mensagem.AppendLine("Horário: " + UnixTimeStampToDateTime(Convert.ToInt64(anubisAPIdata.data[0].timestamp)) + "(UTC)");
            mensagem.AppendLine("Par: BTC/USD");
            mensagem.AppendLine("Preço: " + anubisAPIdata.data[0].price);
            mensagem.AppendLine();
            mensagem.AppendLine("Tendência: *" + tendenciaString + "* (" + anubisAPIdata.data[0].trend + ")");

            return mensagem.ToString();
        }

        public static void telegramEnviarMensagem(long chatID, string mensagem)
        {
            Console.WriteLine("Enviando mensagem para " + chatID);

            botClient.SendTextMessageAsync(chatID, mensagem, Telegram.Bot.Types.Enums.ParseMode.Markdown);
        }

        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp);
            return dtDateTime;
        }

    }
}
