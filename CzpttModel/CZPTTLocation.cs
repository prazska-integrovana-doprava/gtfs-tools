using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace CzpttModel
{
    /// <summary>
    /// Popis dopravního bodu (průjezd vlaku)
    /// </summary>
    [XmlRoot]
    public class CZPTTLocation
    {
        /// <summary>
        /// Základ informace o poloze
        /// </summary>
        [XmlElement]
        public LocationIdent Location { get; set; }

        /// <summary>
        /// Určení času v lokalitě
        /// </summary>
        [XmlElement]
        public TimingAtLocation TimingAtLocation { get; set; }

        /// <summary>
        /// Dopravce, který odpovídá za vlak na daném úseku. Jedná se o právní odpovědnost dopravce. Číselník společností je uveden na WS KADR.
        /// </summary>
        [XmlElement]
        public string ResponsibleRU { get; set; }

        /// <summary>
        /// Správce infrastruktury/jízdních řádů, který odpovídá za daný úsek.
        /// </summary>
        [XmlElement]
        public string ResponsibleIM { get; set; }

        /// <summary>
        /// Vždy 1 = veřejný osobní vlak
        /// </summary>
        [XmlElement]
        public int TrainType { get; set; }

        /// <summary>
        /// Druh vlaku
        /// </summary>
        [XmlElement]
        public TrafficTypeEnum TrafficType { get; set; }

        /// <summary>
        /// Komerční druh vlaku, který využívá dopravce pro komunikaci se zákazníky, např. SC, EC, IC, EN, TLX, Ee.
        /// Aktuální seznam kódů uvádí <see cref="CommercialTrafficType.CommercialTrafficTypes"/>.
        /// </summary>
        [XmlElement]
        [DefaultValue(0)]
        public int CommercialTrafficType { get; set; }

        /// <summary>
        /// Přítomen pouze v případě, že v dané lokalitě bude probíhat nějaká aktivita uvedená v číselníku aktivit.
        /// V českém prostředí se používá pojem úkon.
        /// </summary>
        [XmlElement]
        public List<TrainActivity> TrainActivity { get; set; }

        /// <summary>
        /// Číslo vlaku tak jak je uváděno v jízdním řádu
        /// </summary>
        [XmlElement]
        public int OperationalTrainNumber { get; set; }

        /// <summary>
        /// Národní parametry. Veškeré údaje, které nejsou obsaženy v mezinárodně odsouhlasených parametrech zpráv.
        /// </summary>
        [XmlElement]
        public List<NameAndValuePair> NetworkSpecificParameter { get; set; }

        public override string ToString()
        {
            return $"{OperationalTrainNumber} at {Location.PrimaryLocationName} ({Location.LocationPrimaryCode})";
        }
    }
}
