using Google.Apis.Sheets.v4.Data;

namespace ConsoleApp
{
    static class CellFormats
    {
        public static (CellFormat, string Fields) Minus90DegreeRotateFormat = (new CellFormat
        {
            TextRotation = new TextRotation { Angle = -90 },
        }, "UserEnteredFormat(TextRotation)");

        public static (CellFormat CellFormat, string Fields) BoldFormat = (new CellFormat
        {
            TextFormat = new TextFormat { Bold = true },
        }, "UserEnteredFormat(TextFormat)");

        public static (CellFormat CellFormat, string Fields) RightHorizontalAlignmentFormat = (new CellFormat
        {
            HorizontalAlignment = "RIGHT"
        }, "UserEnteredFormat(HorizontalAlignment)");

        public static (CellFormat CellFormat, string Fields) LeftHorizontalAlignmentFormat = (new CellFormat
        {
            HorizontalAlignment = "LEFT"
        }, "UserEnteredFormat(HorizontalAlignment)");

        public static (CellFormat CellFormat, string Fields) CenterHorizontalAlightmentFormat = (new CellFormat
        {
            HorizontalAlignment = "CENTER"
        }, "UserEnteredFormat(HorizontalAlignment)");
    }
}
