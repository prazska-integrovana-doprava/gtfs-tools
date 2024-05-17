using GtfsModel.Enumerations;
using JR_XML_EXP;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace StopProcessor
{
    /// <summary>
    /// Záznam o jedné zastávce (sloupku)
    /// </summary>
    [Serializable()]
    [XmlType(TypeName = "stop")]
    [JsonObject(MemberSerialization.OptIn)]
    public class Stop
    {
        /// <summary>
        /// Číslo uzlu dle ASW JŘ
        /// </summary>
        [XmlIgnore]
        public int NodeId { get; set; }

        /// <summary>
        /// Číslo sloupku dle ASW JŘ.
        /// </summary>
        [XmlIgnore]
        public int StopId { get; set; }

        [XmlAttribute(AttributeName = "id")] 
        [JsonProperty(PropertyName = "id")]
        public string UniqueStopId
        {
            get { return $"{NodeId}/{StopId}"; }
            set { throw new InvalidOperationException(); } // jen pro serializaci
        }

        /// <summary>
        /// Stanoviště..
        /// </summary>
        [XmlAttribute(AttributeName = "platform")]
        [JsonProperty(PropertyName = "platform", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue("")]
        public string PlatformCode { get; set; }

        /// <summary>
        /// Název2 zastávky dle ASW JŘ (nemusí být unikátní, ale měl by být bez různých přídomků typu "nábřeží", " - A" apod.)
        /// </summary>
        [XmlIgnore()]
        public string Name2 { get; set; }

        /// <summary>
        /// Název používaný v CRWS a vyhledávači IDOS (název 7 v ASW JŘ)
        /// </summary>
        [XmlAttribute(AttributeName = "altIdosName")]
        [JsonProperty(PropertyName = "altIdosName")]
        public string IdosName { get; set; }

        /// <summary>
        /// Název používaný v CRWS a vyhledávači IDOS, kde ale nahradíme dvojčárky jednou
        /// </summary>
        [XmlIgnore]
        public string IdosNameWithoutMultipleCommas
        {
            get
            {
                return IdosName.Replace(",,", ",");
            }
        }

        /// <summary>
        /// Kategorie (číslo seznamu v CRWS)
        /// </summary>
        [XmlIgnore()]
        public int IdosCategoryNumber { get; set; }

        [XmlAttribute(AttributeName = "lat")]
        [JsonProperty(PropertyName = "lat")]
        public float GpsLatitude { get; set; }

        [XmlAttribute(AttributeName = "lon")]
        [JsonProperty(PropertyName = "lon")]
        public float GpsLongitude { get; set; }

        /// <summary>
        /// X souřadnice v Křovákovi
        /// </summary>
        [XmlAttribute(AttributeName = "jtskX")]
        [JsonProperty(PropertyName = "jtskX")]
        public float SjtskX { get; set; }

        /// <summary>
        /// Y souřadnice v Křovákovi
        /// </summary>
        [XmlAttribute(AttributeName = "jtskY")]
        [JsonProperty(PropertyName = "jtskY")]
        public float SjtskY { get; set; }

        /// <summary>
        /// Tarifní pásma dle číselníku ASW JŘ (mohou být oddělena čárkou)
        /// </summary>
        [XmlAttribute(AttributeName = "zone")]
        [JsonProperty(PropertyName = "zone")]
        public string ZoneId { get; set; }

        /// <summary>
        /// Typ dopravního prostředku
        /// </summary>
        [XmlAttribute(AttributeName = "mainTrafficType")]
        [JsonProperty(PropertyName = "mainTrafficType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public TrafficTypeExtended MainTrafficType { get; set; }

        /// <summary>
        /// CIS číslo zastávky
        /// </summary>
        [XmlIgnore()]
        public int CisNumber { get; set; }

        /// <summary>
        /// "SPZ" zastávky
        /// </summary>
        [XmlIgnore()]
        public string DistrictCode { get; set; }

        /// <summary>
        /// Obec, na jejímž území zastávka leží
        /// </summary>
        [XmlIgnore()]
        public string Municipality { get; set; }

        /// <summary>
        /// Indikátor, jestli jde o veřejnou zastávku (neveřejné jsou např. provozovny)
        /// </summary>
        [XmlIgnore()]
        public bool IsPublic { get; set; }

        /// <summary>
        /// Indikace bezbariérovosti zastávky dle výčtu GTFS
        /// </summary>
        [XmlAttribute(AttributeName = "wheelchairAccess")]
        [JsonProperty(PropertyName = "wheelchairAccess")]
        [JsonConverter(typeof(StringEnumConverter))]
        public WheelchairBoarding WheelchairBoarding { get; set; }

        /// <summary>
        /// Seznam GTFS identifikátorů zastávek
        /// </summary>
        [XmlAttribute(AttributeName = "gtfsIds")]
        [JsonProperty(PropertyName = "gtfsIds")]
        public List<string> GtfsIds { get; set; }

        /// <summary>
        /// Seznam projíždějících linek (vyplní StopRoutesAssignOperation) - indexováno číslem linky a směrem (direction)
        /// </summary>
        [XmlIgnore()]
        public IDictionary<int, IDictionary<int, PassingRoute>> PassingRoutes { get; set; }

        public IEnumerable<PassingRoute> PassingRoutesFlat
        {
            get { return PassingRoutes.Values.SelectMany(prd => prd.Values); }
        }

        [XmlElement(ElementName = "line")]
        [JsonProperty(PropertyName = "lines")]
        public PassingRoute[] PassingRoutesOrdered
        {
            get
            {
                return PassingRoutesFlat.Where(route => route.Type == TrafficType.Metro).OrderBy(route => route.Name)
                    .Union(PassingRoutesFlat.Where(route => route.Type == TrafficType.Tram).OrderBy(route => route.LineNumber))
                    .Union(PassingRoutesFlat.Where(route => route.Type == TrafficType.Rail).OrderBy(route => route.Name))
                    .Union(PassingRoutesFlat.Where(route => route.Type == TrafficType.Bus).OrderBy(route => route.LineNumber))
                    .Union(PassingRoutesFlat.OrderBy(route => route.LineNumber)).ToArray();
            }
            set { throw new InvalidOperationException(); }
        }

        /// <summary>
        /// Odkaz na zastávku načtenou z XML (pro zpětný propis využití)
        /// </summary>
        [XmlIgnore]
        public Zastavka XmlStop { get; set; }

        /// <summary>
        /// Zastávka je využitá, pokud přes ni jezdí nějaká linka
        /// </summary>
        [XmlIgnore()]
        public bool IsUsed { get; set; }

        /// <summary>
        /// Jde o nástupiště metra
        /// </summary>
        [XmlAttribute(AttributeName = "isMetro")]
        [JsonProperty(PropertyName = "isMetro", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(false)]
        public bool IsMetro
        {
            get
            {
                return StopId >= 100 && StopId <= 199;
            }
            set
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Jde o vlakovou zastávku?
        /// </summary>
        [XmlIgnore()]
        public bool IsTrain
        {
            get
            {
                // takhle to prostě v datech je
                return StopId >= 300 && StopId <= 399;
            }
            set
            {
                throw new InvalidOperationException(); // jen pro účely serializace do xml, musí mít set
            }
        }
        
        // povinný kvůli serializaci
        public Stop()
        {
            PassingRoutes = new Dictionary<int, IDictionary<int, PassingRoute>>();
            GtfsIds = new List<string>();
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(PlatformCode))
                return $"{NodeId}/{StopId} {Name2} {PlatformCode}";
            else
                return $"{NodeId}/{StopId} {Name2}";
        }
    }
}
