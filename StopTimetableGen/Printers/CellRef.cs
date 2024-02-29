using System;
using System.Linq;

namespace StopTimetableGen.Printers
{
    /// <summary>
    /// Odkaz na buňku v Excelu (umí se transformovat z formátu používaném v excelu).
    /// Pozor, indexuje se od 1.
    /// </summary>
    struct CellRef
    {
        /// <summary>
        /// Řádek. Indexuje se od 1.
        /// </summary>
        public int Row { get; set; }

        /// <summary>
        /// Sloupec. Indexuje se od 1 (1 = 'A')
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// Vytvoří odkaz na buňku posunutý oproti tomuto
        /// </summary>
        /// <param name="rowDelta">Počet políček směrem dolů (záporné číslo = nahoru)</param>
        /// <param name="columnDelta">Počet políček směrem vpravo (záporné číslo = vlevo)</param>
        /// <returns>Nová instance obsahující vypočtené koordináty.</returns>
        public CellRef Move(int rowDelta, int columnDelta)
        {
            return new CellRef()
            {
                Row = Row + rowDelta,
                Column = Column + columnDelta,
            };
        }

        /// <summary>
        /// Vytvoří instanci na základě excelovského označení buňky. Např. A1 = (1,1), Z15 = (15, 26), AA11 = (11, 27) apod.
        /// </summary>
        /// <param name="cell">Označení buňky</param>
        /// <returns>Odkaz na buňku</returns>
        public static CellRef FromCellCode(string cell)
        {
            int separatorIndex = 0;
            while (!char.IsDigit(cell[separatorIndex]))
            {
                if (!char.IsLetter(cell[separatorIndex]))
                    throw new FormatException($"Cell code {cell} contains non-letter in column identification");
                separatorIndex++;
                if (separatorIndex >= cell.Length)
                    throw new FormatException($"Cell code {cell} does not contain row number");
            }

            if (separatorIndex <= 0)
            {
                throw new FormatException($"Cell code {cell} does not begin with a letter");
            }

            for (int i = separatorIndex; i < cell.Length; i++)
            {
                if (!char.IsDigit(cell[i]))
                    throw new FormatException($"Cell code {cell} contains non-digit in row number");
            }

            var colStr = cell.Substring(0, separatorIndex);
            var rowNum = cell.Substring(separatorIndex);

            return new CellRef()
            {
                Column = ColumnCodeToNumber(colStr),
                Row = int.Parse(rowNum),
            };
        }

        /// <summary>
        /// Přeloží označení sloupečku z excelovského tvaru (písmena) na číslo sloupce (A = 1, Z = 26, AA = 27, BA = 53 atd.)
        /// </summary>
        /// <param name="column">Označení sloupečku</param>
        /// <returns>Číslo sloupečku (indexované od 1 = A)</returns>
        public static int ColumnCodeToNumber(string column)
        {
            int result = 0;
            var multiplier = 1;
            foreach (var letter in column.Reverse())
            {
                result += (char.ToUpper(letter) - 'A' + 1) * multiplier;
                multiplier *= 26;
            }

            return result;
        }
    }
}
