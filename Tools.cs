using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;

namespace Tilapia
{
    internal class Tools
    {
        public static string TFSN(string estado)
        {
            if (String.Equals(estado, "true", StringComparison.OrdinalIgnoreCase))
            {
                return "sim";
            }
            else
            {
                return "não";
            }
        }

        public static string GetCoinID(string busca, dynamic coinList)
        {
            for (int i = 0; i < ((JArray)coinList).Count; i++)
            {
                if (String.Equals(coinList[i].id.ToString(), busca, StringComparison.OrdinalIgnoreCase) | String.Equals(coinList[i].name.ToString(), busca, StringComparison.OrdinalIgnoreCase) | String.Equals(coinList[i].symbol.ToString(), busca, StringComparison.OrdinalIgnoreCase))
                {
                    return coinList[i].id;
                }
            }
            return null;
        }

        public static dynamic LastMarketDataAwesomeApi(string[] pairs)
        {
            return JsonConvert.DeserializeObject(new WebClient().DownloadString("https://economia.awesomeapi.com.br/json/last/" + string.Join(',', pairs)));
        }

        public static List<string> listaDeXingamentosCultos = new List<string>() { "abantesma", "bonifrate", "concupiscente", "dendroclasta", "espurco", "futre", "grasnador", "histrião", "intrujão", "jacobeu", "liliputiano", "misólogo", "nóxio", "obnubilado", "peralvilho", "quebra-louças", "réprobo", "soez", "traga-mouros", "usurário", "valdevinos", "xenômano", "zoantropo" };

        public static string GerarXingamentoGratuito(int tgId = 0)
        {
            switch (new Random().Next(5))
            {
                case 1:
                    {
                        return "Todos sabem o quão " + listaDeXingamentosCultos[new Random().Next(listaDeXingamentosCultos.Count)] + " o " + "[Agente Álvaro](t.me/CypherSecoficial)" + " é";
                    }

                case 2:
                    {
                        return "[Agente Álvaro](t.me/CypherSecoficial)" + " só não é mais " + listaDeXingamentosCultos[new Random().Next(listaDeXingamentosCultos.Count)] + " por falta de intelecto";
                    }

                case 3:
                    {
                        return "[Agente Álvaro](t.me/CypherSecoficial)" + " deixe de ser tão " + listaDeXingamentosCultos[new Random().Next(listaDeXingamentosCultos.Count)];
                    }

                case 4:
                    {
                        return "Além de " + listaDeXingamentosCultos[new Random().Next(0, listaDeXingamentosCultos.Count / 2)] + " o " + "[Agente Álvaro](t.me/CypherSecoficial)" + " também é " + listaDeXingamentosCultos[new Random().Next(listaDeXingamentosCultos.Count / 2, listaDeXingamentosCultos.Count)];
                    }

                default:
                    {
                        return null;
                    }
            }
        }

        public static DateTimeOffset UnixTimeStampToDateTime(long unixTimeStamp)
        {
            System.DateTimeOffset dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp);
            return dtDateTime;
        }
    }
}