using AswModel.Extended;
using CommonLibrary;
using GtfsProcessor.DataClasses;
using GtfsProcessor.Logging;
using System.Collections.Generic;
using System.Linq;

namespace GtfsProcessor
{
    /// <summary>
    /// Vytváří GTFS trasy na základě sledu zastávek spojů (<see cref="MergedTripGroup"/>).
    /// Trasy se sestavují z tras v mezizastávkových úsecích obsažených ve spojích. Používá se sled zastávek od
    /// první veřejné po poslední veřejnou (ignorují se neveřejné části na začátku a na konci), neveřejné
    /// zastávky uvnitř spoje se však používají též, protože trasu stavíme po mezizastávkových úsecích
    /// 
    /// Zároveň řeší chyby a loguje nedohledané a nedotažené trasy.
    /// 
    /// Vrací <see cref="ShapeEx"/>, což je rozšíření GTFS shape o vyznačení bodů v místech zastávek
    /// (podle nich se pak stop times dovodí kilometráže - shape_dist_traveled)
    /// </summary>
    class ShapeFragmentConnector
    {
        private ITrajectoryConnectorLogger trajLog = Loggers.TrajectoryConnectorLoggerInstance;
        
        /// <summary>
        /// Vytvoří trasu pro daný spoj (respektive jeho sled zastávek). Vrací výsledek vždy a případně generuje
        /// hlášky do logů.
        /// </summary>
        /// <param name="trip">Spoj</param>
        /// <returns>Trasa</returns>
        public ShapeEx CreateShapeForTrip(MergedTripGroup trip)
        {
            var result = new ShapeEx();
            result.Points = new List<GtfsModel.Extended.ShapePoint>();
            result.ReferenceTrip = trip;
            result.PointsForStopTimes = new GtfsModel.Extended.ShapePoint[trip.StopTimes.Length];
            result.ServiceAsBits = ServiceDaysBitmap.CreateAlwaysValidBitmap(trip.ServiceAsBits.Length); // pro začátek samé jedničky

            Coordinates? shapeEndCoordinate = null;
            ShapeFragmentDescriptor prevDescriptor = null;

            // stop time obsahuje vždy info o trase DO zastávky, čili začínáme až od druhého
            for (int i = 1; i < trip.StopTimes.Length; i++)
            {
                var fragment = trip.StopTimes[i].TrackToThisStop;
                var descriptor = trip.StopTimes[i].TrackVariantDescriptor;

                if (fragment != null)
                {
                    result.ServiceAsBits = result.ServiceAsBits.Intersect(fragment.ServiceAsBits);

                    // byla-li nalezena alespoň nějaká trasa, provedeme napojení
                    double distanceFromLastFragment;
                    GtfsModel.Extended.ShapePoint firstFragmentPoint;
                    ConnectPointsToShape(result.Points, shapeEndCoordinate, fragment.Coordinates, out firstFragmentPoint, out distanceFromLastFragment);
                    if (distanceFromLastFragment > 1)
                    {
                        // fragmenty nenavazují, což znamená, že buď předchozí fragment není dotažený do konce, nebo tento fragment není dotažený na začátku
                        // obecně to ale nejde rozlišit, použijeme jako metriku prostě to, který konec je blíž zastávce, kde se fragmenty mají potkat
                        var distanceFromLastFragmentToStop = shapeEndCoordinate.Value.DistanceTo(trip.StopTimes[i].Stop.Position);
                        var distanceFromThisFragmentToStop = fragment.Coordinates.First().DistanceTo(trip.StopTimes[i].Stop.Position);
                        if (distanceFromThisFragmentToStop < distanceFromLastFragmentToStop)
                        {
                            // tento je blíž, asi předchozí není dotažený
                            trajLog.LogUnconnected(prevDescriptor, distanceFromLastFragment, trip.ReferenceTrip);
                            result.PointsForStopTimes[i - 1] = firstFragmentPoint;
                        }
                        else
                        {
                            trajLog.LogUnconnected(descriptor, distanceFromLastFragment, trip.ReferenceTrip);
                        }
                    }

                    shapeEndCoordinate = fragment.Coordinates.Last();
                }
                else
                {
                    // trasa úplně chybí, uděláme přímé propojení
                    if (!result.Points.Any())
                    {
                        result.Points.Add(new GtfsModel.Extended.ShapePoint()
                        {
                            Position = new GpsCoordinates(descriptor.Source.Position.GpsLatitude, descriptor.Source.Position.GpsLongitude),
                            DistanceTraveledMeters = 0,
                        });

                        shapeEndCoordinate = descriptor.Source.Position;
                    }

                    result.Points.Add(new GtfsModel.Extended.ShapePoint()
                    {
                        Position = new GpsCoordinates(descriptor.Destination.Position.GpsLatitude, descriptor.Destination.Position.GpsLongitude),
                        DistanceTraveledMeters = result.Points.Last().DistanceTraveledMeters + shapeEndCoordinate.Value.DistanceTo(descriptor.Destination.Position),
                    });

                    shapeEndCoordinate = descriptor.Destination.Position;
                }
                
                result.PointsForStopTimes[i] = result.Points.Last();
                prevDescriptor = descriptor;
            }

            result.PointsForStopTimes[0] = result.Points.First();
            return result;
        }

