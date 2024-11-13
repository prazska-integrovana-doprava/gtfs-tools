using CommonLibrary;
using GtfsModel.Extended;
using Microsoft.Office.Core;
using StopTimetableGen.StopTimetableModel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Excel = Microsoft.Office.Interop.Excel;

namespace StopTimetableGen.Printers
{
    /// <summary>
    /// Vypisuje <see cref="LineTimetables"/> do standardizovaného excelu
    /// </summary>
    class ExcelPrinter
    {
        // navigace pro přestupní stanice metra (ručně určujeme, ke které stanici patří která linka)
        // TODO mělo by pak správně být obecné a již v modelu (např. jako speciální druh remarku)
        // TODO revidovat až budou zapojena data SŽDC, jestli se nezměnily názvy stanic
        private readonly Dictionary<string, string> metroTransferStations = new Dictionary<string, string>()
        {
            { "PRAHA-VELESLAVÍN", "A" },
            { "PRAHA-DEJVICE", "A" },
            { "PRAHA-JINONICE", "B" },
            { "PRAHA-SMÍCHOV", "B" },
            { "PRAHA MASARYKOVO NÁDR.", "B" },
            { "PRAHA-VYSOČANY", "B" },
            { "PRAHA-RAJSKÁ ZAHRADA", "B" },
            { "PRAHA-HOLEŠOVICE", "C" },
            { "PRAHA-BUBNY VLTAVSKÁ", "C" },
            { "PRAHA-BUBNY", "C" },
            { "PRAHA HL.N.", "C" },
            { "PRAHA-KAČEROV", "C" },
        };

        public static ExcelTemplate[] Templates { get; private set; }

        public event EventHandler<int> OnPrintProgressChange;

        private static readonly Color SelectedStopBackgroundColor = Color.FromArgb(64, 64, 64);
        private static readonly Color PastStopTextColor = Color.FromArgb(96, 96, 96);

        private const string HiddenLineNumberCell = "F1";
        private const string HiddenBeginningOfValidityCell = "G1";
        private const string HiddenNodeNumberCell = "H1";
        private const string HiddenStopNumberCell = "I1";
        private const string HiddenStationNameCell = "J1";
        private const string HiddenDirectionNameCell = "K1";
        private const string HiddenDirectionCell = "L1";
        private const string HiddenAliasCell = "N1";
        private const string HiddenStopIndexCell = "O1";
        private const string HiddenTrafficTypeCell = "P1";
        private const string HiddenGenerationDateCell = "S1";
        private const string HiddenCompanyIdCell = "U1";
        private const string HiddenIsExceptionalCell = "V1";
        private const string HiddenIsHolidayCell = "AE1";
        private const string HiddenIsShortTermCell = "AF1";
        private const string HiddenEndOfValidityCell = "AG1";

        private LineTimetables lineTimetables;
        private ExcelTemplate template;
        private string outputFolder;
        private bool ignoreWheelchairAccessibleAttribute;
        
        public static void InitTemplates(string templatesFolder)
        {
            Templates = new ExcelTemplate[]
            {
                new A5ExcelTemplate(templatesFolder),
                new A5_2ColExcelTemplate(templatesFolder),
                new A5_2Col11TimeslotsExcelTemplate(templatesFolder),
                //new A5HighExcelTemplate(templatesFolder),
                new A4ExcelTemplate(templatesFolder),
            };
        }

        public ExcelPrinter(LineTimetables lineTimetables, ExcelTemplate template, string outputFolder, bool ignoreWheelchairAccessibleAttribute)
        {
            this.lineTimetables = lineTimetables;
            this.template = template;
            this.outputFolder = outputFolder;
            this.ignoreWheelchairAccessibleAttribute = ignoreWheelchairAccessibleAttribute;
        }

