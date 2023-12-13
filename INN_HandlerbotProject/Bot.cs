using System;
using System.Configuration;
using Dadata;
using Dadata.Model;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

namespace INN_HandlerbotProject
{
    class Bot
    {
        private TelegramBotClient botClient;
        public string lastCommand = string.Empty;
        public void Run()
        {           

            botClient = new TelegramBotClient(ConfigurationManager.AppSettings.Get("token"));
            botClient.OnMessage += BotOnMessageReceived;

            botClient.StartReceiving();

        }

        private async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;

            if (message == null || message.Type != MessageType.Text)
                return;

            
            if (message.Text.ToLower().StartsWith("/inn"))
            {
                var innCommand = message.Text.Replace("/inn", "").Trim();
                var inns = innCommand.Split(' ');
                lastCommand = message.Text;
                foreach (var inn in inns)
                {
                    var data = InitilizeData(inn);

                    if (data.suggestions.Count == 0)
                    {
                        await botClient.SendTextMessageAsync(
                          chatId: message.Chat.Id,
                          text: $"Не было найдено компании по ИНН {inn}"
                        );
                        return;
                    }

                    foreach (var item in data.suggestions)
                    {
                        await botClient.SendTextMessageAsync(
                          chatId: message.Chat.Id,
                          text: $"🔍 Информация по ИНН {inn}:\n" +
                                $"🏢 Наименование компании: {item.value}\n" +
                                $"📍 Адрес компании: {item.data.address.value}"
                        );
                    }
                }
            }
            else if (message.Text.ToLower().StartsWith("/full"))
            {
                var innCommand = message.Text.Replace("/full", "").Trim();
                var data = InitilizeData(innCommand);

                foreach (var item in data.suggestions)
                {
                    await botClient.SendTextMessageAsync(
                      chatId: message.Chat.Id,
                      text: $"🔍 Полная информация по ИНН {innCommand}:\n" +
                            $"🏢 Наименование компании: {item.value}\n" +
                            $"🆔 ОГРН: {item.data.ogrn}\n" +
                            $"📅 Дата выдачи ОГРН: {item.data.ogrn_date}\n" +
                            $"📍 Адрес: {item.data.address.unrestricted_value}\n" +
                            $"👤 Руководитель: {item.data.management.name}\n" +
                            $"📊 Статус: {ComputedPartyState(item.data.state.status)}\n" +
                            $"🏢 Количество филиалов: {item.data.branch_count}"
                    );
                }
            }
            else if (message.Text.ToLower().StartsWith("/help"))
            {
                var helpMessage = "Справка по доступным командам:\n\n" +
                                  "/start – начать общение с ботом.\n" +
                                  "/help – вывести справку о доступных командах.\n" +
                                  "/hello – информация о авторе.\n" +
                                  "/inn [ИНН1 ИНН2 ...] – получить наименования и адреса компаний по ИНН. Можно указать несколько ИНН за одно обращение к боту.\n" +
                                  "/last – повторить последнее действие бота.\n" +
                                  "/full [ИНН] – по ИНН выводить подробную информацию об одной компании";

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: helpMessage
                );
            }
            else if (message.Text.StartsWith("/hello"))
            {
                var helloMessage = "Привет! Меня зовут Амир Абдурахимов. Вот мои контактные данные:\n" +
                                   "✉️ Email: tiam11@bk.ru\n" +
                                   "🔗 GitHub: [https://github.com/mr-abdurakhimov](https://github.com/mr-abdurakhimov)";

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: helloMessage,
                    parseMode: ParseMode.Markdown
                );
            }
            else if (message.Text.ToLower().StartsWith("/last"))
            {
                messageEventArgs.Message.Text = lastCommand;
                BotOnMessageReceived(sender, messageEventArgs);

            }
            else if (message.Text.ToLower().StartsWith("/start"))
            {
                lastCommand = "/start";
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Добрый день, {message.From.FirstName} ! Я бот, который по ИНН компании выдает информацию по этой компании !"
                );
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Я не знаю такой команды( Подробнее о моих командах можно узнать, написав мне '/help'"
                );
            }
        }

        private SuggestResponse<Party> InitilizeData(string INN)
        {
            var api = new SuggestClient(ConfigurationManager.AppSettings.Get("data_api"));

            var response = api.FindParty(INN);

            return response;
        }

        private string ComputedPartyState(PartyStatus status)
        {
            switch (status)
            {
                case PartyStatus.ACTIVE:
                    return "действующая";
                case PartyStatus.LIQUIDATING:
                    return "ликвидируется";
                case PartyStatus.LIQUIDATED:
                    return "ликвидирована";
                default:
                    return string.Empty;
            }
        }
    }
}
