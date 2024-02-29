using Microsoft.Office.Interop.Excel;

namespace StopTimetableGen.Printers
{
    static class COMExcelExtensions
    {
        /// <summary>
        /// Vrátí zadanou buňku. Row a col jsou indexy od 1
        /// </summary>
        public static Range GetCells(this Worksheet sheet, int row, int col)
        {
            return sheet.Cells[row, col];
        }

        /// <summary>
        /// Vrátí zadanou buňku podle odkazu
        /// </summary>
        public static Range GetCells(this Worksheet sheet, CellRef cell)
        {
            return GetCells(sheet, cell.Row, cell.Column);
        }

        /// <summary>
        /// Vrátí zadanou buňku podle označení
        /// </summary>
        public static Range GetCells(this Worksheet sheet, string cell)
        {
            var cellRef = CellRef.FromCellCode(cell);
            return GetCells(sheet, cellRef);
        }

        /// <summary>
        /// Vrátí zadaný rozsah buněk od <paramref name="origin"/> <paramref name="rows"/> řádků a <paramref name="columns"/> sloupců.
        /// Speciálně rows = 1 a columns = 1 vrátí jednu buňku.
        /// </summary>
        public static Range GetCells(this Worksheet sheet, CellRef origin, int rows, int columns)
        {
            return sheet.Range[GetCells(sheet, origin), GetCells(sheet, origin.Row + rows - 1, origin.Column + columns - 1)];
        }

        /// <summary>
        /// Vrátí zadaný rozsah buněk od <paramref name="origin"/> <paramref name="rows"/> řádků a <paramref name="columns"/> sloupců.
        /// Speciálně rows = 1 a columns = 1 vrátí jednu buňku.
        /// </summary>
        public static Range GetCells(this Worksheet sheet, CellRef origin, CellRef bottomRight)
        {
            return sheet.Range[GetCells(sheet, origin), GetCells(sheet, bottomRight)];
        }
    }
}
