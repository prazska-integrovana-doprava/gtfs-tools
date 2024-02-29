using AswModel.Extended.Logging;
using CommonLibrary;
using GtfsLogging;
using JR_XML_EXP;
using System;

namespace AswModel.Extended.Processors
{
    /// <summary>
    /// Zpracovává <see cref="Grafikon"/>y z ASW JŘ a přidává je do databáze <see cref="TheAswDatabase.Graphs"/>
    /// </summary>
    class GraphProcessor : IProcessor<Grafikon>
    {
        private ICommonLogger dataLog = Loggers.DataLoggerInstance;
        private TheAswDatabase db;

        public GraphProcessor(TheAswDatabase db)
        {
            this.db = db;
        }

        public void Process(Grafikon xmlGraph)
        {
            var graphId = new GraphIdAndCompany()
            {
                GraphId = xmlGraph.GrafID,
                CompanyId = xmlGraph.CZavodu,
            };

            if (db.Graphs.ContainsKey(graphId))
            {
                dataLog.Log(LogMessageType.ERROR_GRAPH_REPEATED_NUMBER, $"Opakované číslo grafikonu {xmlGraph.GrafID}, ignoruji.");
                return;
            }

            var graph = new Graph()
            { 
                Id = graphId,
                ValidityRange = ServiceDaysBitmap.FromBitmapString(xmlGraph.KJ),
            };

            graph.DaysInWeek[(int)DayOfWeek.Monday] = xmlGraph.ProvozniDen[0] == '1';
            graph.DaysInWeek[(int)DayOfWeek.Tuesday] = xmlGraph.ProvozniDen[1] == '1';
            graph.DaysInWeek[(int)DayOfWeek.Wednesday] = xmlGraph.ProvozniDen[2] == '1';
            graph.DaysInWeek[(int)DayOfWeek.Thursday] = xmlGraph.ProvozniDen[3] == '1';
            graph.DaysInWeek[(int)DayOfWeek.Friday] = xmlGraph.ProvozniDen[4] == '1';
            graph.DaysInWeek[(int)DayOfWeek.Saturday] = xmlGraph.ProvozniDen[5] == '1';
            graph.DaysInWeek[(int)DayOfWeek.Sunday] = xmlGraph.ProvozniDen[6] == '1';

            db.Graphs.Add(graphId, graph);
        }
    }
}