        public void PrintToExcel()
        {
            var xlApp = new Excel.Application();
            if (xlApp == null)
            {
                throw new Exception("Excel is not properly installed!!");
            }
            
            var xlWorkBook = xlApp.Workbooks.Open(template.TemplateFileName);
            var xlTemplateWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets[1];
            for (int i = 1; i < lineTimetables.StopTimetables.Count; i++)
            {
                xlTemplateWorkSheet.Copy(xlTemplateWorkSheet);
                if (OnPrintProgressChange != null)
                    OnPrintProgressChange.Invoke(this, (i + 1) * 10 / lineTimetables.StopTimetables.Count);
            }

            for (int i = 0; i < lineTimetables.StopTimetables.Count; i++)
            {
                var stopTimetable = lineTimetables.StopTimetables[i];
                PrintStopTimetable(xlWorkBook.Worksheets[i + 1], stopTimetable, lineTimetables, i + 1);
                if (OnPrintProgressChange != null)
                    OnPrintProgressChange.Invoke(this, 10 + (i + 1) * 90 / lineTimetables.StopTimetables.Count);
            }

            xlApp.DisplayAlerts = false;
            var path = $"{Path.Combine(outputFolder, lineTimetables.LineNumber)}.xlsm";
            xlWorkBook.SaveAs(path, Excel.XlFileFormat.xlOpenXMLWorkbookMacroEnabled);
            xlWorkBook.Close();
            xlApp.Quit();
            System.Diagnostics.Process.Start(path);

            //Marshal.ReleaseComObject(xlTemplateWorkSheet);
            //Marshal.ReleaseComObject(xlWorkBook);
            //Marshal.ReleaseComObject(xlApp);

        }

        // zpracovává jeden list (jízdní řád z jedné zastávky v jednom směru
        private void PrintStopTimetable(Excel.Worksheet xlWorksheet, StopTimetable stopTimetable, LineTimetables lineData, int stopIndex)
        {
            var allTripsWheelchairAccessible = stopTimetable.AreAllTripsWheelchairAccessible();
            xlWorksheet.Name = new string($"{stopTimetable.Stop.AswNodeId}_{stopTimetable.Stop.AswStopId}-{stopTimetable.Direction}-{stopTimetable.SrcStopName}".Take(31).ToArray());
            var lineNumberCell = xlWorksheet.GetCells(ExcelTemplate.LineNumberCell);
            if (!allTripsWheelchairAccessible || ignoreWheelchairAccessibleAttribute)
            {
                lineNumberCell.Value = stopTimetable.OwnerLine.LineNumber;
            }
            else
            {
                // plně bezbariérová linka má před číslem symbol invalidy
                lineNumberCell.Value = $"H{stopTimetable.OwnerLine.LineNumber}";
                lineNumberCell.Characters[1, 1].Font.Name = "Timetable";
                lineNumberCell.Characters[1, 1].Font.Size = 24;
            }

            xlWorksheet.GetCells(ExcelTemplate.DirectionCell).Value = stopTimetable.Stops.Last().Name.ToUpper();
            xlWorksheet.GetCells(template.ValidityStartDateCell).Value = $"'{stopTimetable.OwnerLine.ValidFrom.ToString("d. M. yyyy")}";
            xlWorksheet.GetCells(ExcelTemplate.OperatorNameCell).Value = $"Dopravce: {stopTimetable.OwnerLine.OperatorName}";
            PrintStops(xlWorksheet, stopTimetable.Stops);

            IEnumerable<IRemark> remarks = stopTimetable.Remarks;
            if (!ignoreWheelchairAccessibleAttribute && !allTripsWheelchairAccessible && stopTimetable.IsAnyTripWheelchairAccessible())
            {
                remarks = new IRemark[] { new ManualRemark(null, "__Podtržené spoje__ jsou vhodné pro přepravu cestujících na vozíku.") }.Union(remarks);
            }
            else if (!ignoreWheelchairAccessibleAttribute && allTripsWheelchairAccessible)
            {
                remarks = new IRemark[] { new ManualRemark(null, "Všechny spoje jsou vhodné pro přepravu cestujících na vozíku.") }.Union(remarks);
            }

            if (remarks.Any() && remarks.First() is SeparatorRemark)
            {
                remarks = remarks.Skip(1);
            }

            PrintRemarks(xlWorksheet, remarks, lineData, stopTimetable.Stops.Count);
            
            PrintDepartureTables(xlWorksheet, stopTimetable, allTripsWheelchairAccessible);

            // vyplnění skrytých políček; musí být až na konci, aby byly už hotové čachry se sloupečky
            xlWorksheet.GetCells(HiddenLineNumberCell).Value = lineTimetables.LineId;
            xlWorksheet.GetCells(HiddenBeginningOfValidityCell).Value = lineData.ValidFrom.ToString("yyyyMMdd");
            xlWorksheet.GetCells(HiddenNodeNumberCell).Value = stopTimetable.Stop.AswNodeId;
            xlWorksheet.GetCells(HiddenStopNumberCell).Value = stopTimetable.Stop.AswStopId;
            xlWorksheet.GetCells(HiddenStationNameCell).Value = stopTimetable.SrcStopName;
            xlWorksheet.GetCells(HiddenDirectionNameCell).Value = stopTimetable.Stops.Last().Name;
            xlWorksheet.GetCells(HiddenDirectionCell).Value = stopTimetable.Direction == 0 ? -1 : 0;
            xlWorksheet.GetCells(HiddenAliasCell).Value = lineData.LineNumber;
            xlWorksheet.GetCells(HiddenStopIndexCell).Value = $"{stopIndex}|{stopIndex}";
            xlWorksheet.GetCells(HiddenTrafficTypeCell).Value = 5;
            xlWorksheet.GetCells(HiddenGenerationDateCell).Value = DateTime.Now.Date;
            xlWorksheet.GetCells(HiddenCompanyIdCell).Value = lineTimetables.OperatorId;
            xlWorksheet.GetCells(HiddenIsExceptionalCell).Value = 0;
            xlWorksheet.GetCells(HiddenIsHolidayCell).Value = 0;
            xlWorksheet.GetCells(HiddenIsShortTermCell).Value = 0;
            xlWorksheet.GetCells(HiddenEndOfValidityCell).Value = new DateTime(9999, 12, 31);
        }

