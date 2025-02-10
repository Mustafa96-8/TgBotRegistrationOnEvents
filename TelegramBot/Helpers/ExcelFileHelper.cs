using Telegram.Bot;
using Telegram.Bot.Types;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace TelegramBot.Helpers;

public static class ExcelFileHelper<T> where T : class
{
    public static async Task<Message> WriteFileToExcel(ITelegramBotClient bot, long chatId, IEnumerable<T> listOfObjects, string fileName)
    {
        using var memoryStream = new MemoryStream();

        // Создаем Excel-файл в памяти
        using (SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook))
        {
            WorkbookPart workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());

            Sheets sheets = document.WorkbookPart.Workbook.AppendChild(new Sheets());
            Sheet sheet = new()
            {
                Id = document.WorkbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Data"
            };
            sheets.Append(sheet);

            SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

            // Получаем свойства класса
            var properties = typeof(T).GetProperties();

            // Заголовки
            Row headerRow = new();
            foreach (var prop in properties)
            {
                headerRow.Append(new Cell { CellValue = new CellValue(prop.Name), DataType = CellValues.String });
            }
            sheetData.Append(headerRow);

            // Запись данных
            foreach (var obj in listOfObjects)
            {
                Row dataRow = new();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(obj)?.ToString() ?? string.Empty;
                    dataRow.Append(new Cell { CellValue = new CellValue(value), DataType = CellValues.String });
                }
                sheetData.Append(dataRow);
            }

            workbookPart.Workbook.Save();
        }

        memoryStream.Position = 0;
        var inputFile = new InputFileStream(memoryStream, $"{fileName}.xlsx");
        return await bot.SendDocument(chatId, inputFile, caption: "Excel-файл с зарегистрированными участниками");
    }
}
