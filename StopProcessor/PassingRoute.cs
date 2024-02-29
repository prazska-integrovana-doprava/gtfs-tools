using GtfsModel;
using GtfsModel.Enumerations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace StopProcessor
{
    /// <summary>
    /// Třída reprezentující jednu linku projíždějící zastávkou (<see cref="Stop.PassingRoutes"/>).
    /// </summary>
    [Serializable()]
    [XmlType(TypeName = "line")]
    public class PassingRoute
    {
        /// <summary>
        /// Číslo linky dle ASW JŘ
        /// </summary>
        [XmlAttribute(AttributeName = "id")]
        [JsonProperty(PropertyName = "id")]
        public int LineNumber { get; set; }
        
        /// <summary>
        /// Veřejné označení linky (např. 9, P4, S22)
        /// </summary>
        [XmlAttribute(AttributeName = "name")]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Typ dopravního prostředku
        /// </summary>
        [XmlAttribute(AttributeName = "type")]
        [JsonProperty(PropertyName = "type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public TrafficType Type { get; set; }

        /// <summary>
        /// Směr (0/1 dle GTFS)
        /// </summary>
        //[XmlAttribute(AttributeName = "direction")]
        //[JsonProperty(PropertyName = "direction")]
        [XmlIgnore()]
        [JsonIgnore()]
        public int Direction { get; set; }

        /// <summary>
        /// True, pokud jde o noční linku
        /// </summary>
        [XmlAttribute(AttributeName = "isNight")]
        [JsonProperty(PropertyName = "isNight", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(false)]
        public bool IsNight { get; set; }

        // pro každý směr si uložíme, jak často se používá
        private Dictionary<string, int> headsignFrequencies;

        /// <summary>
        /// Nejčastější směr (headsign), který mají spoje této linky na této zastávce
        /// </summary>
        [XmlAttribute(AttributeName = "direction")]
        [JsonProperty(PropertyName = "direction")]
        public string Headsign
        {
            get
            {
                return GetHeadsignsOrdered().First().Key;
            }
            set { throw new InvalidOperationException(); }
        }

        /// <summary>
        /// Pokud je výrazněji zastoupen ještě nějaký headsign jiný než <see cref="Headsign"/>, je uveden zde
        /// </summary>
        [XmlAttribute(AttributeName = "direction2")]
        [JsonProperty(PropertyName = "direction2", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue("")]
        public string Headsign2
        {
            get
            {
                if (headsignFrequencies.Count < 2)
                    return "";

                var secondBest = GetHeadsignsOrdered().Skip(1).First();
                if (secondBest.Value <= TotalTripCount / 5)
                    return ""; // není dost významný headsign

                return secondBest.Key;
            }
            set { throw new InvalidOperationException(); }
        }

        // kvůli serializaci
        public PassingRoute()
        {
        }

        public PassingRoute(int routeNumber, int direction, GtfsRoute route)
        {
            LineNumber = routeNumber;
            Direction = direction;
            Name = route.ShortName;
            Type = route.Type;
            IsNight = route.IsNight;
            headsignFrequencies = new Dictionary<string, int>();
        }

        /// <summary>
        /// Přidá směr jednoho spoje
        /// </summary>
        /// <param name="headsign">Směr</param>
        public void AddHeadsign(string headsign)
        {
            if (headsignFrequencies.ContainsKey(headsign))
            {
                headsignFrequencies[headsign]++;
            }
            else
            {
                headsignFrequencies.Add(headsign, 1);
            }
        }
        
        public override bool Equals(object obj)
        {
            var other = obj as PassingRoute;
            return other != null && LineNumber == other.LineNumber && Direction == other.Direction;
        }

        public override int GetHashCode()
        {
            return LineNumber.GetHashCode() + Direction.GetHashCode() * 25713;
        }

        /// <summary>
        /// Vrátí cíle spojů seřazené podle četnosti
        /// </summary>
        /// <returns>Cíle</returns>
        private IEnumerable<KeyValuePair<string, int>> GetHeadsignsOrdered()
        {
            return headsignFrequencies.OrderByDescending(h => h.Value);
        }

        private int TotalTripCount
        {
            get { return headsignFrequencies.Sum(h => h.Value); }
        }
    }

}