        private void PrintStops(Excel.Worksheet xlWorksheet, List<StopOnLine> stops)
        {
            var columns = template.GetStopCells(stops.Count);
            var startIndex = 0;
            for (int i = 0; i < columns.Length; i++)
            {
                var numStopsToPrint = stops.Count / columns.Length;
                if (i < stops.Count % columns.Length)
                    numStopsToPrint++;

                PrintStopList(xlWorksheet, stops.Skip(startIndex).Take(numStopsToPrint), columns[i], i < columns.Length - 1);
                startIndex += numStopsToPrint;
            }
        }

        // zpracovává seznam zastávek v jednom sloupci
        private void PrintStopList(Excel.Worksheet xlWorksheet, IEnumerable<StopOnLine> stops, ExcelTemplate.StopSectionDescriptor section, bool addSeparatorLine)
        {
            var originCell = CellRef.FromCellCode(section.OriginCell);
            foreach (var stop in stops)
            {
                var travelTimeCell = xlWorksheet.GetCells(originCell);
                travelTimeCell.Value = stop.TravelTimeMinutes;

                if (stop.OnRequestMode == StopOnLine.OnRequestStatus.OnRequest)
                {
                    xlWorksheet.GetCells(originCell.Row, originCell.Column + ExcelTemplate.RequestStopAttrColumnDelta).Value = "x";
                }

                // výchozí a cílové zastávky a zastávky, kde končí nějaké spoje jsou velkým písmem
                var stopName = stop.Name;
                if ((stop.Flags & (StopOnLine.StopFlags.FirstStop | StopOnLine.StopFlags.LastStop | StopOnLine.StopFlags.SomeTripsEndHere)) != StopOnLine.StopFlags.None)
                {
                    stopName = stopName.ToUpper();
                }

                var stopNameCell = xlWorksheet.GetCells(originCell.Move(0, ExcelTemplate.StopNameColumnDelta), 1, ExcelTemplate.StopZoneColumnDelta - ExcelTemplate.StopNameColumnDelta);
                stopNameCell.Merge();
                var metroTransferLine = metroTransferStations.GetValueOrDefault(stopName.ToUpper());
                if (metroTransferLine != null)
                {
                    // přidání znaku přestupu na metro
                    stopNameCell.Value = $"{stopName} # {metroTransferLine}";
                    stopNameCell.Characters[stopName.Length + 2, 1].Font.Name = "Timetable";
                }
                else
                {
                    stopNameCell.Value = stopName;
                }

                if (addSeparatorLine)
                {
                    var separatorCell = xlWorksheet.GetCells(originCell.Move(0, ExcelTemplate.StopsSectionWidth));
                    separatorCell.Borders[Excel.XlBordersIndex.xlEdgeRight].LineStyle = Excel.XlLineStyle.xlContinuous;
                    separatorCell.Borders[Excel.XlBordersIndex.xlEdgeRight].Weight = Excel.XlBorderWeight.xlHairline;
                }

                xlWorksheet.GetCells(originCell.Row, originCell.Column + ExcelTemplate.StopZoneColumnDelta).Value = stop.Zones;
                var cellRange = xlWorksheet.GetCells(originCell, 1, ExcelTemplate.StopsSectionWidth);
                if (stop.StopClassification == StopOnLine.StopClass.CurrentStop)
                {
                    travelTimeCell.Value = "â";
                    travelTimeCell.Font.Name = "Wingdings";
                    cellRange.Font.FontStyle = "bold";
                    cellRange.Font.Color = ColorTranslator.ToOle(Color.White);
                    cellRange.Interior.Color = ColorTranslator.ToOle(SelectedStopBackgroundColor);
                }
                else if (stop.StopClassification == StopOnLine.StopClass.PastStop)
                {
                    travelTimeCell.Value = "";
                    cellRange.Font.Color = ColorTranslator.ToOle(PastStopTextColor);
                }

                originCell.Row++;
            }
        }

