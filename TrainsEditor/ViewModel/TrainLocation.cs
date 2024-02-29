using CommonLibrary;
using CzpttModel;
using CzpttModel.Kango;
using System;
using System.ComponentModel;
using System.Linq;
using TrainsEditor.CommonLogic;
using TrainsEditor.ExportModel;

namespace TrainsEditor.ViewModel
{
    /// <summary>
    /// ViewModel vlaku zastavení/průjezdu ve stanici/dopravním bodě nad <see cref="CZPTTLocation"/>.
    /// </summary>
    [Serializable]
    class TrainLocation : INotifyPropertyChanged
    {
        /// <summary>
        /// Odkaz na data z XML souboru
        /// </summary>
        public CZPTTLocation LocationData { get; private set; }

        // ukládáme si všechny poznámky vlaku, bez ohledu na to, zda jsou relevantní pro tuto stanici (abychom to dokázali snadno upravovat)
        private TrainNetworkSpecificParamsProvider _networkSpecificParameters;

        // Data ze SR70 a případně i ASW JŘ
        private TrainStop _additionalStationData;
        public TrainStop AdditionalStationData
        {
            get
            {
                return _additionalStationData;
            }
            set
            {
                if (_additionalStationData == value)
                    return;

                _additionalStationData = value;
                LocationData.Location.LocationPrimaryCode = _additionalStationData.PrimaryLocationCode;
                LocationData.Location.PrimaryLocationName = _additionalStationData.Name;
                LocationData.Location.LocationSubsidiaryIdentification = null;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AdditionalStationData"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LocationName"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ZoneIds"));
            }
        }

        /// <summary>
        /// Flagy určující, do kterých IDS vlak v této stanici patří
        /// </summary>
        public IntegratedSystemsEnum IntegratedSystems { get; private set; }

        /// <summary>
        /// Pásma PID, ve kterých se daná stanice nachází, nebo prázdný řetězec, pokud stanice není v PID nebo nemá přiřazené pásmo
        /// </summary>
        public string ZoneIds
        {
            get
            {
                if (AdditionalStationData?.ZoneIds != null)
                {
                    return string.Join<AswModel.Extended.ZoneInfo>(", ", AdditionalStationData.ZoneIds);
                }
                else
                {
                    return "";
                }
            }
        }

        private bool _isFirstOrLastStation;
        /// <summary>
        /// True, pokud jde o výchozí nebo cílovou stanici/dopravní bod vlaku, a to i když by šlo o manipulační bod.
        /// </summary>
        public bool IsFirstOrLastStation
        {
            get
            {
                return _isFirstOrLastStation;
            }
            set
            {
                _isFirstOrLastStation = value;
                OnTrainStopsHereChanged();
            }
        }

        /// <summary>
        /// Data o předchozí stanici/dopravním bodě na trase
        /// </summary>
        protected CZPTTLocation PrevLocationData { get; private set; }

