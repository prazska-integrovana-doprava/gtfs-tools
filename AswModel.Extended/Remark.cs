namespace AswModel.Extended
{
    /// <summary>
    /// Poznámky u spojů všeho druhu. Textové poznámky o všem možném i datové poznámky o garantovaných přestupech.
    /// </summary>
    public class Remark
    {
        /// <summary>
        /// Id poznámky v exportu ASW, při zpracování více souborů není unikátní, protože v každém souboru jsou spoje číslovány od 1.
        /// Pokud bychom to chtěli umět rozlišit, potřebovali bychom jako ID volit dvojici (string filename, int id)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// True, pokud jde o datovou návaznou poznámku. Za návazné aktuálně považujeme pouze poznámky typu "m",
        /// tzn. poznámky <i>já navazuji v zastávce <see cref="ToStop"/> na někoho, kdo přijede do <see cref="FromStop"/>
        /// na lince <see cref="FromRoute"/> ze směru <see cref="FromRouteStopDirection"/></i>.
        /// 
        /// Návazné poznámky typu "M" neřešíme (tj. na mě navazuje...), je jich jen pár a moc se nepoužívají 
        /// (typicky indikují že na náš spoj navazuje vlak, jsou zde uloženy, ale pouze jejich text)
        /// </summary>
        public bool IsTimedTransfer { get; set; }
        
        /// <summary>
        /// Text poznámky
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Zkratka1 poznámky
        /// </summary>
        public string Symbol1 { get; set; }

        /// <summary>
        /// Zastávka, ZE které přestupuji. Vyplněno pouze pokud je <see cref="IsTimedTransfer"/> = true.
        /// Nemůže být <see cref="Stop"/>, musí být StopRef, protože spoje mohou odkazovat na zastávky, které ve feedu zatím nejsou a budou přijoinovány později
        /// (typickým příkladem jsou vyčkávačky na vlaky, kdy vlakové zastávky nejsou součástí XML a připojí se až jako GTFS)
        /// </summary>
        public StopRef FromStop { get; set; }

        /// <summary>
        /// Zastávka, NA kterou přestupuji. Může být shodná s <see cref="FromStop"/>, pokud přestup probíhá u jednoho sloupku.
        /// Vyplněno pouze pokud je <see cref="IsTimedTransfer"/> = true.
        /// </summary>
        public VersionedItemByBitmap<Stop> ToStop { get; set; }
        
        /// <summary>
        /// Číslo linky, na kterou se vyčkává.
        /// Vyplněno pouze u datových návazných poznámek, jinak nedefinováno.
        /// </summary>
        public int FromRouteLineNumber { get; set; }

        /// <summary>
        /// Ze kterého směru má spoj linky <see cref="FromRoute"/> do zastávky <see cref="FromStop"/> přijet.
        /// Společně s ostatními daty se z toho pak odvodí <see cref="FromTrip"/>.
        /// Vyplněno pouze u datových návazných poznámek.
        /// </summary>
        public StopRef FromRouteStopDirection { get; set; }

        /// <summary>
        /// Minimální čas na přestup v sekundách (kolik času musí být mezi příjezdem spoje a odjezdem navazujícího spoje, aby mohla být vazba realizována).
        /// Vyplněno pouze u datových návazných poznámek, jinak nedefinováno.
        /// </summary>
        public int MinimumTransferTimeSeconds { get; set; }

        /// <summary>
        /// Nastavená maximální vyčkávací doba (jak dlouho návazný spoj nejvýše čeká)
        /// </summary>
        public int MaximumWaitingTimeSeconds { get; set; }

        public override string ToString()
        {
            return $"({Id}) {Text}";
        }
    }
}
