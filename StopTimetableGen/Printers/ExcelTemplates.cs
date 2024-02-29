using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StopTimetableGen.Printers
{
    /// <summary>
    /// Jedna šablona pro JŘ v excelu
    /// </summary>
    abstract class ExcelTemplate
    {
        public const string OperatorNameCell = "A4";

        /// <summary>
        /// Políčko pro číslo linky
        /// </summary>
        public const string LineNumberCell = "C2";

        /// <summary>
        /// Políčko s cílovou zastávkou
        /// </summary>
        public const string DirectionCell = "D3";

        /// <summary>
        /// Políčko s příznakem zastávky na znamení (delta oproti sloupečku s počtem minut)
        /// </summary>
        public const int RequestStopAttrColumnDelta = 1;

        /// <summary>
        /// Políčko pro název zastávky (delta oproti sloupečku s počtem minut)
        /// </summary>
        public const int StopNameColumnDelta = 2;

        /// <summary>
        /// Políčko pro počet pásem (delta oproti sloupečku s počtem minut)
        /// </summary>
        public const int StopZoneColumnDelta = 4;

        /// <summary>
        /// Šířka sekce se zastávkami (počet sloupečků)
        /// </summary>
        public const int StopsSectionWidth = 5;

        /// <summary>
        /// Název šablony pro zobrazení uživateli
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Soubor obsahující základní strukturu
        /// </summary>
        public string TemplateFileName { get; protected set; }

        /// <summary>
        /// Nejpravější sloupeček v šabloně
        /// </summary>
        public string RightColumn { get; protected set; }

        /// <summary>
        /// Políčko s datem počátku platnosti JŘ
        /// </summary>
        public string ValidityStartDateCell
        {
            get
            {
                return RightColumn + "3";
            }
        }

        /// <summary>
        /// Políčka pro zastávky
        /// </summary>
        public struct StopSectionDescriptor
        {
            /// <summary>
            /// Levý horní roh, automaticky se počítá, že jde o sloupeček s kódy, +1 doprava je popis poznámky
            /// </summary>
            public string OriginCell { get; set; }

            /// <summary>
            /// Číslo řádku, pod které už se nesmí
            /// </summary>
            public int BottomRow { get; set; }
        }

        /// <summary>
        /// Políčka pro zastávky kde začíná seznam zastávek. Každému řádku (zastávce) odpovídá jedno políčko.
        /// Automaticky se počítá, že políčko obsahuje minuty, +1 doprava je x pro zastávku naznamení,
        /// +2 doprava je název a +4 doprava je pásmo (konstanty jsou v ExcelPrinter.cs)
        /// </summary>
        public abstract StopSectionDescriptor[] GetStopCells(int stopCount);

        /// <summary>
        /// Políčka pro poznámky
        /// </summary>
        public struct RemarkSectionDescriptor
        {
            /// <summary>
            /// Levý horní roh, automaticky se počítá, že jde o sloupeček s kódy, +1 doprava je popis poznámky
            /// </summary>
            public CellRef OriginCell { get; set; }

            /// <summary>
            /// Pravý dolní roh
            /// </summary>
            public string BottomRightCell { get; set; }
        }

        /// <summary>
        /// Políčka pro poznámky.
        /// </summary>
        public abstract RemarkSectionDescriptor GetRemarkCells(int stopCount);

        /// <summary>
        /// Políčka pro odjezdy
        /// </summary>
        public struct DepartureSectionDescriptor
        {
            /// <summary>
            /// Levý horní roh (obsahuje nadpis, pod ním jsou hodiny)
            /// </summary>
            public string TopLeftCell { get; set; }

            /// <summary>
            /// Pravý dolní roh (mělo by vždy odpovídat 25 řádkům, počet sloupečků udává, kolik se do sekce vejde odjezdů)
            /// </summary>
            public string BottomRightCell { get; set; }
        }

        /// <summary>
        /// Políčka pro odjezdy (vždy levý horní roh a šířka).
        /// Volající si může sekce ještě rozdělovat, pokud jich potřebuje více
        /// </summary>
        /// <returns></returns>
        public abstract DepartureSectionDescriptor[] GetDepartureSections();


        public ExcelTemplate(string name, string templateFolder, string templateFileName)
        {
            Name = name;
            TemplateFileName = Path.Combine(templateFolder, templateFileName);
        }
    }

    class A5ExcelTemplate : ExcelTemplate
    {
        public A5ExcelTemplate(string templateFolder)
            : base("A5", templateFolder, "ZJR_A5_vzor.xlsm")
        {
            this.RightColumn = "W";
        }

        public override DepartureSectionDescriptor[] GetDepartureSections()
        {
            return new[] { new DepartureSectionDescriptor() { TopLeftCell = "G6", BottomRightCell = this.RightColumn + "30" } };
        }

        public override RemarkSectionDescriptor GetRemarkCells(int stopCount)
        {
            return new RemarkSectionDescriptor()
            {
                OriginCell = CellRef.FromCellCode("B7").Move(stopCount + 1, 0),
                BottomRightCell = "E30",
            };
        }

        public override StopSectionDescriptor[] GetStopCells(int stopCount)
        {
            return new[] 
            {
                new StopSectionDescriptor()
                {
                    OriginCell = "A7",
                    BottomRow = 30,
                }
            };
        }
    }

    class A5_2ColExcelTemplate : ExcelTemplate
    {
        public A5_2ColExcelTemplate(string templateFolder)
            : base("A5 - 2 sloupce", templateFolder, "ZJR_A5_2sloupce_vzor.xlsm")
        {
            this.RightColumn = "X";
        }

        public override DepartureSectionDescriptor[] GetDepartureSections()
        {
            return new[] { new DepartureSectionDescriptor() { TopLeftCell = "N6", BottomRightCell = this.RightColumn + "30" } };
        }

        public override RemarkSectionDescriptor GetRemarkCells(int stopCount)
        {
            return new RemarkSectionDescriptor()
            {
                OriginCell = CellRef.FromCellCode("B7").Move((stopCount + 1) / 2 + 1, 0),
                BottomRightCell = "L30",
            };
        }

        public override StopSectionDescriptor[] GetStopCells(int stopCount)
        {
            return new[] {
                new StopSectionDescriptor()
                {
                    OriginCell = "A7",
                    BottomRow = 30,
                },
                new StopSectionDescriptor()
                {
                    OriginCell = "H7",
                    BottomRow = 30,
                }
            };
        }
    }

    class A5_2Col11TimeslotsExcelTemplate : ExcelTemplate
    {
        public A5_2Col11TimeslotsExcelTemplate(string templateFolder)
            : base("A5 - 2 sloupce, 11 časů", templateFolder, "ZJR_A5_2sloupce11_vzor.xlsm")
        {
            this.RightColumn = "Z";
        }

        public override DepartureSectionDescriptor[] GetDepartureSections()
        {
            return new[] { new DepartureSectionDescriptor() { TopLeftCell = "N6", BottomRightCell = this.RightColumn + "30" } };
        }

        public override RemarkSectionDescriptor GetRemarkCells(int stopCount)
        {
            return new RemarkSectionDescriptor()
            {
                OriginCell = CellRef.FromCellCode("B7").Move((stopCount + 1) / 2 + 1, 0),
                BottomRightCell = "L30",
            };
        }

        public override StopSectionDescriptor[] GetStopCells(int stopCount)
        {
            return new[] {
                new StopSectionDescriptor()
                {
                    OriginCell = "A7",
                    BottomRow = 30,
                },
                new StopSectionDescriptor()
                {
                    OriginCell = "H7",
                    BottomRow = 30,
                }
            };
        }
    }

    //class A5HighExcelTemplate : ExcelTemplate
    //{
    //    public A5HighExcelTemplate()
    //        : base("A5-na výšku")
    //    {
    //        this.TemplateFileName = @"C:\Users\Jiracek327\Dropbox\Ropid\opendata\GTFS\ZJR_A5_vyska_vzor.xlsm";
    //        this.RightColumn = "P";
    //    }

    //    public override DepartureSectionDescriptor[] GetDepartureSections()
    //    {
    //        return new[] { new DepartureSectionDescriptor() { TopLeftCell = "G5", BottomRightCell = this.RightColumn + "29" } };
    //    }

    //    public override RemarkSectionDescriptor GetRemarkCells(int stopCount)
    //    {
    //        return new RemarkSectionDescriptor()
    //        {
    //            OriginCell = CellRef.FromCellCode("B6").Move(stopCount + 1, 0),
    //            BottomRow = 41,
    //            WidthInChars = 35,
    //        };
    //    }

    //    public override StopSectionDescriptor GetStopCells(int stopCount)
    //    {
    //        return new StopSectionDescriptor()
    //        {
    //            OriginCell = "A6",
    //            BottomRow = 41,
    //        };
    //    }
    //}

    class A4ExcelTemplate : ExcelTemplate
    {
        public A4ExcelTemplate(string templateFolder)
            : base("A4", templateFolder, "ZJR_A4_vzor.xlsm")
        {
            this.RightColumn = "Z";
        }

        public override DepartureSectionDescriptor[] GetDepartureSections()
        {
            return new[] { new DepartureSectionDescriptor() { TopLeftCell = "G5", BottomRightCell = this.RightColumn + "29" },
                           new DepartureSectionDescriptor() { TopLeftCell = "G31", BottomRightCell = this.RightColumn + "55" }};
        }

        public override RemarkSectionDescriptor GetRemarkCells(int stopCount)
        {
            return new RemarkSectionDescriptor()
            {
                OriginCell = CellRef.FromCellCode("B6").Move(stopCount + 1, 0),
                BottomRightCell = "E58",
            };
        }

        public override StopSectionDescriptor[] GetStopCells(int stopCount)
        {
            return new StopSectionDescriptor[]
                {
                    new StopSectionDescriptor()
                        {
                            OriginCell = "A6",
                            BottomRow = 58,
                        }
                };
        }
    }
}
