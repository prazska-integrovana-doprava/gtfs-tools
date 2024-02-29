using System;

namespace CommonLibrary
{
    public static class MapFunctions
    {
        /// <summary>
        /// převod JTSK na GPS
        /// </summary>
        public static void ConvertJtskToWsg84(double Y, double X, out double lat, out double lon)
        {
            /* Přepočet vstupích údajů - vychazi z nejakeho skriptu, ktery jsem nasel na Internetu - nejsem autorem prepoctu. */
            double H = 0;

            /*Vypocet zemepisnych souradnic z rovinnych souradnic*/
            var a = 6377397.15508;
            var e = 0.081696831215303;
            var n = 0.97992470462083;
            var konst_u_ro = 12310230.12797036;
            var sinUQ = 0.863499969506341;
            var cosUQ = 0.504348889819882;
            var sinVQ = 0.420215144586493;
            var cosVQ = 0.907424504992097;
            var alfa = 1.000597498371542;
            var k = 1.003419163966575;
            var ro = Math.Sqrt(X * X + Y * Y);
            var epsilon = 2 * Math.Atan(Y / (ro + X));
            var D = epsilon / n;
            var S = 2 * Math.Atan(Math.Exp(1 / n * Math.Log(konst_u_ro / ro))) - Math.PI / 2;
            var sinS = Math.Sin(S);
            var cosS = Math.Cos(S);
            var sinU = sinUQ * sinS - cosUQ * cosS * Math.Cos(D);
            var cosU = Math.Sqrt(1 - sinU * sinU);
            var sinDV = Math.Sin(D) * cosS / cosU;
            var cosDV = Math.Sqrt(1 - sinDV * sinDV);
            var sinV = sinVQ * cosDV - cosVQ * sinDV;
            var cosV = cosVQ * cosDV + sinVQ * sinDV;
            var Ljtsk = 2 * Math.Atan(sinV / (1 + cosV)) / alfa;
            var t = Math.Exp(2 / alfa * Math.Log((1 + sinU) / cosU / k));
            var pom = (t - 1) / (t + 1);
            double sinB;
            do
            {
                sinB = pom;
                pom = t * Math.Exp(e * Math.Log((1 + e * sinB) / (1 - e * sinB)));
                pom = (pom - 1) / (pom + 1);
            } while (Math.Abs(pom - sinB) > 1e-15);

            var Bjtsk = Math.Atan(pom / Math.Sqrt(1 - pom * pom));


            /* Pravoúhlé souřadnice ve S-JTSK */
            a = 6377397.15508; var f_1 = 299.152812853;
            var e2 = 1 - (1 - 1 / f_1) * (1 - 1 / f_1); ro = a / Math.Sqrt(1 - e2 * Math.Sin(Bjtsk) * Math.Sin(Bjtsk));
            var x = (ro + H) * Math.Cos(Bjtsk) * Math.Cos(Ljtsk);
            var y = (ro + H) * Math.Cos(Bjtsk) * Math.Sin(Ljtsk);
            var z = ((1 - e2) * ro + H) * Math.Sin(Bjtsk);

            /* Pravoúhlé souřadnice v WGS-84*/
            var dx = 570.69; var dy = 85.69; var dz = 462.84;
            var wz = -5.2611 / 3600 * Math.PI / 180; var wy = -1.58676 / 3600 * Math.PI / 180; var wx = -4.99821 / 3600 * Math.PI / 180; var m = 3.543e-6;
            var xn = dx + (1 + m) * (x + wz * y - wy * z); var yn = dy + (1 + m) * (-wz * x + y + wx * z); var zn = dz + (1 + m) * (wy * x - wx * y + z);

            /* Geodetické souřadnice v systému WGS-84*/
            a = 6378137.0; f_1 = 298.257223563;
            var a_b = f_1 / (f_1 - 1); var p = Math.Sqrt(xn * xn + yn * yn); e2 = 1 - (1 - 1 / f_1) * (1 - 1 / f_1);
            var theta = Math.Atan(zn * a_b / p); var st = Math.Sin(theta); var ct = Math.Cos(theta);
            t = (zn + e2 * a_b * a * st * st * st) / (p - e2 * a * ct * ct * ct);
            var B = Math.Atan(t); var L = 2 * Math.Atan(yn / (p + xn)); H = Math.Sqrt(1 + t * t) * (p - a / Math.Sqrt(1 + (1 - e2) * t * t));

            /* Formát výstupních hodnot */

            B = B / Math.PI * 180;

            lat = B;


            L = L / Math.PI * 180;
            lon = L;


        }

        public const double LonDegreeDistance = 71666.9f;
        public const double LatDegreeDistance = 111174.9f;

        /// <summary>
        /// Přibližná vzdálenost mezi dvěma body v metrech
        /// </summary>
        public static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            var dx = Math.Abs(lon2 - lon1) * LonDegreeDistance;
            var dy = Math.Abs(lat2 - lat1) * LatDegreeDistance;
            return Math.Sqrt(dx * dx + dy * dy);
        }

    }

}
