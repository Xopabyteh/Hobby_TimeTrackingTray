using Excel = Microsoft.Office.Interop.Excel;

namespace Hobby_TimeTrackingTray;

public class ExcelConverter
{
    public static void TimeSheetToExcel(
        string timeSheetPath)
    {
        var app = new Excel.ApplicationClass
        {
            Visible = true
        };

        var workbook = app.Workbooks.Add();
        var sheet = (Excel.Worksheet) workbook.ActiveSheet;

        sheet.Cells[1, 1] = "Date";
        sheet.Cells[1, 2] = "Start time";
        sheet.Cells[1, 3] = "Stop time";
        sheet.Cells[1, 4] = "Work time";
        sheet.Cells[1, 5] = "Comment";

        var sheetLines = File.ReadAllLines(timeSheetPath);
        var tableRows = sheetLines.Length;
        for (var i = 0; i < sheetLines.Length; i++)
        {
            var line = sheetLines[i];
            var excelRow = i + 2; // + 1 for table header + 1 for indexing from one

            var values = line.Split(',');
            var date = values[0];
            var startTime = values[1];
            var stopTime = values[2];
            var workTime = values[3];
            var comment = values[4];

            sheet.Cells[excelRow, 1] = date;
            sheet.Cells[excelRow, 2] = startTime;
            sheet.Cells[excelRow, 3] = stopTime;
            sheet.Cells[excelRow, 4] = workTime;
            sheet.Cells[excelRow, 5] = comment;
        }

        // Format as table
        var tableRange = sheet.Range[sheet.Cells[1, 1], sheet.Cells[tableRows + 1, 5]];
        var table = sheet.ListObjects.Add(Excel.XlListObjectSourceType.xlSrcRange, tableRange, null, Excel.XlYesNoGuess.xlYes);
        table.Name = "TimeSheet";

        // Add work time sum
        sheet.Cells[tableRows + 2, 1] = "Total sessions";
        sheet.Cells[tableRows + 3, 1] = $"=COUNT(A2:A{tableRows + 1})";
        sheet.Range[$"A{tableRows + 3}", $"A{tableRows + 3}"].NumberFormat = "0";

        sheet.Cells[tableRows + 2, 4] = "Total work time";
        sheet.Cells[tableRows + 3, 4] = $"=SUM(D2:D{tableRows + 1})";
        sheet.Range[$"D{tableRows + 3}", $"D{tableRows + 3}"].NumberFormat = "[h]:mm:ss;@";

        var range = sheet.UsedRange;
        range.Columns.AutoFit();
        
        // Save the workbook
        workbook.SaveAs(timeSheetPath.Replace(".csv", ".xlsx"), Excel.XlFileFormat.xlOpenXMLWorkbook);
    }
}