        // tisk poznámek a dopravce (pod seznam zastávek)
        private void PrintRemarks(Excel.Worksheet xlWorksheet, IEnumerable<IRemark> remarks, LineTimetables lineData, int stopCount)
        {
            var remarkSection = template.GetRemarkCells(stopCount);
            var remarkCells = xlWorksheet.GetCells(remarkSection.OriginCell, CellRef.FromCellCode(remarkSection.BottomRightCell));
            remarkCells.Merge();
            remarkCells.Font.Size = 8;

            var text = new StringBuilder();
            var underlinedSections = new List<Tuple<int, int>>(); // začátek (indexováno od 1) a délka
            foreach (var remark in remarks)
            {
                if (text.Length > 0)
                    text.Append("\n");

                if (!string.IsNullOrEmpty(remark.Symbol))
                {
                    text.Append(remark.Symbol);
                    text.Append(" - ");
                }

                var remarkText = remark.Text;
                while (remarkText.Contains("__"))
                {
                    var startIndex = remarkText.IndexOf("__");
                    var endIndex = remarkText.IndexOf("__", startIndex + 2);
                    if (startIndex != -1 && endIndex != -1)
                    {
                        underlinedSections.Add(new Tuple<int, int>(startIndex + text.Length + 1, endIndex - startIndex - 2));
                        remarkText = remarkText.Substring(0, startIndex) + remarkText.Substring(startIndex + 2, endIndex - startIndex - 2) + remarkText.Substring(endIndex + 2);
                    }
                    else
                    {
                        remarkText.Replace("__", "");
                    }
                }

                text.Append(remarkText);
            }

            var cell = xlWorksheet.GetCells(remarkSection.OriginCell);
            cell.VerticalAlignment = XlVAlign.xlVAlignTop;
            cell.Value = text.ToString();
            foreach (var underlinedSection in underlinedSections)
            {
                cell.Characters[underlinedSection.Item1, underlinedSection.Item2].Font.Underline = true;
            }
        }
        /*
        private IEnumerable<string> SplitTextToLines(string remarkText, int maxWidth)
        {
            var words = remarkText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var buffer = new StringBuilder(words[0]);
            for (int i = 1; i < words.Length; i++)
            {
                if (buffer.Length + 1 + words[i].Length > maxWidth)
                {
                    yield return buffer.ToString();
                    buffer = new StringBuilder(words[i]);
                }
                else
                {
                    buffer.Append(' ');
                    buffer.Append(words[i]);
                }
            }

            yield return buffer.ToString();
        }*/

