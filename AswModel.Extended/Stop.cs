using CommonLibrary;
using System.Linq;

namespace AswModel.Extended
{
    /// <summary>
    /// Zastávka z ASW JŘ.
    /// 
    /// Respektive jedna její verze platnosti (viz <see cref="VersionedItemByBitmap{T}"/>)
    /// </summary>
    public class Stop : IVersionableByBitmap
    {
        /// <summary>
        /// Číslo uzlu dle ASW JŘ
        /// </summary>
        public int NodeId { get; set; }

        /// <summary>
        /// Číslo sloupku dle ASW JŘ.
        /// </summary>
        public int StopId { get; set; }

        /// <summary>
        /// Platnost záznamu
        /// </summary>
        public ServiceDaysBitmap ServiceAsBits { get; set; }

        /// <summary>
        /// Začátek platnosti záznamu
        /// </summary>
        public RelativeDate ValidityStartDate { get { return ServiceAsBits.GetFirstDayOfService(); } }

        /// <summary>
        /// Stanoviště
        /// </summary>
        public string PlatformCode { get; set; }

        /// <summary>
        /// Název1 zastávky dle ASW JŘ (nemusí být unikátní)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Název2 zastávky dle ASW JŘ (nemusí být unikátní, ale měl by být bez různých přídomků typu "nábřeží", " - A" apod.).
        /// Používá se jako headsign.
        /// </summary>
        public string CommonName { get; set; }

        /// <summary>
        /// Název7 zastávky dle ASW JŘ (obsahuje typicky různé přípisky jako název ulice, okres apod.)
        /// </summary>
        public string IdosName { get; set; }

        /// <summary>
        /// Poloha zastávky na mapě
        /// </summary>
        public Coordinates Position { get; set; }

        /// <summary>
        /// Informace o pásmovém zařazení zastávky pro všechna IDS
        /// </summary>
        public ZoneInfo[] Zones { get; set; }

        /// <summary>
        /// Tarifní pásma PID dle číselníku ASW JŘ (mohou být oddělena čárkou)
        /// </summary>
        public string PidZoneId
        {
            get
            {
                var pidZoneRecord = Zones.FirstOrDefault(zi => zi.TariffSystemShortName == ZoneInfo.PIDSystemName);
                return pidZoneRecord?.ZoneId ?? "";
            }
        }

        /// <summary>
        /// Název obce, ve které se zastávka nachází
        /// </summary>
        public string MunicipalityName { get; set; }

        /// <summary>
        /// Umístění zastávky (hlavně pro tarifní počítání)
        /// </summary>
        public ZoneRegionType ZoneRegionType { get; set; }

        /// <summary>
        /// CIS číslo zastávky
        /// </summary>
        public int CisNumber { get; set; }

        /// <summary>
        /// Číslo zastávky pro odbavovací informační systém
        /// </summary>
        public int OisNumber { get; set; }

        /// <summary>
        /// Jednopísmenné označení kraje (dle SPZ)
        /// </summary>
        public string RegionCode { get; set; }

        /// <summary>
        /// Indikátor, jestli jde o veřejnou zastávku (neveřejné jsou např. provozovny)
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// Vrací true, pokud jde o stanici metra (pozná se podle čísla sloupku)
        /// </summary>
        public bool IsMetro { get { return StopId >= 100 && StopId < 200; } }

        /// <summary>
        /// Vrací true, pokud jde o vlakovou stanici (pozná se podle čísla sloupku)
        /// </summary>
        public bool IsTrain { get { return StopId >= 300; } }

        /// <summary>
        /// Indikace bezbariérovosti zastávky dle výčtu GTFS
        /// </summary>
        public WheelchairAccessibility WheelchairAccessibility { get; set; }

        public void ExtendServiceDays(ServiceDaysBitmap bitmapToAdd)
        {
            ServiceAsBits = ServiceAsBits.Union(bitmapToAdd);
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(PlatformCode))
                return $"{NodeId}/{StopId} {Name} {PlatformCode}";
            else
                return $"{NodeId}/{StopId} {Name}";
        }
    }
}
