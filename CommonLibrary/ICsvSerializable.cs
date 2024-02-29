namespace CommonLibrary
{
    /// <summary>
    /// Povinný interface pro všechny třídy, které by měly být serializovány z/do GTFS nebo obecně CSV.
    /// Netřeba řešit pro základní podporované primitivní typy, ty do CSV umíme serializovat.
    /// </summary>
    public interface ICsvSerializable
    {
        /// <summary>
        /// Načte hodnotu ze stringové reprezentace
        /// </summary>
        /// <param name="str">Stringová reprezentace</param>
        void LoadFromString(string str);


        // K převodu z instance do stringu používáme klasický ToString()
    }
}