        // zpracovává část s odjezdy na jednom listu
        private void PrintDepartureTables(Excel.Worksheet xlWorksheet, StopTimetable stopTimetable, bool areAllTripsWheelchairAccessible)
        {
            var timetableSections = template.GetDepartureSections();
            if (stopTimetable.DayColumns.Count <= timetableSections.Length)
            {
                // provozních dnů je stejně (nebo méně) než chlívků v šabloně, rozhodíme jednoduše 1:1
                if (stopTimetable.DayColumns.Count < timetableSections.Length)
                {
                    // TODO warn
                }

                for (int i = 0; i < stopTimetable.DayColumns.Count; i++)
                {
                    PrintDepartureTable(xlWorksheet, stopTimetable, areAllTripsWheelchairAccessible, timetableSections[i], stopTimetable.DayColumns[i]);
                }
            }
            else if (timetableSections.Length == 1)
            {
                // všechny provozní dny se musí vejít do jednoho chlívku, který se rozdělí
                PrintDepartureTable(xlWorksheet, stopTimetable, areAllTripsWheelchairAccessible, timetableSections.First(), stopTimetable.DayColumns.ToArray());
            }
            else if (timetableSections.Length == 2)
            {
                // 3+ provozní dny do dvou chlívků - dělí se vždy nejdříve spodní, pak vrchní chlívek
                PrintDepartureTable(xlWorksheet, stopTimetable, areAllTripsWheelchairAccessible, timetableSections[0],
                    stopTimetable.DayColumns.Take(stopTimetable.DayColumns.Count / 2).ToArray());
                PrintDepartureTable(xlWorksheet, stopTimetable, areAllTripsWheelchairAccessible, timetableSections[1],
                    stopTimetable.DayColumns.Skip(stopTimetable.DayColumns.Count / 2).ToArray());
            }
            else
            {
                // tři a více chlívků nepodporujeme
                throw new NotImplementedException("Templates with more than 3 departure sections are not supported now.");
            }

        }

