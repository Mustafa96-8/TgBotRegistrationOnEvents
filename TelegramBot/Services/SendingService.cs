using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using TelegramBot.Domain.Entities;
using Telegram.Bot;
using TelegramBot.Handlers;
using DocumentFormat.OpenXml.Spreadsheet;
using static TelegramBot.Domain.Enums.Messages;
using TelegramBot.Contracts;
using TelegramBot.Helpers;

namespace TelegramBot.Services;
public class SendingService
{
    private readonly PersonService personService; 
    private readonly ITelegramBotClient bot;
    private readonly ILogger<UpdateHandler> logger;

    public SendingService(PersonService personService, ITelegramBotClient bot, ILogger<UpdateHandler> logger)
    {
        this.personService = personService;
        this.bot = bot;
        this.logger = logger;
    }

#region Get Info
    public async Task<Message> SendSimpleMessage(Message msg, string _text, InlineKeyboardMarkup _replyMarkup, CancellationToken cancellationToken)
    {
        return await bot.SendMessage(
         chatId: msg.Chat.Id,
         text: _text,
         replyMarkup: _replyMarkup,
         cancellationToken: cancellationToken
        );

    }

    public async Task<Message> SendMessage(Person person, string _text, InlineKeyboardMarkup _replyMarkup, CancellationToken cancellationToken)
    {
        var sentMessage = await bot.SendMessage(
         chatId: person.Id,
         text: _text,
         replyMarkup: _replyMarkup,
         cancellationToken: cancellationToken
        );

        // Сохраняем ID последнего сообщения с профилем
        person.LastProfileMessageId = sentMessage.MessageId;
        await personService.Update(person, cancellationToken);
        return sentMessage;
    }

    public async Task<Message> SendFile(long chatId, IEnumerable<UserProfileResponse> listOfUsers, string fileName) 
    {
        Message message = await ExcelFileHelper<UserProfileResponse>.WriteFileToExcel(bot, chatId, listOfUsers, fileName);
        return message;
    }


    public async Task<Message> EditOrSendMessage(Message msg, Person person, string _text, InlineKeyboardMarkup _replyMarkup, CancellationToken cancellationToken)
    {

        // Проверка, является ли сообщение с профилем последним в переписке
        if (person.LastProfileMessageId.HasValue && person.LastProfileMessageId == msg.Id)
        {
            try
            {
                // Пытаемся обновить ранее отправленное сообщение
                await bot.EditMessageText(
                    chatId: msg.Chat.Id,
                    messageId: person.LastProfileMessageId.Value,
                    text: _text,
                    replyMarkup: _replyMarkup,
                    cancellationToken: cancellationToken
                );

                return msg;
            }
            catch (ApiRequestException ex)
            {
                // Если сообщение не удалось обновить (например, оно было удалено), отправляем новое
                if (ex.ErrorCode == 400)
                {
                    person.LastProfileMessageId = null;
                }
                else
                {
                    throw;
                }
            }
        }

        // Отправляем новое сообщение и сохраняем его ID
        var sentMessage = await bot.SendMessage(
            chatId: msg.Chat.Id,
            text: _text,
            replyMarkup: _replyMarkup,
            cancellationToken: cancellationToken
        );

        // Сохраняем ID последнего сообщения с профилем
        person.LastProfileMessageId = sentMessage.MessageId;
        await personService.Update(person, cancellationToken);

        return sentMessage;
    }
#endregion
}
