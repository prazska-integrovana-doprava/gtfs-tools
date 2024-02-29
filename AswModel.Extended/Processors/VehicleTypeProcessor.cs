using AswModel.Extended.Logging;
using GtfsLogging;
using JR_XML_EXP;

namespace AswModel.Extended.Processors
{
    /// <summary>
    /// Zpracovává záznamy o typech vozů z ASW JŘ <see cref="TypVozu"/>. Do databáze se ukládají jen informace o nízkopodlažnosti (více toho není zatím potřeba)
    /// (<see cref="TheAswDatabase.VehicleTypeIsWheelchairAccessible"/>). Resolvuje duplicity.
    /// </summary>
    class VehicleTypeProcessor : IProcessor<TypVozu>
    {
        private ICommonLogger dataLog = Loggers.DataLoggerInstance;
        private TheAswDatabase db;

        public VehicleTypeProcessor(TheAswDatabase db)
        {
            this.db = db;
        }

        public void Process(TypVozu xmlVehicleType)
        {
            // dočasně dokud je to ve více souborech
            if (db.VehicleTypeIsWheelchairAccessible.ContainsKey(xmlVehicleType.CTypuVozu))
            {
                dataLog.Assert(db.VehicleTypeIsWheelchairAccessible[xmlVehicleType.CTypuVozu] == xmlVehicleType.Nizkopodlazni,
                    LogMessageType.ERROR_VEHICLE_TYPE_DUPLICATE_AMBIGUOUS, "Dva záznamy stejného vehicle type s rozdílnými informacemi o nízkopodlažnosti. Ignoruji druhý. " + xmlVehicleType.CTypuVozu);
                return;
            }

            db.VehicleTypeIsWheelchairAccessible.Add(xmlVehicleType.CTypuVozu, xmlVehicleType.Nizkopodlazni);
        }
    }
}
