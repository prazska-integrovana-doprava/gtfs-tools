using AswModel.Extended.Logging;
using CommonLibrary;
using GtfsLogging;
using JR_XML_EXP;
using System.Collections.Generic;

namespace AswModel.Extended.Processors
{
    /// <summary>
    /// Zpracovává trasy z ASW JŘ <see cref="Trajektorie"/> a ukládá je do databáze <see cref="TheAswDatabase.ShapeFragments"/>.
    /// 
    /// Potřebuje už mít načtené zatávky
    /// </summary>
    class ShapeProcessor : IProcessor<Trasa>
    {
        private ICommonLogger dataLog = Loggers.DataLoggerInstance;
        private ITrajectoryDbLogger trajLog = Loggers.TrajectoryDbLoggerInstance;
        private TheAswDatabase db;

        public ShapeProcessor(TheAswDatabase db)
        {
            this.db = db;
        }

        public void Process(Trasa xmlShape)
        {
            if (xmlShape.CUzlu1 == 0 || xmlShape.CUzlu2 == 0 || xmlShape.CZast1 == 0 || xmlShape.CZast2 == 0)
                return;

            // všechny body
            var fragment = new ShapeFragment()
            {
                ServiceAsBits = ServiceDaysBitmap.FromBitmapString(xmlShape.KJ),
            };

            Stop sourceStop, destinationStop;
            db.Stops.FindOrDefault(StopDatabase.CreateKey(xmlShape.CUzlu1, xmlShape.CZast1), fragment.ServiceAsBits, out sourceStop);
            db.Stops.FindOrDefault(StopDatabase.CreateKey(xmlShape.CUzlu2, xmlShape.CZast2), fragment.ServiceAsBits, out destinationStop);
            if (sourceStop == null || destinationStop == null)
            {
                dataLog.Log(LogMessageType.ERROR_TRAJECTORY_UNKNOWN_STOP, "Zdrojovou nebo cílovou zastávku trasy se nepodařilo najít v databázi.", xmlShape);
                return;
            }

            foreach (var point in xmlShape.Traj.Bod)
            {
                var coordinate = ProcessPoint(point);
                if (coordinate.HasValue)
                {
                    fragment.Coordinates.Add(coordinate.Value);
                }
            }

            if (fragment.Coordinates.Count < 2)
            {
                dataLog.Log(LogMessageType.WARNING_TRAJECTORY_TOO_SHORT, "Trajektorie má méně než dva body, ignoruji.", xmlShape);
                return;
            }

            var descriptor = new ShapeFragmentDescriptor(xmlShape.CZavodu, sourceStop, destinationStop, xmlShape.VarTr);
            if (!db.ShapeFragments.AddOrMergeVersion(descriptor, fragment, (f1, f2) => PointsEqual(f1.Coordinates, f2.Coordinates)))
            {
                trajLog.LogDuplicate(descriptor, fragment.ServiceAsBits);
            }
        }

        // načte souřadnici z elementu "bod" (je v S-JTSK)
        public Coordinates? ProcessPoint(Bod point)
        {
            double lat, lon;
            MapFunctions.ConvertJtskToWsg84(-point.X, -point.Y, out lat, out lon);

            return new Coordinates()
            {
                JtskX = point.X,
                JtskY = point.Y,
                GpsLatitude = lat,
                GpsLongitude = lon,
            };
        }

        // Vrací true, pokud jsou trasy stejné (CollectionEquals)
        private static bool PointsEqual(IList<Coordinates> first, IList<Coordinates> second)
        {
            if (first.Count != second.Count)
                return false;

            for (int i = 0; i < first.Count; i++)
            {
                if (first[i] != second[i])
                    return false;
            }

            return true;
        }
    }
}
