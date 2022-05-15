using Newtonsoft.Json;
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace Tilapia
{
    public class TelegramHandlers
    {
        // 4 horas
        private const int NANO_LUZ_INTERVAL = 14400;

        private static long LastNanoLuz = 0;

        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                UpdateType.Message => BotOnMessageReceived(botClient, update.Message!),
                UpdateType.EditedMessage => BotOnMessageReceived(botClient, update.EditedMessage!),
                UpdateType.CallbackQuery => BotOnCallbackQueryReceived(botClient, update.CallbackQuery!),
                UpdateType.InlineQuery => BotOnInlineQueryReceived(botClient, update.InlineQuery!),
                UpdateType.ChosenInlineResult => BotOnChosenInlineResultReceived(botClient, update.ChosenInlineResult!),
                _ => UnknownUpdateHandlerAsync(botClient, update)
            };

            try
            {
                await handler;
            }
            catch (Exception exception)
            {
                await HandleErrorAsync(botClient, exception, cancellationToken);
            }
        }

        private static async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
        {
            if (message.Type != MessageType.Text)
                return;

            Task<Message> action;

            action = message.Text!.ToLowerInvariant().Normalize().Split(' ', '@')[0] switch
            {
                "/start" => Usage(botClient, message),
                "/help" => Usage(botClient, message),
                "/btc" => Valor(botClient, message),
                "/valor" => Valor(botClient, message),
                "/fg" => MedoEGanancia(botClient, message),
                "/fearandgreed" => MedoEGanancia(botClient, message),
                "/ping" => Ping(botClient, message),
                "/global" => Global(botClient, message),
                "/info" => InfoCoin(botClient, message),
                "/a" => PriceCoin(botClient, message),
                "/dolar" => DolarAmericano(botClient, message),
                "/euro" => EuroEuropeu(botClient, message),
                "/peso" => PesoArgentino(botClient, message),
                _ => null
            };

            if (action == null)
            {
                action = message.Text!.ToLowerInvariant().Normalize() switch
                {
                    string a when a.Contains("tilapia") || a.Contains("tilápia") => Tilapia(botClient, message),
                    string a when a.Contains("nano") => NanoLuz(botClient, message),
                    _ => null
                };
            }

            if (action == null) { return; }

            Console.WriteLine($"Telegram Bot: {message.Type}" + (message.Type == MessageType.Text ? ": " + message.Text : null));

            Message sentMessage = await action;
            Console.WriteLine($"The message was sent with id: {sentMessage.MessageId}");

            static async Task<Message> NanoLuz(ITelegramBotClient botClient, Message message)
            {
                long now = DateTimeOffset.Now.ToUnixTimeSeconds();
                if (now - LastNanoLuz < NANO_LUZ_INTERVAL)
                {
                    Console.WriteLine("⋰·⋰ = 🤫");
                    return message;
                }

                LastNanoLuz = now;
                Console.WriteLine("⋰·⋰ = 💡");

                int randomnumber = new Random().Next(0, 4);

                switch (randomnumber)
                {
                    case 0: return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: @"⋰·⋰ = 💡", parseMode: ParseMode.Markdown);

                    case 1: return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: @"*NANO é luz!*", parseMode: ParseMode.Markdown);

                    case 2: return await botClient.SendStickerAsync(chatId: message.Chat.Id, sticker: "CAACAgEAAxkBAAEB8TNgPmq4ThlL_SfjDsQ3hjk4NfjJ7wAC-gADO33ZRfzHd3_kBtX-HgQ", disableNotification: false, replyToMessageId: message.MessageId);

                    default: return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: @"*NANO é luz!* (quem apagou favor ligar de volta)", parseMode: ParseMode.Markdown);
                }
            }

            static async Task<Message> Tilapia(ITelegramBotClient botClient, Message message)
            {
                return await botClient.SendStickerAsync(chatId: message.Chat.Id, sticker: "CAADAQADAgADpcjpLxh-FFNqO1CJFgQ", disableNotification: false, replyToMessageId: message.MessageId);
            }

            static async Task<Message> Valor(ITelegramBotClient botClient, Message message)
            {
                Console.WriteLine("\n" + message.Text);
                WebClient getBitcoinPrice = new WebClient();
                StringBuilder mensagemPrice = new StringBuilder();
                DateTimeOffset agoraUTC = DateTime.UtcNow;
                dynamic ticker = JsonConvert.DeserializeObject(getBitcoinPrice.DownloadString("https://blockchain.info/ticker"));
                mensagemPrice.AppendLine("Hoje, " + agoraUTC.Date.ToShortDateString() + ", " + agoraUTC.Hour.ToString().PadLeft(2, '0') + ':' + agoraUTC.Minute.ToString().PadLeft(2, '0') + " (UTC)");
                mensagemPrice.AppendLine("*Um bitcoin* vale *R$ " + ticker.BRL.last + '*');
                mensagemPrice.AppendLine("*Um bitcoin* vale *U$ " + ticker.USD.last + '*');
                mensagemPrice.AppendLine("*1 real* vale só *BTC " + Math.Round(1 / Convert.ToDouble(ticker.BRL.last), 8).ToString("0" + '.' + "#########") + '*');

                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: mensagemPrice.ToString(), parseMode: ParseMode.Markdown);
            }

            static async Task<Message> MedoEGanancia(ITelegramBotClient botClient, Message message)
            {
                Console.WriteLine("\n" + message.Text);

                return await botClient.SendPhotoAsync(chatId: message.Chat.Id, photo: "https://alternative.me/crypto/fear-and-greed-index.png", caption: "Medo e ganância do cryptomercado");
            }

            static async Task<Message> Ping(ITelegramBotClient botClient, Message message)
            {
                Console.WriteLine("\n" + message.Text);
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "pong!", disableNotification: true);
            }

            static async Task<Message> Global(ITelegramBotClient botClient, Message message)
            {
                Console.WriteLine("\n" + message.Text);
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

                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: mensagem.ToString(), parseMode: ParseMode.Markdown);
            }

            static async Task<Message> InfoCoin(ITelegramBotClient botClient, Message message)
            {
                Console.WriteLine("\n" + message.Text);
                StringBuilder mensagem = new StringBuilder();
                DateTimeOffset agoraUTC = DateTime.UtcNow;

                string[] comando = message.Text.Split(' ', 2);

                if (comando.Length < 2)
                {
                    mensagem.Append("digite o código da moeda após o comando. Ex: `/info BTC`");
                }
                else
                {
                    string coinID = Tools.GetCoinID(comando[1], Program.coinList);

                    if (coinID == null)
                    {
                        return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "Moeda não encontrada. Certifique-se que digitou corretamento. Talvéz a moeda que você digitou não esteja listada em nosso indexador", parseMode: ParseMode.Markdown);
                    }

                    dynamic info = JsonConvert.DeserializeObject(new WebClient().DownloadString("https://api.coinpaprika.com/v1/coins/" + coinID));

                    if (info.rank.ToString() != "0") { mensagem.Append("*" + info.rank.ToString() + " - *"); }
                    mensagem.AppendLine("*" + info.name + " (" + info.symbol + ")*");
                    mensagem.AppendLine();
                    mensagem.AppendLine("_" + info.description + "_").AppendLine();
                    if (info.whitepaper.link != null) { mensagem.AppendLine("*Whitepaper*: [acessar](" + info.whitepaper.link + ")").AppendLine(); }
                    mensagem.AppendLine("*Detalhes:*");
                    if (info.type != null) { mensagem.AppendLine("Tipo: `" + info.type.ToString() + "`"); }
                    if (info.is_new != null) { mensagem.AppendLine("É nova: `" + Tools.TFSN(info.is_new.ToString()) + "`"); }
                    if (info.is_active != null) { mensagem.AppendLine("Está ativa: `" + Tools.TFSN(info.is_active.ToString()) + "`"); }
                    if (info.open_source != null) { mensagem.AppendLine("Open Source: `" + Tools.TFSN(info.open_source.ToString()) + "`"); }
                    if (info.hardware_wallet != null) { mensagem.AppendLine("Hardware Wallet: `" + Tools.TFSN(info.hardware_wallet.ToString()) + "`"); }
                    if (info.development_status != null) { mensagem.AppendLine("Estado de desenvolvimento: `" + info.development_status + "`"); }
                    if (info.org_structure != null) { mensagem.AppendLine("Estrutura Organizacional: `" + info.org_structure + "`"); }
                    if (info.hash_algorithm != null) { mensagem.AppendLine("Algoritmo de Hash: `" + info.hash_algorithm + "`"); }
                    if (info.proof_type != null) { mensagem.AppendLine("Proof type: `" + info.proof_type + "`"); }
                    mensagem.AppendLine();
                    if (info.links != null) { mensagem.AppendLine("*Links:*"); }
                    if (info.links.explorer != null) { mensagem.AppendLine("[explorer](" + info.links.explorer[0] + ")"); }
                    if (info.links.facebook != null) { mensagem.AppendLine("[facebook](" + info.links.facebook[0] + ")"); }
                    if (info.links.reddit != null) { mensagem.AppendLine("[reddit](" + info.links.reddit[0] + ")"); }
                    if (info.links.source_code != null) { mensagem.AppendLine("[código fonte](" + info.links.source_code[0] + ")"); }
                    if (info.links.website != null) { mensagem.AppendLine("[website](" + info.links.website[0] + ")"); }
                    if (info.links.youtube != null) { mensagem.AppendLine("[youtube](" + info.links.youtube[0] + ")"); }
                    if (info.links.medium != null) { mensagem.AppendLine("[medium](" + info.links.medium[0] + ")"); }
                }

                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: mensagem.ToString(), parseMode: ParseMode.Markdown);
            }

            static async Task<Message> PriceCoin(ITelegramBotClient botClient, Message message)
            {
                Console.WriteLine("\n" + message.Text);
                StringBuilder mensagem = new StringBuilder();
                DateTimeOffset agoraUTC = DateTime.UtcNow;

                string[] comando = message.Text.Split(' ', 2);

                if (comando.Length < 2)
                {
                    mensagem.Append("digite o código da moeda após o comando. Ex: `/a BTC`");
                }
                else
                {
                    string coinID = Tools.GetCoinID(comando[1], Program.coinList);

                    if (coinID == null)
                    {
                        return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "Moeda não encontrada. Certifique-se que digitou corretamento. Talvéz a moeda que você digitou não esteja listada em nosso indexador", parseMode: ParseMode.Markdown);
                    }

                    dynamic info = JsonConvert.DeserializeObject(new WebClient().DownloadString("https://api.coinpaprika.com/v1/tickers/" + coinID));

                    mensagem.AppendLine("*" + info.name + " (" + info.symbol + ")*");
                    mensagem.AppendLine("Rank: " + info.rank);
                    mensagem.AppendLine("ß: " + Math.Round(Convert.ToDouble(info.beta_value), 4));
                    mensagem.AppendLine("Price: U$ " + Math.Round(Convert.ToDouble(info.quotes.USD.price), 2) + " (" + Math.Round(Convert.ToDouble(info.quotes.USD.percent_change_24h), 2) + "%)");
                    mensagem.AppendLine("Price: R$ " + Math.Round(Convert.ToDouble(info.quotes.USD.price * (double)Tools.LastMarketDataAwesomeApi(new[] { "USD-BRL" }).USDBRL.ask), 2));
                }

                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: mensagem.ToString(), parseMode: ParseMode.Markdown);
            }

            static async Task<Message> DolarAmericano(ITelegramBotClient botClient, Message message)
            {
                Console.WriteLine("\n" + message.Text);
                StringBuilder mensagem = new StringBuilder();

                dynamic marketData = Tools.LastMarketDataAwesomeApi(new[] { "USD-BRL" });

                mensagem.AppendLine("*Dolar cotado em real: *`" + Math.Round((double)marketData.USDBRL.ask, 2).ToString() + "`");
                mensagem.AppendLine("*Real cotado em dolar: *`" + Math.Round(1 / (double)marketData.USDBRL.ask, 2).ToString() + "`");

                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: mensagem.ToString(), parseMode: ParseMode.Markdown);
            }

            static async Task<Message> EuroEuropeu(ITelegramBotClient botClient, Message message)
            {
                Console.WriteLine("\n" + message.Text);
                StringBuilder mensagem = new StringBuilder();

                dynamic marketData = Tools.LastMarketDataAwesomeApi(new[] { "EUR-BRL" });

                mensagem.AppendLine("*Euro cotado em real: *`" + Math.Round((double)marketData.EURBRL.ask, 2).ToString() + "`");
                mensagem.AppendLine("*Real cotado em euro: *`" + Math.Round(1 / (double)marketData.EURBRL.ask, 2).ToString() + "`");
                mensagem.AppendLine();
                try { mensagem.AppendLine(Tools.GerarXingamentoGratuito(1834100906)); } catch { }
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: mensagem.ToString(), parseMode: ParseMode.Markdown);
            }

            static async Task<Message> PesoArgentino(ITelegramBotClient botClient, Message message)
            {
                Console.WriteLine("\n" + message.Text);
                StringBuilder mensagem = new StringBuilder();

                dynamic marketData = Tools.LastMarketDataAwesomeApi(new[] { "ARS-BRL" });

                mensagem.AppendLine("*Peso cotado em real: *`" + Math.Round((double)marketData.ARSBRL.ask, 4).ToString() + "`");
                mensagem.AppendLine("*Real cotado em peso: *`" + Math.Round(1 / (double)marketData.ARSBRL.ask, 4).ToString() + "`");

                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: mensagem.ToString(), parseMode: ParseMode.Markdown);
            }

            static async Task<Message> Usage(ITelegramBotClient botClient, Message message)
            {
                const string usage = "Usage:\n" +
                                     "/help - Lista de comandos.\n" +
                                     "/a \\[coin] - Cotação da moeda.\n" +
                                     "/info \\[coin] - Informações da moeda.\n" +
                                     "/btc - Resumo de preço do bitcoin.\n" +
                                     "/global - Resumo global do mercado cripto.\n" +
                                     "/fg - Indicador Fear&Greed (Medo e Ganância).\n" +
                                     "/dolar - Valor do Dólar cotado em Reais.\n" +
                                     "/euro - Valor do Euro cotado em Reais.\n" +
                                     "/peso - Valor do Peso cotado em Reais.\n";

                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                            text: usage,
                                                            parseMode: ParseMode.Markdown,
                                                            replyMarkup: new ReplyKeyboardRemove());
            }
        }

        // Process Inline Keyboard callback data
        private static async Task BotOnCallbackQueryReceived(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: $"Received {callbackQuery.Data}");

            await botClient.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: $"Received {callbackQuery.Data}");
        }

        private static async Task BotOnInlineQueryReceived(ITelegramBotClient botClient, InlineQuery inlineQuery)
        {
            Console.WriteLine($"Received inline query from: {inlineQuery.From.Id}");

            InlineQueryResult[] results = {
			// displayed result
			new InlineQueryResultArticle(
                id: "3",
                title: "TgBots",
                inputMessageContent: new InputTextMessageContent(
                    "hello"
                )
            )
        };

            await botClient.AnswerInlineQueryAsync(inlineQueryId: inlineQuery.Id,
                                                   results: results,
                                                   isPersonal: true,
                                                   cacheTime: 0);
        }

        private static Task BotOnChosenInlineResultReceived(ITelegramBotClient botClient, ChosenInlineResult chosenInlineResult)
        {
            Console.WriteLine($"Received inline result: {chosenInlineResult.ResultId}");
            return Task.CompletedTask;
        }

        private static Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
        {
            Console.WriteLine($"Unknown update type: {update.Type}");
            return Task.CompletedTask;
        }
    }
}
