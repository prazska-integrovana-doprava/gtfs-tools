using CsvSerializer.Attributes;

namespace GtfsModel
{
    /// <summary>
    /// Jeden dopravce jedné linky (k jedné lince může existovat více záznamů)
    /// </summary>
    public class RouteSubAgency
    {
        /// <summary>
        /// GTFS ID linky
        /// </summary>
        [CsvField("route_id", 1)]
        public string RouteId { get; set; }

        /// <summary>
        /// Licenční (CIS) číslo linky
        /// </summary>
        [CsvField("route_licence_number", 2, CsvFieldPostProcess.None, 0)]
        public int LicenceNumber { get; set; }

        /// <summary>
        /// ID dopravce
        /// </summary>
        [CsvField("sub_agency_id", 3)]
        public int SubAgencyId { get; set; }

        /// <summary>
        /// Název dopravce
        /// </summary>
        [CsvField("sub_agency_name", 4, CsvFieldPostProcess.Quote)]
        public string SubAgencyName { get; set; }

        // když generujeme sub agencies může se od různých verzí vyrobit shodný záznam, tak aby tam nestrašil, chceme umět distinct
        public override bool Equals(object obj)
        {
            var other = obj as RouteSubAgency;
            if (other == null)
                return false;

            return RouteId == other.RouteId && LicenceNumber == other.LicenceNumber && SubAgencyId == other.SubAgencyId && SubAgencyName == other.SubAgencyName;
        }

        public override int GetHashCode()
        {
            return RouteId.GetHashCode() + LicenceNumber.GetHashCode() * 171 + SubAgencyId.GetHashCode() * 1561 + SubAgencyName.GetHashCode() * 19871;
        }
    }
}
