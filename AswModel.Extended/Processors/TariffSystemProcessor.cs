using AswModel.Extended.Logging;
using GtfsLogging;
using JR_XML_EXP;

namespace AswModel.Extended.Processors
{
    /// <summary>
    /// Zpracovává info o tarifním systému z <see cref="IntegrovanySystem"/> a ukládá je do <see cref="TheAswDatabase.TariffSystems"/>.
    /// 
    /// Pokud tarifní systémy nejsou v souboru vůbec zadány, používá výchozí mapu, která obsahuje alespoň PID
    /// </summary>
    class TariffSystemProcessor : IProcessor<IntegrovanySystem>
    {
        private ICommonLogger dataLog = Loggers.DataLoggerInstance;
        private TheAswDatabase db;

        public TariffSystemProcessor(TheAswDatabase db)
        {
            this.db = db;

            if (!db.TariffSystems.ContainsKey(1))
            {
                db.TariffSystems.Add(1, "PID");
            }
        }

        public void Process(IntegrovanySystem item)
        {   
            if (db.TariffSystems.ContainsKey(item.CIDS))
            {
                if (db.TariffSystems[item.CIDS] != item.Zkratka)
                {
                    dataLog.Log(LogMessageType.ERROR_INCONSISTENT_TARRIFS, $"Tarifní systém {item.CIDS} označen jako {db.TariffSystems[item.CIDS]} a {item.Zkratka} (mělo by být shodné ve všech souborech).");
                }

                return;
            }         

            db.TariffSystems.Add(item.CIDS, item.Zkratka);
        }
    }
}
