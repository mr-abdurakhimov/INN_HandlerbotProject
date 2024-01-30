using System;
using System.Collections.Generic;
using System.Configuration;
using Dadata;
using Dadata.Model;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace INN_HandlerbotProject
{
    class Bot
    {
        private Dictionary<long, Stack<string>> lastMessages = new Dictionary<long, Stack<string>>();
        private TelegramBotClient botClient;
        private bool fromLast;
        public void Run()
        {           

            botClient = new TelegramBotClient(ConfigurationManager.AppSettings.Get("token"));
            botClient.OnMessage += BotOnMessageReceived;

            botClient.StartReceiving();
            Console.WriteLine("Press any key to stop receiving messages...");
            Console.ReadKey();
            botClient.StopReceiving();
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
                foreach (var inn in inns)
                {
                    
                    var data = InitilizeData(inn);

                    if (data.suggestions.Count == 0)
                        {
                            SendWrongInnMessage(messageEventArgs, inn);
                            continue;
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
                if (data.suggestions.Count > 0)
                {
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
                else
                {
                    SendWrongInnMessage(messageEventArgs, innCommand);
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
                if (CheckLastCommand(message.From.Id))
                {
                    messageEventArgs.Message.Text = lastMessages[message.From.Id].Pop();
                    fromLast = true;
                    BotOnMessageReceived(sender, messageEventArgs);
                    return;
                }
                else
                    await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: $"Последние команды закончились! Используйте другие команды, чтобы /last стала доступна");
  
            }
            else if (message.Text.ToLower().StartsWith("/start"))
            {
                lastMessages[message.From.Id] = new Stack<string>();
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Добрый день, {message.From.FirstName}! Я бот, который по ИНН компании выдает информацию по этой компании!");
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Я не знаю такой команды( Подробнее о моих командах можно узнать, написав мне '/help'"
                );
            }
            if(message.Text.StartsWith('/') && !fromLast)
            {
                if(lastMessages.ContainsKey(message.From.Id))
                {
                    lastMessages[message.From.Id].Push(message.Text);
                }
                else {
                    await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Что-то пошло не так. Попробуйте перезапустить меня командой /start"
                );
                }
            }
            fromLast = false;
                
        }

        private async void SendWrongInnMessage(MessageEventArgs message,string inn)
        {
            await botClient.SendTextMessageAsync(
                              chatId: message.Message.Chat.Id,
                              text: $"Неправильный инн: {inn}"
                            );
        }
        private bool CheckLastCommand(long chat_id)
        {
            if (lastMessages.ContainsKey(chat_id))
                return lastMessages[chat_id].Count > 0;
            return false;
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
