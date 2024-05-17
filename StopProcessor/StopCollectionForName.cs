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
    /// Reprezentuje skupinu zastávek se stejným unikátním názvem
    /// </summary>
    [Serializable()]
    [JsonObject(MemberSerialization.OptIn)]
    public class StopCollectionForName
    {
        [XmlIgnore]
        public StopCollectionId UniqueIdentification { get; private set; }

        [XmlAttribute(AttributeName = "name")]
        [JsonProperty(PropertyName = "name")]
        public string Name2
        {
            get { return UniqueIdentification.Name2; }
            set { throw new NotImplementedException(); } // kvůli serializaci musí být setter
        }

        [XmlAttribute(AttributeName = "districtCode")]
        [JsonProperty(PropertyName = "districtCode")]
        public string DistrictCode
        {
            get { return UniqueIdentification.DistrictCode; }
            set { throw new NotImplementedException(); } // kvůli serializaci musí být setter
        }

        [XmlAttribute(AttributeName = "isTrain")]
        [JsonProperty(PropertyName = "isTrain", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(false)]
        public bool IsTrain
        {
            get { return UniqueIdentification.IsTrain; }
            set { throw new NotImplementedException(); } // kvůli serializaci musí být setter
        }

        [XmlAttribute(AttributeName = "idosCategory")]
        [JsonProperty(PropertyName = "idosCategory")]
        public int IdosCategory { get; set; }

        [XmlAttribute(AttributeName = "idosName")]
        [JsonProperty(PropertyName = "idosName")]
        public string IdosName
        {
            get
            {
                var names = Stops.Select(s => s.IdosNameWithoutMultipleCommas).ToArray();
                if (names.All(n => string.Compare(n, names[0], true) == 0))
                {
                    // všehny názvy stejné, vrátíme první
                    return names[0];
                }

                // odmažeme první uzávorkovaný výraz
                // proč první? Protože viz třeba zastávka "Čistá (u čerpací stanice) (RA)" --> "Čistá (RA)"
                for(int i = 0; i < names.Length; i++)
                {
                    var indexOfOpenBrace = names[i].IndexOf('(');
                    if (indexOfOpenBrace >= 0)
                    {
                        var indexOfCloseBrace = names[i].IndexOf(')', indexOfOpenBrace + 1);
                        if (indexOfCloseBrace >= 0)
                        {
                            names[i] = names[i].Substring(0, indexOfOpenBrace).TrimEnd() + names[i].Substring(indexOfCloseBrace + 1);
                        }
                        else
                        {
                            names[i] = names[i].Substring(0, indexOfOpenBrace).TrimEnd();
                        }
                    }
                }

                if (names.All(n => string.Compare(n, names[0], true) == 0))
                {
                    // všehny názvy stejné, vrátíme první
                    return names[0];
                }

                // nevyřešili jsme, vrátíme nejkratší název
                return names.OrderBy(n => n.Length).First();
            }
            set { throw new NotImplementedException(); } // kvůli serializaci musí být setter
        }

        [XmlAttribute(AttributeName = "fullName")]
        [JsonProperty(PropertyName = "fullName")]
        public string FullName { get; set; }

        [XmlAttribute(AttributeName = "uniqueName")]
        [JsonProperty(PropertyName = "uniqueName")]
        public string UniqueName { get; set; }
                
        [XmlAttribute(AttributeName = "node")]
        [JsonProperty(PropertyName = "node")]
        public int NodeNumber { get; set; }

        [XmlAttribute(AttributeName = "cis")]
        [JsonProperty(PropertyName = "cis")]
        public int CisNumber { get; set; }

        [XmlAttribute(AttributeName = "avgLat")]
        [JsonProperty(PropertyName = "avgLat")]
        public float AverageGpsLatitude { get { return Stops.Average(s => s.GpsLatitude); } set { throw new InvalidOperationException(); } }

        [XmlAttribute(AttributeName = "avgLon")]
        [JsonProperty(PropertyName = "avgLon")]
        public float AverageGpsLongitude { get { return Stops.Average(s => s.GpsLongitude); } set { throw new InvalidOperationException(); } }

        [XmlAttribute(AttributeName = "avgJtskX")]
        [JsonProperty(PropertyName = "avgJtskX")]
        public float AverageSjtskX { get { return Stops.Average(s => s.SjtskX); } set { throw new InvalidOperationException(); } }

        [XmlAttribute(AttributeName = "avgJtskY")]
        [JsonProperty(PropertyName = "avgJtskY")]
        public float AverageSjtskY { get { return Stops.Average(s => s.SjtskY); } set { throw new InvalidOperationException(); } }

        [XmlAttribute(AttributeName = "municipality")]
        [JsonProperty(PropertyName = "municipality")]
        public string Municipality { get; set; }

        [XmlAttribute(AttributeName = "mainTrafficType")]
        [JsonProperty(PropertyName = "mainTrafficType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public TrafficTypeExtended MainTrafficType { get; set; }

        [XmlElement(ElementName = "stop")]
        [JsonProperty(PropertyName = "stops")]
        public List<Stop> Stops { get; set; } // kvůli serializaci musí být setter

        public StopCollectionForName() // kvůli serializaci
        {
            throw new NotImplementedException();
        }

        public StopCollectionForName(StopCollectionId id)
        {
            Stops = new List<Stop>();
            UniqueIdentification = id;
        }
        
        /// <summary>
        /// Přidá zastávku do seznamu. Zastávka musí do skupiny patřit, tedy musí mít shodné <see cref="StopCollectionId"/>, jinak je generována výjimka.
        /// </summary>
        /// <param name="stopRecord">Zastávka</param>
        public void Add(Stop stopRecord)
        {
            if (!StopCollectionId.FromStop(stopRecord).Equals(UniqueIdentification))
                throw new InvalidOperationException($"Zastávka {stopRecord} do kolekce {UniqueIdentification} nepatří.");

            Stops.Add(stopRecord);
        }
    }
}
