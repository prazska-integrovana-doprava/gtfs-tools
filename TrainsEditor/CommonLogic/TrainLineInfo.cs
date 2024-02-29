using System.Text.RegularExpressions;

namespace TrainsEditor.CommonLogic
{
    /// <summary>
    /// Popisuje linku IDS
    /// </summary>
    class TrainLineInfo
    {
        /// <summary>
        /// Singleton pro nedefinovanou linku
        /// </summary>
        public static readonly TrainLineInfo UndefinedLineInfoInstance = new TrainLineInfo(TrainLineType.Undefined, null, 0);

        /// <summary>
        /// Číselná řada (expres, rychlík, esko PID, ...)
        /// </summary>
        public TrainLineType LineType { get; private set; }

        /// <summary>
        /// Plný název linky (např. "S1" nebo "Ex7")
        /// </summary>
        public string LineName { get; private set; }

        /// <summary>
        /// ID linky dle číselníku SŽ
        /// </summary>
        public int LineTrIdentification { get; private set; }

        // pozor, že vrátí true, neznamená, že jde o PIDovou linku - cílem je jen vyloučit například brněnské S1 apod.
        // vrací true u expresů, rychlíků a vlaků z PIDu a okolních regionů
        public bool IsNonPidLine
        {
            get
            {
                return LineType == TrainLineType.Unknown || LineType == TrainLineType.Undefined || LineType == TrainLineType.Odis;
            }
        }

        public TrainLineInfo(TrainLineType lineType, string lineName, int lineTrIdentification)
        {
            LineType = lineType;
            LineName = lineName;
            LineTrIdentification = lineTrIdentification;
        }


        public override bool Equals(object obj)
        {
            var other = obj as TrainLineInfo;
            if (other == null)
                return false;

            return LineType == other.LineType && LineName == other.LineName && LineTrIdentification == other.LineTrIdentification;
        }

        public override int GetHashCode()
        {
            return LineType.GetHashCode() + LineName.GetHashCode() * 11 + LineTrIdentification.GetHashCode() * 373;
        }

        public override string ToString()
        {
            return $"{LineType} {LineName} ({LineTrIdentification})";
        }

        public static bool operator == (TrainLineInfo first, TrainLineInfo second)
        {
            return ReferenceEquals(first, second) || (!ReferenceEquals(first, null) && first.Equals(second));
        }

        public static bool operator != (TrainLineInfo first, TrainLineInfo second)
        {
            return !ReferenceEquals(first, second) && (ReferenceEquals(first, null) || !first.Equals(second));
        }

        /// <summary>
        /// Vytvoří instanci <see cref="TrainLineInfo"/> z číselníkové hodnoty SŽ (ta obsahuje informaci o IDS+lince)
        /// </summary>
        /// <param name="trainLineNumberCode">Číslo linky dle číselníku linek SŽ</param>
        public static TrainLineInfo TrainLineNumberToName(int trainLineNumberCode)
        {
            if (trainLineNumberCode <= 7)
            {
                return new TrainLineInfo(TrainLineType.Express, $"Ex{trainLineNumberCode}", trainLineNumberCode);
            }
            else if (trainLineNumberCode < 100)
            {
                return new TrainLineInfo(TrainLineType.FastTrain, $"R{trainLineNumberCode % 100}", trainLineNumberCode);
            }
            else if (trainLineNumberCode > 1100 && trainLineNumberCode < 1200)
            {
                return new TrainLineInfo(TrainLineType.PidFastTrain, $"R{trainLineNumberCode % 100}", trainLineNumberCode);
            }
            else if (trainLineNumberCode > 1000 && trainLineNumberCode < 1100)
            {
                return new TrainLineInfo(TrainLineType.Pid, $"S{trainLineNumberCode % 100}", trainLineNumberCode);
            }
            else if (trainLineNumberCode > 2000 && trainLineNumberCode < 2100)
            {
                return new TrainLineInfo(TrainLineType.Pid, $"T{trainLineNumberCode % 100}", trainLineNumberCode);
            }
            else if (trainLineNumberCode > 4000 && trainLineNumberCode < 4100)
            {
                return new TrainLineInfo(TrainLineType.Duk, $"U{trainLineNumberCode % 100}", trainLineNumberCode);
            }
            else if (trainLineNumberCode > 5000 && trainLineNumberCode < 5100)
            {
                return new TrainLineInfo(TrainLineType.Idol, $"L{trainLineNumberCode % 100}", trainLineNumberCode);
            }
            else if (trainLineNumberCode > 5200 && trainLineNumberCode < 5300)
            {
                return new TrainLineInfo(TrainLineType.Iredo, $"V{trainLineNumberCode % 100}", trainLineNumberCode);
            }
            else if (trainLineNumberCode > 8000 && trainLineNumberCode < 8100)
            {
                return new TrainLineInfo(TrainLineType.Odis, $"S{trainLineNumberCode % 100}", trainLineNumberCode);
            }
            else
            {
                return new TrainLineInfo(TrainLineType.Unknown, "", trainLineNumberCode);
            }
        }
        
        /// <summary>
        /// Převede stringový název linky na číslo dle číselníku SŽ. Funguje korektně pouze pro PID (kvůli kolizím, "S1" se nedá jednoznačně určit, jestli je pražská, brněnská nebo ostravská).
        /// </summary>
        /// <param name="lineName">Název linky (např. "S1" nebo "Ex7")</param>
        public static int TrainLineNameToNumberPid(string lineName)
        {
            var regex = new Regex("([A-Za-z]+)([0-9]+)");
            var match = regex.Match(lineName);
            if (match.Success)
            {
                var lineType = match.Groups[1].Value;
                var lineNumber = int.Parse(match.Groups[2].Value);
                if (lineNumber == 0 || lineNumber >= 100)
                {
                    return 0;
                }

                if (lineType == "Ex" && lineNumber <= 7)
                {
                    return lineNumber;
                }
                else if (lineType == "R" && lineNumber < 40)
                {
                    return lineNumber;
                }
                else if (lineType == "R")
                {
                    return 1100 + lineNumber;
                }
                else if (lineType == "T")
                {
                    return 2000 + lineNumber;
                }
                else if (lineType == "S")
                {
                    return 1000 + lineNumber;
                }
                else if (lineType == "U")
                {
                    return 4000 + lineNumber;
                }
                else if (lineType == "L")
                {
                    return 5000 + lineNumber;
                }
                else if (lineType == "V")
                {
                    return 5200 + lineNumber;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }
    }
}
