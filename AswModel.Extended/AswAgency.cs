namespace AswModel.Extended
{
    /// <summary>
    /// Dopravce z číselníku ASW JŘ
    /// </summary>
    public class AswAgency
    {
        /// <summary>
        /// ID dopravce
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Název dopravce
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Adresa sídla dopravce
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Telefonní číslo na dopravce
        /// </summary>
        public string PhoneNumber { get; set; }

        public override string ToString()
        {
            return $"({Id}) {Name}";
        }
    }
}