        /// <summary>
        /// ViewModel aktivit vlaku
        /// </summary>
        public TrainActivityViewModel TrainActivity { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Název stanice/dopravního bodu
        /// </summary>
        public string LocationName => LocationData.Location.PrimaryLocationName;

        /// <summary>
        /// Typ a číslo vlaku na odjezdu ze stanice (např. "R 981")
        /// </summary>
        public string TrainTypeAndNumber => LocationData.GetTrainTypeAndNumber();

        /// <summary>
        /// Vrací nebo nastaví čas příjezdu do stanice
        /// </summary>
        public Time? ArrivalTime
        {
            get
            {
                return LocationData.GetLocationArrivalTime();
            }
            set
            {
                LocationData.SetLocationTime(TimingAtLocation.TimingQualifierCodeEnum.Arrival, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ArrivalTime"));
                OnTrainStopsHereChanged();
            }
        }

        /// <summary>
        /// Vrací nebo nastaví čas odjezdu ze stanice
        /// </summary>
        public Time? DepartureTime
        {
            get
            {
                return LocationData.GetLocationDepartureTime();
            }
            set
            {
                LocationData.SetLocationTime(TimingAtLocation.TimingQualifierCodeEnum.Departure, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DepartureTime"));
                OnTrainStopsHereChanged();
            }
        }

        /// <summary>
        /// Označení linky dle svého IDS na odjezdu ze stanice
        /// </summary>
        public string LineNumber
        {
            get
            {
                return LocationData.GetLineInfo().LineName;
            }
            set
            {
                // TODO pokud to chceme umožnit i pro nePID, musí uživatel zadávat i IDS, do kterého chce vlak zařadit
                var lineNumber = TrainLineInfo.TrainLineNameToNumberPid(value);
                if (lineNumber != 0)
                {
                    LocationData.SetLineName(lineNumber.ToString());
                }
                else
                {
                    LocationData.SetLineName(value);
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LineNumber"));
            }
        }

        /// <summary>
        /// Vrací/nastavuje příznak, zda místo vlaku ze stanice odjíždí náhradní doprava
        /// </summary>
        public bool IsAlternativeTransport
        {
            get
            {
                return LocationData.IsAlternativeTransportOnDeparture();
            }
            set
            {
                LocationData.SetAlternativeTransportOnDeparture(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsAlternativeTransport"));
            }
        }

        /// <summary>
        /// Vrací textový příznak, zda je vlak v tomto místě bezbariérově přístupný
        /// 
        /// Nastavuje se přes vlastnický objekt vlaku (resp. network specific parameters). Následně je potřeba zavolat <see cref="OnWheelchairAccessChanged"/>,
        /// aby se tato hodnota obnovila a zároveň propsala do UI
        /// </summary>
        public string WheelchairAccessibilityIndicator
        {
            get
            {
                if (_networkSpecificParameters.FindCentralNotesForLocation(LocationData, CentralNoteCode.WheelchairTransportAndPickup).Any())
                {
                    return "♿+";
                }
                else if (_networkSpecificParameters.FindCentralNotesForLocation(LocationData, CentralNoteCode.WheelchairTransportAvailable).Any())
                {
                    return "♿";
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// True, pokud zde vlak zastavuje pro cestující
        /// </summary>
        public bool TrainStopsHere
        {
            get
            {
                return LocationData.TrainStopsHere(PrevLocationData, IsFirstOrLastStation);
            }
        }

        /// <summary>
        /// True, pokud jde o veřejnou část vlaku (osekanou o případný manipulační nájezd na začátku nebo zátah na konci)
        /// </summary>
        public bool IsPublicTrainPart
        {
            get
            {
                return LocationData.IsInPublicPart(PrevLocationData);
            }
        }

        // debug účely, všechny poznámky platné ve stanici
        public string IPTSNotesText
        {
            get
            {
                if (_networkSpecificParameters == null)
                    return null;

                return string.Join(", ", _networkSpecificParameters.GetIPTSsForLocation(LocationData).Select(note => note.Code).OrderBy(c => c));
            }
        }

        protected TrainLocation(CZPTTLocation locationData, CZPTTLocation prevLocationData, TrainStop additionalStationData, IntegratedSystemsEnum integratedSystems, bool isFirstOrLastStation,
            TrainNetworkSpecificParamsProvider networkSpecificParams)
        {
            LocationData = locationData;
            _networkSpecificParameters = networkSpecificParams;
            AdditionalStationData = additionalStationData;
            IntegratedSystems = integratedSystems;
            _isFirstOrLastStation = isFirstOrLastStation;
            PrevLocationData = prevLocationData;
            TrainActivity = new TrainActivityViewModel(LocationData.TrainActivity);
            TrainActivity.PropertyChanged += TrainActivity_PropertyChanged;
        }

        /// <summary>
        /// Sestaví instanci na základě zadaných dat.
        /// </summary>
        /// <param name="locationData">Data o lokaci</param>
        /// <param name="prevLocationData">Data o předchozí lokaci vlaku</param>
        /// <param name="isFirstOrLastStation">True, pokud jde o první nebo poslední stanici (včetně manipulačních částí)</param>
        /// <param name="stationDb">Databáze stanic</param>
        /// <param name="routeDb">Databáze linek</param>
        /// <param name="networkSpecificParams">Všechny poznámky vlaku (i ty, které neplatí v dané stanici)</param>
        /// <returns></returns>
        public static TrainLocation Construct(CZPTTLocation locationData, CZPTTLocation prevLocationData, bool isFirstOrLastStation, StationDatabase stationDb, RouteDatabase routeDb,
            TrainNetworkSpecificParamsProvider networkSpecificParams)
        {
            var additionalData = locationData.GetAdditionalData(stationDb);
            var lineInfo = locationData.GetLineInfo();

            var integratedSystems = IntegratedSystemsEnum.None;
            if ((additionalData?.IsFromAsw).GetValueOrDefault())
            {                
                if (!lineInfo.IsNonPidLine && routeDb.Lines.ContainsKey(lineInfo.LineName))
                {
                    integratedSystems |= IntegratedSystemsEnum.PID;
                }
            }

            if (lineInfo.LineType == TrainLineType.Odis)
            {
                integratedSystems |= IntegratedSystemsEnum.ODIS;
            }

            return new TrainLocation(locationData, prevLocationData, additionalData, integratedSystems, isFirstOrLastStation, networkSpecificParams);
        }

        /// <summary>
        /// Nastaví typ a číslo linky
        /// </summary>
        /// <param name="commercialTrafficTypeId">Typ vlaku dle číselníku <see cref="CommercialTrafficType.CommercialTrafficTypes"/></param>
        /// <param name="trainNumber">Číslo vlaku</param>
        public void SetTrainTypeAndNumber(int commercialTrafficTypeId, int trainNumber)
        {
            LocationData.CommercialTrafficType = commercialTrafficTypeId;
            LocationData.OperationalTrainNumber = trainNumber;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TrainTypeAndNumber"));
        }
        
        /// <summary>
        /// Wheelchair accessibility se musí nastavovat zvrchu, protože se nastavuje skrz celý vlak.
        /// Pokud někdo změní wheelchair accessibilitu, musí zavolat tuto metodu
        /// </summary>
        public void OnWheelchairAccessChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("WheelchairAccessibilityIndicator"));
        }

        private void TrainActivity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnTrainStopsHereChanged();
        }

        private void OnTrainStopsHereChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TrainStopsHere"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsPublicTrainPart"));
        }
    }
}
