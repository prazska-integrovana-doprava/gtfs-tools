using CommonLibrary;

namespace AswModel.Extended
{
    /// <summary>
    /// Rozhraní pro prvky, které chceme verzovat podle platnosti pomocí <see cref="VersionedItemByBitmap{T}"/>.
    /// </summary>
    public interface IVersionableByBitmap
    {
        /// <summary>
        /// Platnost záznamu
        /// </summary>
        ServiceDaysBitmap ServiceAsBits { get; }

        /// <summary>
        /// Rozšíří platnost záznamu
        /// </summary>
        /// <param name="bitmapToAdd">Dny, o které má být platnost rozšířena</param>
        void ExtendServiceDays(ServiceDaysBitmap bitmapToAdd);
    }
}
