using CommonLibrary;
using System.Collections.Generic;

namespace AswModel.Extended
{
    /// <summary>
    /// Kus trasy mezi dvěma zastávkami (načtený z ASW JŘ)
    /// 
    /// Respektive jedna jeho verze platnosti (viz <see cref="VersionedItemByBitmap{T}"/>)
    /// </summary>
    public class ShapeFragment : IVersionableByBitmap
    {
        /// <summary>
        /// Posloupnost souřadnic
        /// </summary>
        public List<Coordinates> Coordinates { get; private set; }

        /// <summary>
        /// Platnost trasy
        /// </summary>
        public ServiceDaysBitmap ServiceAsBits { get; set; }

        public ShapeFragment()
        {
            Coordinates = new List<Coordinates>();
        }

        public void ExtendServiceDays(ServiceDaysBitmap bitmapToAdd)
        {
            ServiceAsBits = ServiceAsBits.Union(bitmapToAdd);
        }
    }
}