        // zpracovává jednu tabulku s odjezdy
        private void PrintDepartureTable(Excel.Worksheet xlWorksheet, StopTimetable stopTimetable, bool areAllTripsWheelchairAccessible, 
            ExcelTemplate.DepartureSectionDescriptor timetableSection, params StopTimetableDay[] dayColumns)
        {
            var originCell = CellRef.FromCellCode(timetableSection.TopLeftCell);
            var bottomRight = CellRef.FromCellCode(timetableSection.BottomRightCell);

            // V šabloně je jen jedna tabulka pro časy odjezdů. Pokud je zadáno více provozních dnů (DayColumns), než jeden,
            // musíme si ty ostatní vytvořit.
            //
            // Situace se má v tuto chvíli takto:
            // Ve výchozím stavu je 15 sloupečků na časy. Přidání každého dalšího provozního dne je jeden zužitkován na sloupeček s
            // hodinama. Zároveň při přidání každého sudého provozního dne je zužitkován jeden sloupeček na mezery. Konkrétní počty
            // sloupečků vystihuje tabulka níže:
            //
            // Počet provozních dnů     | Počet sloupečků k dispozici   | Počet zbývajících pixelů
            //          1               |           15                  |            0
            //          2               |           13                  |           12
            //          3               |           12                  |            1
            //          4               |           10                  |           13
            //          5               |            9                  |            2
            //         ...              |           ...                 |          ...
            //
            // Každý sloupeček na minuty má šířku 23 pixelů, sloupeček s mezerou mezi provozními dny má 7 px, sloupeček s mezerou
            // mezi hodinami a minutami má 4 px. To vychází právě tak, že za jeden sloupeček s minutami umíme udělat dvě mezery
            // (a ještě nám pixel zbyde). Počet pixelů, které zbydou po rekonstrukci vystihuje tabulka též.
            // 
            // V první fázi musíme zjistit, jestli se do tohoto počtu vejdeme a jestli nám zbývají nějaké volné sloupečky. Volné místo
            // pak rozdistribuujeme pokud možno rovnoměrně mezi všechny provozní dny.
            //

            // počet provozních dnů, které budeme přidávat
            var additionalDaysCount = dayColumns.Length - 1;

            // pro každý provozní den počet sloupečků, který potřebuje (podle hodiny s největším počtem odjezdů)
            var minimumNeededWidths = dayColumns.Select(dayCol => dayCol.GetNumberOfMinuteColumnsNeeded()).ToArray();

            // počet sloupečků pro minuty k dispozici (dle tabulky výše)
            var widthAvailable = (bottomRight.Column - originCell.Column - 1) - additionalDaysCount - (additionalDaysCount + 1) / 2;
            var widthFree = widthAvailable - minimumNeededWidths.Sum();
            if (widthFree < 0)
            {
                // TODO nevejdou se všechny odjezdy
            }

            // rozdistribuujeme volné místo, poté by se suma widths měla rovnat widthAvailable
            var widths = minimumNeededWidths.ToArray();
            for (int i = 0; i < widths.Length; i++)
            {
                widths[i] += widthFree / widths.Length;
                if (i < widthFree % widths.Length)
                    widths[i]++;
                else if (widthFree < 0 && i >= (widthFree + widths.Length) % widths.Length)
                    widths[i]--;
            }

            // nyní známe šířky sloupečků, provedeme zásahy do excelu (vytvoření oddělovacích sloupečků a sloupečků s hodinami)
            var currentCell = originCell.Move(0, 2); // posun na první odjezd nad první hodinou
            for (int i = 0; i < additionalDaysCount; i++)
            {
                currentCell.Column += widths[i]; // posun na políčko za posledním odjezdem (ze kterého bude mezera)
                if (i % 2 == 1) // jen u lichých přidáváme sloupeček, jinak zužitkujeme existující
                    xlWorksheet.GetCells(currentCell).EntireColumn.Insert(Excel.XlInsertShiftDirection.xlShiftToRight, Excel.XlInsertFormatOrigin.xlFormatFromLeftOrAbove);
                var daysSpacingCells = xlWorksheet.GetCells(currentCell, 25, 1);
                daysSpacingCells.ColumnWidth = 0.58;
                daysSpacingCells.Interior.ColorIndex = 0;
                daysSpacingCells.Borders[Excel.XlBordersIndex.xlEdgeTop].LineStyle = Excel.XlLineStyle.xlLineStyleNone;
                daysSpacingCells.Borders[Excel.XlBordersIndex.xlEdgeBottom].LineStyle = Excel.XlLineStyle.xlLineStyleNone;
                daysSpacingCells.Borders[Excel.XlBordersIndex.xlEdgeLeft].LineStyle = Excel.XlLineStyle.xlContinuous;
                daysSpacingCells.Borders[Excel.XlBordersIndex.xlEdgeLeft].Weight = Excel.XlBorderWeight.xlHairline;
                daysSpacingCells.Borders[Excel.XlBordersIndex.xlEdgeRight].LineStyle = Excel.XlLineStyle.xlContinuous;
                daysSpacingCells.Borders[Excel.XlBordersIndex.xlEdgeRight].Weight = Excel.XlBorderWeight.xlHairline;

                currentCell.Column++; // posun na políčko s hodinami
                var hoursCells = xlWorksheet.GetCells(currentCell.Move(1, 0), 24, 1);
                hoursCells.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                hoursCells.Font.Bold = true;
                //hoursCells.Borders[Excel.XlBordersIndex.xlEdgeRight].LineStyle = Excel.XlLineStyle.xlContinuous;
                //hoursCells.Borders[Excel.XlBordersIndex.xlEdgeRight].Weight = Excel.XlBorderWeight.xlHairline;

                currentCell.Column++; // posun na políčko za hodinami (z něho bude mezera mezi hodinami a minutami)
                xlWorksheet.GetCells(currentCell).EntireColumn.Insert(Excel.XlInsertShiftDirection.xlShiftToRight, Excel.XlInsertFormatOrigin.xlFormatFromLeftOrAbove);
                var hoursSeparatorCells = xlWorksheet.GetCells(currentCell.Move(1, 0), 24, 1);
                hoursSeparatorCells.ColumnWidth = 0.1666;

                hoursCells.Interior.Color = Color.FromArgb(217, 217, 217);
                currentCell.Column++; // posun na první odjezd nad první hodinu
            }

            if (additionalDaysCount % 2 == 1)
            {
                // pokud jsme přidali lichý počet provozních dnů, zbylo nám 11 pixelů, které můžeme někde utavit
                // TODO nefunguje
                var col3 = xlWorksheet.GetCells("C1").EntireColumn;
                col3.ColumnWidth += 0.91;
            }

            for (int i = 0; i < dayColumns.Length; i++)
            {
                PrintDepartureTimes(xlWorksheet, dayColumns[i], originCell, widths[i], stopTimetable.FirstHour, stopTimetable.LastHour,
                    !areAllTripsWheelchairAccessible && !ignoreWheelchairAccessibleAttribute);
                originCell.Column += widths[i] + 3;
            }
        }

