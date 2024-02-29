using CzpttModel.Kango;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace CzpttModel
{
    /// <summary>
    /// Root zprávy o jednom vlaku
    /// </summary>
    [XmlRoot]
    public class CZPTTCISMessage
    {
        /// <summary>
        /// Identifikace objektů, které jsou řešeny v daném CZPTT - trasy a vlaku.
        /// 
        /// Ve zprávě CZPTTCISMessage budou vždy uvedeny 2 identifikátory TR ID a PA ID jako PlannedTransportIdentifiers.
        /// 
        /// Pokud CZPTT je odklonový, bude obsahovat i položku RelatedPlannedTransportIdentifiers se shodnou strukturou označující původní vlak.
        /// </summary>
        [XmlArray]
        [XmlArrayItem("PlannedTransportIdentifiers", typeof(PlannedTransportIdentifiers))]
        [XmlArrayItem("RelatedPlannedTransportIdentifiers", typeof(RelatedPlannedTransportIdentifiers))]
        public CompositeIdentifierPlannedType[] Identifiers { get; set; }
        
        /// <summary>
        /// Datum a čas vytvoření CZPTT
        /// </summary>
        [XmlElement]
        public DateTime CZPTTCreation { get; set; }

        /// <summary>
        /// Informace o vlaku včetně trasy
        /// </summary>
        [XmlElement]
        public CZPTTInformation CZPTTInformation { get; set; }

        /// <summary>
        /// Národní parametry. Veškeré údaje, které nejsou obsaženy v mezinárodně odsouhlasených parametrech zpráv.
        /// </summary>
        [XmlElement]
        public List<NameAndValuePair> NetworkSpecificParameter { get; set; }

        /// <summary>
        /// Vrátí TR ID, pokud je v poli <see cref="Identifiers"/> přítomno.
        /// </summary>
        public PlannedTransportIdentifiers GetTrainIdentifier()
        {
            return (PlannedTransportIdentifiers) Identifiers.FirstOrDefault(idf => idf is PlannedTransportIdentifiers && idf.ObjectType == CompositeIdentifierPlannedType.ObjectTypeEnum.TR);
        }

        /// <summary>
        /// Vrátí instance z <see cref="NetworkSpecificParameter"/>, které jsou centrálními poznámkami
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CZCentralPTTNote> GetTrainCentralNotes()
        {
            return NetworkSpecificParameter.Where(nsp => nsp.Name == "CZCentralPTTNote").Select(nsp => new CZCentralPTTNote(nsp));
        }

        /// <summary>
        /// Vrátí instance z <see cref="NetworkSpecificParameter"/>, které jsou poznámkami o IDS
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CZIPTS> GetTrainIntegratedSystemsNotes()
        {
            return NetworkSpecificParameter.Where(nsp => nsp.Name == "CZIPTS").Select(nsp => new CZIPTS(nsp));
        }

        /// <summary>
        /// Přidá novou centrální poznámku a vrátí odkaz na ni
        /// </summary>
        public CZCentralPTTNote CreateTrainCentralNote()
        {
            var nsp = new NameAndValuePair()
            {
                Name = "CZCentralPTTNote",
                Value = "",
            };

            NetworkSpecificParameter.Add(nsp);
            return new CZCentralPTTNote(nsp);
        }

        /// <summary>
        /// Odstraní poznámku
        /// </summary>
        /// <param name="nameAndValuePair">Poznámka k odstranění</param>
        public void RemoveTrainNote(NameAndValuePair nameAndValuePair)
        {
            NetworkSpecificParameter.Remove(nameAndValuePair);
        }

        public override string ToString()
        {
            if (!Identifiers.Any())
            {
                return base.ToString();
            }
            else
            {
                return GetTrainIdentifier().ToString();
            }
        }
    }
}
