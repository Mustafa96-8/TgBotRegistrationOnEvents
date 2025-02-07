using Console.Advanced.Abstract;
using Telegram.Bot;
using TelegramBot.Handlers;

namespace TelegramBot.Services.TelegramServices;

// Compose Receiver and UpdateHandler implementation
public class ReceiverService(ITelegramBotClient botClient, UpdateHandler updateHandler, ILogger<ReceiverServiceBase<UpdateHandler>> logger)
    : ReceiverServiceBase<UpdateHandler>(botClient, updateHandler, logger);