        // zpracovává odjezdy jednoho provozního dne
        private void PrintDepartureTimes(Excel.Worksheet xlWorksheet, StopTimetableDay timetable, CellRef originCell, int width, int firstHour, int lastHour,
            bool markWheelchairAccessibleTrips)
        {
            var cellRange = xlWorksheet.GetCells(originCell, 1, width + 2);
            cellRange.Merge();
            if (timetable.TitleSymbols.Length > 0)
            {
                cellRange.Value = $"{timetable.Title} {timetable.TitleSymbols}";
                cellRange.Characters[timetable.Title.Length + 2].Font.Name = "Timetable";
            }
            else
            {
                cellRange.Value = timetable.Title;
            }

            for (int i = 0; i < lastHour - firstHour + 1; i++)
            {
                var hour = (i + firstHour) % 24;
                xlWorksheet.GetCells(originCell.Row + i + 1, originCell.Column).Value = hour;
                PrintDepartureTimesForHour(xlWorksheet, originCell.Move(i + 1, 2), timetable.Hours[hour], width, markWheelchairAccessibleTrips);
            }
        }

        // zpracovává odjezdy jednoho pracovního dne v jedné hodině
        private void PrintDepartureTimesForHour(Excel.Worksheet xlWorksheet, CellRef originCell, List<Departure> departureTimes, int width,
            bool markWheelchairAccessibleTrips)
        {
            for (int i = 0; i < departureTimes.Count; i++)
            {
                if (i >= width)
                {
                    // TODO error, nevejde se
                    return;
                }

                var cell = xlWorksheet.GetCells(originCell.Row, originCell.Column + i);
                var departure = departureTimes[i];
                var minutesStr = departure.Minute.ToString("00");
                var remarks = new string(departure.Remarks.SelectMany(r => r.Symbol).ToArray());
                cell.Value = minutesStr + remarks;

                if (markWheelchairAccessibleTrips && departure.IsWheelchairAccessible)
                {
                    cell.Characters[1, 2].Font.Underline = true;
                }

                if (remarks.Length > 0)
                {
                    cell.Characters[3, remarks.Length].Font.Size = 6;
                }
            }
        }
    }
}