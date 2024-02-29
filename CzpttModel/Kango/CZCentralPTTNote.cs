namespace CzpttModel.Kango
{
    /// <summary>
    /// Centrální poznámka k vlaku
    /// </summary>
    public class CZCentralPTTNote : NetworkSpecificParameterBase, IStationRangeParam
    {
        public CZCentralPTTNote(NameAndValuePair nameAndValuePair) : base(nameAndValuePair)
        {
        }

        /// <summary>
        /// Kód - Číslo centrální poznámky (vybrané konstanty v <see cref="CentralNoteCode"/>, kompletní seznam viz dokumentace SŽ)
        /// </summary>
        public CentralNoteCode Code
        {
            get { return (CentralNoteCode)GetAsInt(0); }
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
        /// 0 - poznámka se vztahuje k odjezdu z daného bodu.
        /// 1 - poznámka se vztahuje k příjezdu do daného bodu.
        /// </summary>
        public int AppliesToArrival
        {
            get { return GetAsIntOrDefault(5); }
            set { Set(5, value, 0); }
        }

        /// <summary>
        /// ID kalendáře použitého v elementu CZCalendarPTTNote. V případě, že poznámka bude mít stejný kalendář platnosti jako žádost, tento element nebude vyplněn. (např. 1, 9999)
        /// </summary>
        public int CalendarID
        {
            get { return GetAsIntOrDefault(6); }
            set { Set(6, value, 0); }
        }

        /// <summary>
        /// Vytvoří hlubokou kopii poznámky
        /// </summary>
        public CZCentralPTTNote Clone()
        {
            return new CZCentralPTTNote(NameAndValuePair.Clone());
        }
    }
}
