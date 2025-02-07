using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBot.Helpers;
public static class CsvFileHelper<T> where T : class
{
    public static async Task<Message> WriteFileToCsv(ITelegramBotClient bot, long chatId, IEnumerable<T> listOfObjects,string fileName)
    {

        using var memoryStream = new MemoryStream();
        using (var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";"
            };
            using var csvWriter = new CsvWriter(streamWriter, configuration: config);
            csvWriter.WriteRecords(listOfObjects);
        }
        memoryStream.Position = 0;
        var inputFile = new InputFileStream(memoryStream, fileName+".csv");
        return await bot.SendDocument(chatId, inputFile, caption: "CSV файл с Зарегестрированными участниками");

    }
}
