using CzpttModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using TrainsEditor.CommonLogic;
using TrainsEditor.CommonModel;
using TrainsEditor.ExportModel;

namespace TrainsEditor.ViewModel
{
    /// <summary>
    /// Jeden soubor - odpovídá jednomu XML z disku, může být buď soubor s vlakem, anebo rušící soubor.
    /// Využívá datový model <see cref="TrainsEditor.CommonModel"/>, tedy nadstavbu nad standardním CZPTT.
    /// </summary>
    ///
    // Co přesně obsahují jednotlivé datové atributy se nejlépe zjistí z konstruktorů tříd TrainFile a TrainCancelationFile.
    abstract class AbstractTrainFile
    {
        /// <summary>
        /// Data vlaku (info o souboru)
        /// </summary>
        public SingleTrainFile FileData { get; private set; }

        /// <summary>
        /// Cesta k souboru
        /// </summary>
        public string FullPath => FileData.FileFullPath;

        /// <summary>
        /// Vlaky (soubory), které tento soubor přepisuje (ve dnech, kdy tento vlak jede)
        /// </summary>
        public List<AbstractTrainFile> OriginalTrains { get; set; }

        /// <summary>
        /// Vlaky (soubory), které tento soubor přepisují (ve dnech, kdy tento vlak jede)
        /// </summary>
        public List<AbstractTrainFile> OverwrittingTrains { get; set; }

        /// <summary>
        /// Všechny vlaky ve skupině (vlastně různé záznamy stejného vlaku, které mají různé kalendáře)
        /// </summary>
        public IEnumerable<AbstractTrainFile> AllTrainsInGroup => OriginalTrains.Union(new[] { this }).Union(OverwrittingTrains);

        /// <summary>
        /// Kalendář, kdy vlak jede. V případě rušícího souboru popisuje dny, kdy je vlak zrušen.
        /// </summary>
        public TrainCalendar Calendar { get; private set; }

        /// <summary>
        /// Barvy na škále popisující, jak moc vlak v daný den jede. Vždy 7 položek, každá pro daný den v týdnu, na indexu 0 je pondělí, na indexu 6 je neděle.
        /// Dopočítává se z kalendáře. U rušících souborů není relevantní a obsahuje vždy ve všech dnech v týdnu bílou.
        /// </summary>
        public Brush[] DaysOfWeekColors { get; protected set; }

        /// <summary>
        /// True, pokud byl soubor v editoru upraven, ale není uložen (tedy instance obsahuje nějaké změny oproti souboru na disku)
        /// </summary>
        public bool HasUnsavedChanges { get; set; }

        /// <summary>
        /// Seznam stanic, kde vlak staví i kde projíždí. U rušících souborů je prázdný.
        /// </summary>
        public ObservableCollection<TrainLocation> Locations { get; protected set; }

        /// <summary>
        /// Bitmapa popisující kalendář souboru.
        /// </summary>
        public CalendarVisualBitmap VisualBitmap { get; set; }

        /// <summary>
        /// TR ID vlaku (podle něho se určuje přiřazení do skupiny)
        /// </summary>
        public abstract PlannedTransportIdentifiers TrainId { get; }

        /// <summary>
        /// Datum a čas vytvoření souboru
        /// </summary>
        public abstract DateTime CreationDate { get; set; }

        /// <summary>
        /// Linka, na níž vlak jede, v případě že vlak mění linku po trase, může řetězec obsahovat více linek oddělených lomítkem
        /// </summary>
        public string LineName { get; protected set; }

        /// <summary>
        /// Trasa vlaku
        /// </summary>
        public string Route { get; protected set; }

        /// <summary>
        /// Typ a číslo vlaku. Pokud vlak mění číslo za jízdy, může obsahovat více hodnot oddělených lomítkem
        /// </summary>
        public string TrainTypeAndNumber { get; protected set; }

        /// <summary>
        /// Všechna čísla, která vlak po své trase má
        /// </summary>
        public int[] AllTrainNumbers { get; protected set; }

        /// <summary>
        /// Všechny linky, které vlak po trase vystřídá
        /// </summary>
        public string[] AllLineNames { get; protected set; }

        /// <summary>
        /// Bitové pole indikující, které integrované systémy tento vlak zahrnují
        /// </summary>
        public IntegratedSystemsEnum IntegratedSystems { get; protected set; }

        /// <summary>
        /// True, pokud je vlak integrován v PID
        /// </summary>
        public bool IsPid => IntegratedSystems.Contains(IntegratedSystemsEnum.PID);

        /// <summary>
        /// True, pokud je vlak integrován v ODIS
        /// </summary>
        public bool IsOdis => IntegratedSystems.Contains(IntegratedSystemsEnum.ODIS);

        protected AbstractTrainFile(SingleTrainFile fileData)
        {
            FileData = fileData;
            Calendar = new TrainCalendar(this);
        }

        /// <summary>
        /// Znovu nastaví data ze souboru (přemaže údaje v této instanci údaji ze zadaného souboru)
        /// </summary>
        /// <param name="fileData">Data souboru vlaku</param>
        public virtual void ResetData(SingleTrainFile fileData, StationDatabase stationDb, RouteDatabase routeDb)
        {
            FileData = fileData;
        }

        /// <summary>
        /// Nastaví příznak <see cref="HasUnsavedChanges"/> na false.
        /// </summary>
        public virtual void ClearUnsavedChangesFlag()
        {
            HasUnsavedChanges = false;
        }

        public abstract void RefreshDaysOfWeekVisual();

        /// <summary>
        /// Obnoví bitmapy platnosti všem vlakům ve skupině (typicky je to nutné udělat najednou, protože kalendáře přepisujících se vlaků se navzájem ovlivňují).
        /// </summary>
        public void RefreshVisualBitmapsForWholeGroup()
        {
            FileData.OwnerGroup.ProcessCalendars();

            foreach (var otherTrain in AllTrainsInGroup)
            {
                if (otherTrain != this)
                {
                    otherTrain.Calendar.RefreshDateRecords();
                }

                otherTrain.VisualBitmap.RefreshVisual();
            }
        }
    }
}