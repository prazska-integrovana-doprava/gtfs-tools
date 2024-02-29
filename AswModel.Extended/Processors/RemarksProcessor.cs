using AswModel.Extended.Logging;
using GtfsLogging;
using JR_XML_EXP;

namespace AswModel.Extended.Processors
{
    /// <summary>
    /// Načítá poznámky ke spojům <see cref="Poznamka"/> z ASW JŘ a ukládá je ke spojům v <see cref="AswSingleFileFeed.Trips"/>
    /// 
    /// Potřebuje už mít načtené linky, zastávky a spoje
    /// </summary>
    public class RemarksProcessor : IProcessor<Poznamka>
    {
        private TheAswDatabase db;
        private AswSingleFileFeed feedFile;

        private ICommonLogger dataLog = Loggers.DataLoggerInstance;

        public RemarksProcessor(AswSingleFileFeed feedFile, TheAswDatabase db)
        {
            this.feedFile = feedFile;
            this.db = db;
        }

        public void Process(Poznamka xmlRemark)
        {
            Remark remark = new Remark()
            { 
                Id = xmlRemark.PozID,
                Symbol1 = xmlRemark.Zkratka1,
                Text = xmlRemark.Text,
            };

            if (xmlRemark.TypNavazne == "m")
            {
                // tento spoj navazuje na jiný
                var firstStop = db.Stops.Find(StopDatabase.CreateKey(xmlRemark.CUzlu, xmlRemark.CZast));
                if (firstStop != null)
                {
                    remark.IsTimedTransfer = true;
                    remark.FromStop = new StopRef(xmlRemark.CUzlu2, xmlRemark.CZast2);
                    remark.ToStop = firstStop; // ano, je to schválně opačně, druhá zastávka je zastávka, kam přijede spoj, na který navazuji (pro poznámky "m")
                    remark.FromRouteLineNumber = xmlRemark.CLinky2;
                    remark.FromRouteStopDirection = new StopRef(xmlRemark.CUzluSmer, xmlRemark.CZastSmer);
                    remark.MinimumTransferTimeSeconds = xmlRemark.MinDoba;
                    remark.MaximumWaitingTimeSeconds = xmlRemark.CekaciDoba;
                }
                else
                {
                    dataLog.Log(LogMessageType.WARNING_TRANSFER_UNKNOWN_STOP, "Zastávka spoje, ve které se má odehrát přestup, nebyla nalezena. Záznam nebude považován za přestupní vazbu.", xmlRemark);
                }
            }
            else
            {
                // není návazná, uložíme si jen symbol a text (ale zatím se to k ničemu nepoužívá)
            }

            feedFile.Remarks.Add(remark.Id, remark);
        }
    }
}
