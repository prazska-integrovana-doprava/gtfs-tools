using System.Collections.Generic;
using System.Linq;

namespace StopTimetableGen.StopTimetableModel
{
    /// <summary>
    /// Jeden minutový odjezd v jízdním řádu
    /// </summary>
    class Departure
    {
        /// <summary>
        /// Minuta (hodnota 0-59)
        /// </summary>
        public int Minute { get; set; }

        /// <summary>
        /// Poznámky ke spoji
        /// </summary>
        public List<IRemark> Remarks { get; set; }

        /// <summary>
        /// True, pokud jde o nízkopodlažní spoj
        /// </summary>
        public bool IsWheelchairAccessible { get; set; }

        public Departure()
        {
            Remarks = new List<IRemark>();
        }

        public override string ToString()
        {
            return $"{Minute:00}{string.Join("", Remarks.Select(r => r.Symbol).ToArray())}";
        }
    }
}
