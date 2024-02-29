using System.Collections.Generic;
using System.Xml.Serialization;

namespace CzpttModel
{
    // Informace o trase a kalendáři
    [XmlRoot]
    public class CZPTTInformation
    {
        /// <summary>
        /// Popisy bodů, přes které vlak jede. Ve zprávě musí být uvedeny minimálně 2 lokality - dopravní body.
        /// </summary>
        [XmlElement]
        public List<CZPTTLocation> CZPTTLocation { get; set; }

        /// <summary>
        /// Obsahuje určení kalendáře pro výchozí bod trasy. 
        /// Podle jednotlivých dnů jízdy uvedených v bitové mapě se počítá denní tvar identifikátoru PA ID.
        /// 
        /// Pokud je třeba zjistit konkrétní den jízdy v konkrétním bodě, 
        /// pak se musí vzít den z kalendáře a k němu připočítat hodnotu z elementu Offset.
        /// </summary>
        [XmlElement]
        public PlannedCalendar PlannedCalendar { get; set; }
    }
}
