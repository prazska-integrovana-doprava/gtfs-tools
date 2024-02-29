using GtfsModel.Extended;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StopTimetableGen.StopTimetableModel
{
    /// <summary>
    /// Poznámka v jízdním řádu (buď u odjezdu nebo u zastávky v seznamu zastávek)
    /// </summary>
    class TripVariantRemark : IRemark
    {
        /// <summary>
        /// Symbol, který poznámku reprezentuje u odjezdu nebo v seznamu zastávek
        /// </summary>
        public string Symbol { get; private set; }

        /// <summary>
        /// Textová vysvětlivka k symbolu
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Zastávky, kterých se poznámka týká (může být prázdné)
        /// </summary>
        public List<Stop> StopsInvolved { get; set; }

        public TripVariantRemark()
        {
            StopsInvolved = new List<Stop>();
        }

        public char SetLetter(List<char> availableLetters)
        {
            // zkoušíme po jednotlivých dotčených zastávkách postupně nejdřív první slovo, pak druhé atd.
            for (int wordIndex = 0; wordIndex < 5; wordIndex++)
            {
                foreach (var stop in StopsInvolved)
                {
                    var stopNameSplit = stop.Name.Split(new[] { ' ', '.', ',', '-', '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (stopNameSplit.Length > wordIndex)
                    {
                        var word = stopNameSplit[wordIndex];
                        var letter = GetPlainLetter(char.ToUpper(word[0]));
                        if (availableLetters.Contains(letter))
                        {
                            Symbol = letter.ToString();
                            return letter;
                        }
                    }
                }
            }

            // pokud jsme zde, tak žádná zastávka není zasažená, nebo žádné písmenko není volné,
            // defaultujeme na abecedu
            Symbol = availableLetters.First().ToString();
            return availableLetters.First();
        }

        private static char GetPlainLetter(char ch)
        {
            switch (ch)
            {
                case 'Á': return 'A';
                case 'Č': return 'C';
                case 'Ď': return 'D';
                case 'É': return 'E';
                case 'Ě': return 'E';
                case 'Í': return 'I';
                case 'Ň': return 'N';
                case 'Ó': return 'O';
                case 'Ř': return 'R';
                case 'Š': return 'S';
                case 'Ť': return 'T';
                case 'Ú': return 'U';
                case 'Ý': return 'Y';
                case 'Ž': return 'Z';
                default: return ch;
            }
        }
        
        public override string ToString()
        {
            return $"{Symbol} - {Text}";
        }
    }
}
