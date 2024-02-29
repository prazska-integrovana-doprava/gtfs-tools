namespace CzpttModel.Kango
{
    /// <summary>
    /// Poznámka o IDS vlaku
    /// </summary>
    public class CZIPTS : NetworkSpecificParameterBase, IStationRangeParam
    {
        public CZIPTS(NameAndValuePair nameAndValuePair) : base(nameAndValuePair)
        {
        }

        /// <summary>
        /// Kód IDS z číselníku IDS, viz. příloha 8.8 (hodnoty 1-99)
        /// </summary>
        public IPTSCode Code
        {
            get { return (IPTSCode)GetAsInt(0); }
            set { Set(0, (int)value); }
        }

        public string FromStationCode
        {
            get { return Get(1); }
            set { Set(1, value); }
        }

        public int FromStationCodeOccurence
        {
            get { return GetAsIntOrDefault(2); }
            set { Set(2, value, 0); }
        }

        public string ToStationCode
        {
            get { return Get(3); }
            set { Set(3, value); }
        }

        public int ToStationCodeOccurence
        {
            get { return GetAsIntOrDefault(4); }
            set { Set(4, value, 0); }
        }

        /// <summary>
        /// ID kalendáře použitého v elementu CZCalendarIPTS. V případě, že IDS bude mít stejný kalendář platnosti jako žádost/DJŘ, tento element nebude vyplněn. (např. 1, 9999)
        /// </summary>
        public int CalendarID
        {
            get { return GetAsIntOrDefault(5); }
            set { Set(5, value, 0); }
        }

        /// <summary>
        /// Vytvoří hlubokou kopii poznámky
        /// </summary>
        public CZIPTS Clone()
        {
            return new CZIPTS(NameAndValuePair.Clone());
        }
    }
}
