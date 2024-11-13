using CommonLibrary;
using CzpttModel;
using CzpttModel.Kango;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrainsEditor.CommonLogic
{
    /// <summary>
    /// Poskytuje informace o poznámkách k vlaku přetříděné k jednotlivým bodům na trase vlaku.
    /// Většina poznámek totiž není v datech vlaků uvedena přímo u bodů na trase, ale jako Name-Value páry vztahující se k vlaku,
    /// které obsahují info o tom, od jaké po jakou stanici platí. Tato třída přetřídí tuto strukturu,
    /// aby bylo možno se jednoduše dotazovat, která poznámka platí ke které stanici
    /// </summary>
    class TrainNetworkSpecificParamsProvider
    {
        private CZPTTCISMessage _trainData;

        // přiřazení poznámek ke stanicím
        private Dictionary<CZPTTLocation, List<IStationRangeParam>> _locationParams;

        /// <summary>
        /// Inicializuje instanci provideru (roztřídí poznámky podle platnosti k jednotlivým lokacím vlaku)
        /// </summary>
        /// <param name="trainData">Data vlaku</param>
        public TrainNetworkSpecificParamsProvider(CZPTTCISMessage trainData)
        {
            _trainData = trainData;
            ResetParamsForLocations();
        }

        // načte z dat vlaku položku _locationParams (přiřazení poznámek ke stanicím)
        public void ResetParamsForLocations()
        {
            _locationParams = new Dictionary<CZPTTLocation, List<IStationRangeParam>>();
            var notYetValidParams = _trainData.GetTrainCentralNotes().Cast<IStationRangeParam>().Union(_trainData.GetTrainIntegratedSystemsNotes()).ToList();
            var validParams = new List<IStationRangeParam>();
            var stationCodeOccurences = new Dictionary<string, int>();

            foreach (var location in _trainData.CZPTTInformation.CZPTTLocation)
            {
                var locationCode = location.GetLocationWithCountryCode();
                var occurence = stationCodeOccurences.GetValueOrDefault(locationCode);

                var newlyValidParams = notYetValidParams.Where(par => par.FromStationCode == locationCode && par.FromStationCodeOccurence == occurence).ToArray();
                validParams.AddRange(newlyValidParams);
                _locationParams.Add(location, new List<IStationRangeParam>(validParams));
                foreach (var par in newlyValidParams)
                {
                    notYetValidParams.Remove(par);
                }

                var newlyNoLongerValidParams = validParams.Where(par => par.ToStationCode == locationCode && par.ToStationCodeOccurence == occurence).ToArray();
                foreach (var par in newlyNoLongerValidParams)
                {
                    validParams.Remove(par);
                }

                stationCodeOccurences[locationCode] = occurence + 1;
            }
        }

        /// <summary>
        /// Vrátí všechny centrální poznámky týkající se vlaku
        /// </summary>
        public IEnumerable<CZCentralPTTNote> GetAllCentralPTTNotes()
        {
            return _trainData.GetTrainCentralNotes();
        }

        /// <summary>
        /// Vrátí všechny poznámky o IDS týkající se vlaku
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CZIPTS> GetAllIPTs()
        {
            return _trainData.GetTrainIntegratedSystemsNotes();
        }

        /// <summary>
        /// Vrátí všechny centrální poznámky platné v daném bodě trasy <paramref name="location"/>
        /// </summary>
        /// <exception cref="KeyNotFoundException">Pokud stanice není na trase</exception>
        public IEnumerable<CZCentralPTTNote> GetCentralPTTNotesForLocation(CZPTTLocation location)
        {
            return _locationParams[location].OfType<CZCentralPTTNote>();
        }

        /// <summary>
        /// Vrátí všechny poznámky o IDS platné v daném bodě trasy <paramref name="location"/>
        /// </summary>
        /// <exception cref="KeyNotFoundException">Pokud stanice není na trase</exception>
        public IEnumerable<CZIPTS> GetIPTSsForLocation(CZPTTLocation location)
        {
            return _locationParams[location].OfType<CZIPTS>();
        }

        /// <summary>
        /// Ověří existenci centrálních poznámek daného kódu platných v daném bodě trasy <paramref name="location"/> a vrátí instance nalezených poznámek.
        /// </summary>
        /// <param name="location">Bod na trase, ke kterému musí být centrální poznámky platné</param>
        /// <param name="centralNoteCodes">Kódy poznámek, které hledáme</param>
        /// <returns>Nalezené centrální poznámky obsahující některý ze zadaných kódů, případně prázdný seznam</returns>
        /// <exception cref="KeyNotFoundException">Pokud stanice není na trase</exception>
        public IEnumerable<CZCentralPTTNote> FindCentralNotesForLocation(CZPTTLocation location, params CentralNoteCode[] centralNoteCodes)
        {
            return GetCentralPTTNotesForLocation(location).Where(note => centralNoteCodes.Contains(note.Code));
        }

        /// <summary>
        /// Ověří existenci poznámek daného kódu IDS platných v daném bodě trasy <paramref name="location"/> a vrátí instance nalezených poznámek.
        /// </summary>
        /// <param name="location">Bod na trase, ke kterému musí být poznámky o IDS platné</param>
        /// <param name="iptsCodes">Kódy IDS poznámek, které hledáme</param>
        /// <returns>Nalezené poznámky obsahující některý ze zadaných kódů, případně prázdný seznam</returns>
        /// <exception cref="KeyNotFoundException">Pokud stanice není na trase</exception>
        public IEnumerable<CZIPTS> FindIPTSForLocation(CZPTTLocation location, params IPTSCode[] iptsCodes)
        {
            return GetIPTSsForLocation(location).Where(note => iptsCodes.Contains(note.Code));
        }

        /// <summary>
        /// Pokud poznámka daného kódu už existuje, rozšíří ji tak, aby zahrnovala celý zadaný interval. To může zahrnovat i stanice mimo tento interval, pokud je disjunktní s tím původním.
        /// Pokud žádná neexistuje, vytvoří novou.
        /// </summary>
        /// <param name="firstLocation">Počátek cílového intervalu</param>
        /// <param name="centralNoteCode">Konec cílového intervalu</param>
        /// <param name="lastLocation">Kód poznámky</param>
        public void ExpandOrCreateCentralNoteForLocations(CZPTTLocation firstLocation, CZPTTLocation lastLocation, CentralNoteCode centralNoteCode)
        {
            var existingParams = GetAllCentralPTTNotes().Where(note => note.Code == centralNoteCode);
            if (existingParams.Any())
            {
                ExpandNote(existingParams.First(), firstLocation, lastLocation);
            }
            else
            {
                var newCentralNote = _trainData.CreateTrainCentralNote();
                newCentralNote.Code = centralNoteCode;
                ExpandNote(newCentralNote, firstLocation, lastLocation);
            }
        }

        /// <summary>
        /// Odstraní poznámky, které pokrývají vybrané lokace. Reálně odstraní víc než asi uživatel čeká, protože stačí, když vybrané lokace
        /// jen zasahují do rozsahu, který poznámka pokrývá a smaže se celá poznámka.
        /// 
        /// TODO udělat chytřeji
        /// </summary>
        /// <param name="locations">Lokace, ze kterých chceme poznámku odstranit</param>
        /// <param name="centralNoteCode">Kód centrální poznámky k odstranění</param>
        public void RemoveCentralNoteForLocations(IEnumerable<CZPTTLocation> locations, CentralNoteCode centralNoteCode)
        {
            var paramSet = new HashSet<CZCentralPTTNote>();
            foreach (var location in locations)
            {
                var pars = FindCentralNotesForLocation(location, centralNoteCode);
                foreach (var par in pars)
                {
                    paramSet.Add(par);
                }
            }

            foreach (var par in paramSet)
            {
                _trainData.RemoveTrainNote(par.NameAndValuePair);
            }

            ResetParamsForLocations();
        }

        // prozatím neumí poznámky dělit, tj. vždycky protahuje tu stávající, i když je úsek (firstLocation, lastLocation) disjunktní se současnou poznámkou
        //  - správně by se v takovém případě měla udělat duplicitní poznámka pro novou část zastávek, ale pak je problém s mergem
        private void ExpandNote(IStationRangeParam param, CZPTTLocation firstLocation, CZPTTLocation lastLocation)
        {
            bool passedOriginalFromStation = false;
            bool passedOriginalToStation = false;
            bool passedNewToStation = false;
            string newToStationCode = null;
            int newToStationCodeOccurence = 0;
            var stationCodeOccurences = new Dictionary<string, int>();

            foreach (var location in _trainData.CZPTTInformation.CZPTTLocation)
            {
                var locationCode = location.GetLocationWithCountryCode();
                var occurence = stationCodeOccurences.GetValueOrDefault(locationCode);

                if (param.FromStationCode == locationCode && param.FromStationCodeOccurence == occurence)
                {
                    passedOriginalFromStation = true;
                }

                if (location == firstLocation && !passedOriginalFromStation)
                {
                    param.FromStationCode = locationCode;
                    param.FromStationCodeOccurence = occurence;
                }
                
                if (param.ToStationCode == locationCode && param.ToStationCodeOccurence == occurence)
                {
                    passedOriginalToStation = true;
                    newToStationCode = locationCode;
                    newToStationCodeOccurence = occurence;
                }

                if (location == lastLocation)
                {
                    passedNewToStation = true;
                    newToStationCode = locationCode;
                    newToStationCodeOccurence = occurence;
                }

                if (passedNewToStation && passedOriginalToStation)
                {
                    break;
                }

                stationCodeOccurences[locationCode] = occurence + 1;
            }

            param.ToStationCode = newToStationCode;
            param.ToStationCodeOccurence = newToStationCodeOccurence;

            ResetParamsForLocations();
        }
    }
}
