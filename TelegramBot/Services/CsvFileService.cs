﻿using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBot.Services;
public static class CsvFileService<T> where T:class
{
    public static async Task<Message> WriteFileToCsv(ITelegramBotClient bot,long chatId,IEnumerable<T> listOfObjects)
    {

        using var memoryStream = new MemoryStream();
        using (var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8,leaveOpen:true))
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";"
            };
            using var csvWriter = new CsvWriter(streamWriter, configuration: config);
            csvWriter.WriteRecords(listOfObjects);
        }
        memoryStream.Position = 0;
        var inputFile = new InputFileStream(memoryStream, "data.csv");
        return await bot.SendDocument(chatId, inputFile, caption: "CSV файл с Зарегестрированными участниками");

    }
}
