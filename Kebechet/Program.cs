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
        static long lastTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        static WebClient webClient = new WebClient();
        static TelegramBotClient botClient = new TelegramBotClient("");

        static void Main(string[] args)
        {
            Console.WriteLine("Kebechet Bot Iniciado " + UnixTimeStampToDateTime(lastTimestamp) + " (UTC)\n\n");

            while (true)
            {
                try { buscarInformacoes(); } catch { }
                Thread.Sleep(30000);
            }
        }

        static void buscarInformacoes()
        {
            dynamic anubisAPIdata = JsonConvert.DeserializeObject(webClient.DownloadString("https://anubis.website/api/anubis/trend/"));

            if (anubisAPIdata.result == "success")
            {
                Console.WriteLine("Dados de API obtidos");
                verificarReversao(anubisAPIdata);
            }
            else
            {
                Console.WriteLine("Problemas com API Anubis");
            }
        }

        static void verificarReversao(dynamic anubisAPIdata)
        {
            if (Convert.ToInt64(anubisAPIdata.data[0].timestamp) > lastTimestamp && anubisAPIdata.data[0].trend != anubisAPIdata.data[1].trend)
            {
                Console.WriteLine("Reversão de tendência detectada");

                string tendenciaString = "ERRO";
                if (anubisAPIdata.data[0].trend == "LONG") { tendenciaString = "Alta"; } else if (anubisAPIdata.data[0].trend == "SHORT") { tendenciaString = "Baixa"; }
                StringBuilder mensagem = new StringBuilder();
                Console.Write(anubisAPIdata.data[0].timestamp.ToString() + "\n" + lastTimestamp);
                mensagem.AppendLine("*Reversão de Tendência*");
                mensagem.AppendLine();
                mensagem.AppendLine("Horário: " + UnixTimeStampToDateTime(Convert.ToInt64(anubisAPIdata.data[0].timestamp)) + "(UTC)");
                mensagem.AppendLine("Par: BTC/USD");
                mensagem.AppendLine("Preço: " + anubisAPIdata.data[0].price);
                mensagem.AppendLine();
                mensagem.AppendLine("Tendência: *" + tendenciaString + "* (" + anubisAPIdata.data[0].trend + ")");
                Console.Write("a");
                telegramEnviarMensagem(-1001250570722, mensagem.ToString());

                lastTimestamp = Convert.ToInt64(anubisAPIdata.data[0].timestamp);
            }
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
