using JR_XML_EXP;

namespace AswModel.Extended.Processors
{
    /// <summary>
    /// Zpracovává <see cref="Dopravce"/> z ASW JŘ a přidává je do databáze <see cref="TheAswDatabase.Agencies"/>
    /// </summary>
    class AgencyProcessor : IProcessor<Dopravce>
    {
        private TheAswDatabase db;

        public AgencyProcessor(TheAswDatabase db)
        {
            this.db = db;
        }

        public void Process(Dopravce xmlAgency)
        {
            var agency = new AswAgency()
            {
                Id = xmlAgency.CDopravce,
                Name = xmlAgency.Nazev,
                Address = $"{xmlAgency.Ulice}, {xmlAgency.PSC}, {xmlAgency.Mesto}",
                PhoneNumber = xmlAgency.Telefon,
            };

            if (db.Agencies.ContainsKey(agency.Id))
            {
                return; // už v databázi je
            }

            db.Agencies.Add(agency.Id, agency);
        }
    }
}