        /// <summary>
        /// Připojí mezizastávkový úsek k trase
        /// </summary>
        /// <param name="shape">Dosavadní trasa</param>
        /// <param name="lastCoordinate">Poslední bod na trase (null, pokud jde o první úsek)</param>
        /// <param name="points">Nový úsek (měl by navazovat na dosavadní trasu)</param>
        /// <param name="firstFragmentPoint">Vrátí první bod této části trasy</param>
        /// <param name="distanceFromPreviousSegment">Vrátí vzdálenost tohoto fragmentu od předchozí části trasy</param>
        private void ConnectPointsToShape(List<GtfsModel.Extended.ShapePoint> shape, Coordinates? lastCoordinate, List<Coordinates> points, 
            out GtfsModel.Extended.ShapePoint firstFragmentPoint, out double distanceFromPreviousSegment)
        {
            firstFragmentPoint = null;
            if (points.Count < 2)
            {
                distanceFromPreviousSegment = double.MaxValue;
                return;
            }

            // vzdálenost zpracovávaného bodu od počátku trasy
            double distTraveled = 0;

            if (shape.Count > 0)
            {
                distanceFromPreviousSegment = lastCoordinate.Value.DistanceTo(points.First());
                distTraveled = shape.Last().DistanceTraveledMeters + distanceFromPreviousSegment;
            }
            else
            {
                distanceFromPreviousSegment = 0.0;
            }

            for (int i = 0; i < points.Count; i++) 
            {
                if (i == 0 && shape.Count > 0 && distanceFromPreviousSegment < 0.5)
                {
                    // pokud je vzdálenost od posledního bodu malá, nebudeme opakovat podobný bod a přeskočíme ho

                    // TODO jen pro ladění, pak jde vyhodit, abychom to měli přesnější
                    distTraveled = shape.Last().DistanceTraveledMeters;
                    points[0] = lastCoordinate.Value;
                    // AŽ SEM

                    // tento bod je tedy propojovací
                    firstFragmentPoint = shape.Last();
                    continue; 
                }
                else if (i > 0)
                {
                    distTraveled += points[i - 1].DistanceTo(points[i]);
                }

                var newPoint = new GtfsModel.Extended.ShapePoint()
                {
                    Position = new GpsCoordinates(points[i].GpsLatitude, points[i].GpsLongitude),
                    DistanceTraveledMeters = distTraveled,
                };

                if (firstFragmentPoint == null)
                {
                    firstFragmentPoint = newPoint;
                }

                shape.Add(newPoint);
            }
        }
    }
}
