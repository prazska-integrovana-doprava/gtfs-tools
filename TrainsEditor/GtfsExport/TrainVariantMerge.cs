using CommonLibrary;
using GtfsLogging;
using System.Linq;
using TrainsEditor.ExportModel;
using static CommonLibrary.EnumerableExtensions;

namespace TrainsEditor.GtfsExport
{
    /// <summary>
    /// Porovnává vlaky podle obsahu. Slouží ke sloučení více variant vlaku, které ale mají identický jízdní řád
    /// </summary>
    class TrainVariantMerge : ICompareAndMerge<Train>
    {
        private ISimpleLogger log;

        public TrainVariantMerge(ISimpleLogger log)
        {
            this.log = log;
        }

        /// <summary>
        /// Vrací true, pokud jde o stejný spoj (jedou ve stejné časy přes stejné zastávky). Lišit se mohou jen kalendářem.
        /// </summary>
        public bool AreIdentical(Train first, Train second)
        {
            var enuFirst = first.StopTimes.GetEnumerator();
            var enuSecond = second.StopTimes.GetEnumerator();

            // cyklus končí jakmile v jednom ze seznamů dojdou zastávky
            bool isNextFirst, isNextSecond;
            while ((isNextFirst = enuFirst.MoveNext()) & (isNextSecond = enuSecond.MoveNext()))
            {
                if (!CompareStopTimes(enuFirst.Current, enuSecond.Current, enuFirst.Current == first.StopTimes.First(), enuFirst.Current == first.StopTimes.Last()))
                {
                    return false;
                }
            }

            // pokud skončily oba seznamy zastávek najednou, pak je to v pořádku
            return !isNextFirst && !isNextSecond;
        }

        /// <summary>
        /// Vrací true, pokud jde o stejná zastavení (stejný čas, zastávka a způsob zastavení)
        /// </summary>
        public bool CompareStopTimes(StationTime x, StationTime y, bool isFirst, bool isLast)
        {
            return x.Stop == y.Stop && (x.TrainLineOnDeparture == y.TrainLineOnDeparture || isLast) && (x.TrainNumberOnDeparture == y.TrainNumberOnDeparture || isLast)
                && (x.IsSubstituteTransportOnDeparture == y.IsSubstituteTransportOnDeparture || isLast)
                && (x.ArrivalTime == y.ArrivalTime || isFirst) && (x.DepartureTime == y.DepartureTime || isLast)
                && (x.DropOffType == y.DropOffType || isLast) && (x.PickupType == y.PickupType || isFirst);
        }

        // zamerguje 'trainToMerge' do 'train'
        public void MergeSecondIntoFirst(Train train, Train trainToMerge)
        {
            log.Log($"MERGE: {train.TrIdCompanyAndCoreAndYear}: {train.TrIdVariant} od {train.StartDate:d.M.yyyy} + {trainToMerge.TrIdVariant} od {trainToMerge.StartDate:d.M.yyyy}");
            var commonStartDate = train.StartDate < trainToMerge.StartDate ? train.StartDate : trainToMerge.StartDate;
            var firstBitmap = Enumerable.Repeat(false, (train.StartDate - commonStartDate).Days).Concat(train.ServiceBitmap).ToArray();
            var secondBitmap = Enumerable.Repeat(false, (trainToMerge.StartDate - commonStartDate).Days).Concat(trainToMerge.ServiceBitmap).ToArray();
            train.ServiceBitmap = new ServiceDaysBitmap(firstBitmap).Union(new ServiceDaysBitmap(secondBitmap));
            train.StartDate = commonStartDate;
            train.TrIdVariant += $"+{trainToMerge.TrIdVariant}";
        }
    }
}
