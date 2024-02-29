using System;
using System.Linq;

namespace AswModel.Extended
{
    /// <summary>
    /// Databáze zastávek. Jako klíč se používá kombinace uzel+zastávka (ke konstrukci se využije metoda <see cref="CreateKey(int, int)"/>).
    /// 
    /// O pásmovém zařazení více viz <see cref="ZoneType"/>.
    /// </summary>
    public class StopDatabase : BaseCollectionOfVersionedItems<int, Stop>
    {
        /// <summary>
        /// Transformuje ID zastávky do typu, který je používaný jako klíč v kolekci
        /// </summary>
        /// <param name="nodeNumber">Číslo uzlu</param>
        /// <param name="stopNumber">Číslo sloupku</param>
        /// <returns>Klíč použitelný v databázi</returns>
        public static int CreateKey(int nodeNumber, int stopNumber)
        {
            if (stopNumber >= 10000)
                throw new ArgumentException("Stop number must not be greater than 9999");
            return nodeNumber * 10000 + stopNumber;
        }

        /// <summary>
        /// Vrátí všechny verze, které k této zastávce existují. Pokud zastávka v databázi není, vratí null.
        /// </summary>
        /// <param name="stop">Konkrétní verze zastávky</param>
        public VersionedItemByBitmap<Stop> GetAllVersions(Stop stop)
        {
            var result = Find(CreateKey(stop.NodeId, stop.StopId));
            if (result != null && result.AllVersions().Contains(stop))
            {
                // museli jsme zkontrolovat, jestli instance zastávky v poli opravdu je a není vzatá odjinud
                // (třeba z jiného souboru, v takovém případě bychom vraceli null)
                return result;
            }
            else
            {
                return null;
            }
        }
    }
}
