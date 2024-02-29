using GtfsModel.Extended;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StopTimetableGen.StopTimetableModel
{
    /// <summary>
    /// Jeden záznam (řádek) o zastávce uvnitř zastávkového jízdního řádu
    /// </summary>
    class StopOnLine
    {
        /// <summary>
        /// Rozlišení minulé, aktuální a budoucí zastávky
        /// </summary>
        public enum StopClass
        {
            PastStop = -1,
            CurrentStop = 0,
            FutureStop = 1,
        }

        /// <summary>
        /// Indikace zastávky na znamení
        /// </summary>
        public enum OnRequestStatus
        {
            StopsAlways = 0,
            OnRequest = 1,
        }

        /// <summary>
        /// Další vlastnosti zastávky
        /// </summary>
        public enum StopFlags
        {
            /// <summary>
            /// Žádná speciální vlastnost
            /// </summary
            None = 0,

            /// <summary>
            /// První zastávka na trase linky
            /// </summary>
            FirstStop = 1,

            /// <summary>
            /// Poslední zastávka na trase linky
            /// </summary>
            LastStop = 2,

            /// <summary>
            /// Existují spoje, pro které je tato zastávka konečná
            /// </summary>
            SomeTripsEndHere = 4,
        }

        /// <summary>
        /// Zastávka, kterou záznam reprezentuje
        /// </summary>
        public Stop Stop { get; set; }
        
        /// <summary>
        /// Název zastávky tak jak má být vytištěn do JŘ
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Počet minut, kolik trvá cesta z aktuální zastávky (pro kterou je generovaný JŘ) do této zastávky
        /// </summary>
        public int TravelTimeMinutes { get; set; }

        /// <summary>
        /// Rozlišení minulé, aktuální a budoucí zastávky
        /// </summary>
        public StopClass StopClassification { get; set; }

        /// <summary>
        /// Rozlišení zastávky na znamení (má křížek)
        /// </summary>
        public OnRequestStatus OnRequestMode { get; set; }

        /// <summary>
        /// Další vlastnosti zastávky na trase
        /// </summary>
        public StopFlags Flags { get; set; }

        /// <summary>
        /// Poznámky k zastávce v jízdním řádu
        /// </summary>
        public List<IRemark> Remarks { get; set; }

        /// <summary>
        /// Pásmo zastávky
        /// </summary>
        public string Zones { get; set; }

        public StopOnLine(Stop stop)
        {
            Stop = stop;
            Name = stop.Name;
            Zones = stop.ZoneId;
            Remarks = new List<IRemark>();
        }

        public override string ToString()
        {
            var remarksStr = string.Join("", Remarks.Select(r => r.Symbol).ToArray());
            switch (StopClassification)
            {
                case StopClass.PastStop:
                    return $"   {Name} {remarksStr} [{Zones}]";

                case StopClass.CurrentStop:
                    return $" - {Name} {remarksStr} [{Zones}]";

                case StopClass.FutureStop:
                    return $"{TravelTimeMinutes,2} {Name} {remarksStr} [{Zones}]";

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
