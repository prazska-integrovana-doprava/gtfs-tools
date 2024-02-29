namespace GtfsModel.Enumerations
{
    /// <summary>
    /// Možnost převézt kolo na spoji
    /// </summary>
    public enum BikeAccessibility
    {
        Unknown = 0,
        Possible = 1,
        NotPossible = 2,

        // extenze
        AllowedToStayOnBoard = 3,
        PickupOnly = 4,
        DropOffOnly = 5,
    }
}
