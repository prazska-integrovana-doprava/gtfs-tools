using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using TrainsEditor.ViewModel;

namespace TrainsEditor.CommonLogic
{
    /// <summary>
    /// Provádí textové porovnání dat dvou vlaků
    /// </summary>
    class TrainTextComparison
    {
        /// <summary>
        /// Porovná <paramref name="newerTrain"/> oproti <paramref name="olderTrain"/>. Vypíše všechny stanice a označí společné, přidané, odebrané a změněné.
        /// </summary>
        /// <param name="olderTrain">Původní (starší) vlak</param>
        /// <param name="newerTrain">Nový vlak</param>
        /// <returns></returns>
        public string CompareTrains(TrainFile olderTrain, TrainFile newerTrain)
        {
            var olderIndex = 0;
            var newerIndex = 0;
            var text = new StringBuilder();
            while (olderIndex < olderTrain.Locations.Count || newerIndex < newerTrain.Locations.Count)
            {
                if (olderIndex == olderTrain.Locations.Count)
                {
                    PrintLocations(text, newerTrain.Locations, newerIndex, newerTrain.Locations.Count - 1, '+');
                    break;
                }
                else if (newerIndex == newerTrain.Locations.Count)
                {
                    PrintLocations(text, olderTrain.Locations, olderIndex, olderTrain.Locations.Count - 1, '-');
                    break;
                }

                var olderInNewerOffset = FindFirstOffset(newerTrain.Locations, newerIndex, olderTrain.Locations[olderIndex]);
                var newerInOlderOffset = FindFirstOffset(olderTrain.Locations, olderIndex, newerTrain.Locations[newerIndex]);

                if (newerIndex + olderInNewerOffset >= newerTrain.Locations.Count && olderIndex + newerInOlderOffset >= olderTrain.Locations.Count)
                {
                    // vlak B má další stanici, která není na trase vlaku A a vlak A má další stanici, která není na trasu B, posuneme se v obou případech o 1
                    PrintLocations(text, olderTrain.Locations, olderIndex, 1, '-');
                    PrintLocations(text, newerTrain.Locations, newerIndex, 1, '+');
                    olderIndex++;
                    newerIndex++;
                } 
                else if (olderInNewerOffset <= newerInOlderOffset)
                {
                    PrintLocations(text, newerTrain.Locations, newerIndex, olderInNewerOffset - 1, '+');
                    newerIndex += olderInNewerOffset;
                }
                else
                {
                    PrintLocations(text, olderTrain.Locations, olderIndex, newerInOlderOffset - 1, '-');
                    olderIndex += newerInOlderOffset;
                }

                if (olderIndex == olderTrain.Locations.Count || newerIndex == newerTrain.Locations.Count)
                {
                    PrintLocations(text, olderTrain.Locations, olderIndex, olderTrain.Locations.Count - 1, '-');
                    PrintLocations(text, newerTrain.Locations, newerIndex, newerTrain.Locations.Count - 1, '+');
                    break;
                }

                var olderCurrent = olderTrain.Locations[olderIndex].LocationData;
                var newerCurrent = newerTrain.Locations[newerIndex].LocationData;
                if (olderCurrent.Location.LocationPrimaryCode == newerCurrent.Location.LocationPrimaryCode && olderCurrent.GetLocationArrivalTime().GetValueOrDefault() == newerCurrent.GetLocationArrivalTime().GetValueOrDefault()
                    && olderCurrent.GetLocationDepartureTime().GetValueOrDefault() == newerCurrent.GetLocationDepartureTime().GetValueOrDefault()
                    && olderCurrent.TrainActivity.Select(a => a.TrainActivityType).ToHashSet().SetEquals(newerCurrent.TrainActivity.Select(a => a.TrainActivityType))
                    && olderCurrent.TrainType == newerCurrent.TrainType && olderCurrent.TrafficType == newerCurrent.TrafficType && olderCurrent.OperationalTrainNumber == newerCurrent.OperationalTrainNumber
                    && olderCurrent.CommercialTrafficType == newerCurrent.CommercialTrafficType && olderCurrent.IsAlternativeTransportOnDeparture() == newerCurrent.IsAlternativeTransportOnDeparture())
                {
                    PrintLocations(text, newerTrain.Locations, newerIndex, newerIndex, ' ');
                }
                else
                {
                    PrintLocations(text, olderTrain.Locations, olderIndex, olderIndex, '-');
                    PrintLocations(text, newerTrain.Locations, newerIndex, newerIndex, '+');
                }

                olderIndex++;
                newerIndex++;
            }

            return text.ToString();
        }

        private int FindFirstOffset(Collection<TrainLocation> locations, int startIndex, TrainLocation search)
        {
            for (int i = startIndex; i < locations.Count; i++)
            {
                if (locations[i].LocationData.Location.LocationPrimaryCode == search.LocationData.Location.LocationPrimaryCode)
                {
                    return i - startIndex;
                }
            }

            return locations.Count - startIndex;
        }

        private void PrintLocations(StringBuilder text, Collection<TrainLocation> locations, int startIndex, int endIndex, char symbol)
        {
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (symbol == '+')
                {
                    text.Append("+++\t");
                }
                else if (symbol == '-')
                {
                    text.Append("----\t");
                }
                else
                {
                    text.Append("\t");
                }

                var location = locations[i].LocationData;
                text.Append(location.GetTrainTypeAndNumber() + "\t");
                text.Append(location.Location.PrimaryLocationName + " ");
                var arrivalTime = location.GetLocationArrivalTime();
                var departureTime = location.GetLocationDepartureTime();
                if (arrivalTime.HasValue && departureTime.HasValue && arrivalTime.Value != departureTime.Value)
                {
                    text.Append(arrivalTime.Value + "-" + departureTime.Value);
                }
                else if (departureTime.HasValue)
                {
                    text.Append(departureTime.Value);
                }
                else if (arrivalTime.HasValue)
                {
                    text.Append(arrivalTime.Value);
                }

                if (location.IsAlternativeTransportOnDeparture())
                    text.Append(" NAD");
                foreach (var activity in location.TrainActivity)
                {
                    text.Append(" " + activity.TrainActivityType);
                }

                text.AppendLine();
            }
        }
    }
}
