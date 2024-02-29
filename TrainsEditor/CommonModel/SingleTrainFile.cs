using System;
using System.Collections.Generic;
using CzpttModel;

/// <summary>
/// CommonModel je základní rozšíření nad <see cref="CzpttModel"/>, které používá jak editor XML, tak převodník do GTFS.
/// Jednak poskytuje abstrakci nad jinak neslučitelnými třídami <see cref="CZPTTCISMessage"/> a <see cref="CZCanceledPTTMessage"/>.
/// Zároveň umí sdružit vlaky do skupin <see cref="TrainsEditor.CommonModel.TrainGroupCollection"/> podle TRAIN ID a dopočítat kalendáře podle toho, jak se vlaky ve skupině přepisují.
/// </summary>
namespace TrainsEditor.CommonModel
{
    /// <summary>
    /// Reprezentuje jeden soubor s vlakem ve skupině <see cref="TrainGroup"/>.
    /// 
    /// Může obsahovat buď data vlaku (trasu, časy apod.) nebo informaci o zrušení vlaku v některé časy. Vždy je vyplněna právě jedna
    /// z dvojice hodnot <see cref="TrainData"/> nebo <see cref="CancelTrainData"/>.
    /// 
    /// Jde o základní rozšíření nad <see cref="CZPTTCISMessage"/> a <see cref="CZCanceledPTTMessage"/>, které používá jak generátor GTFS, tak editor vlaků,
    /// které k vlakům přistupují skrze <see cref="TrainGroupCollection"/>, která nad klasickým CZPTT modelem poskytuje sdružení vlaků do skupin podle TRAIN ID
    /// a dopočítává kalendáře podle toho, jak se vlaky ve skupině přepisují.
    /// </summary>
    class SingleTrainFile
    {
        /// <summary>
        /// True, pokud soubor reprezentuje zrušení vlaku. V takovém případě jsou data o zrušení v položce <see cref="CancelTrainData"/>.
        /// False, pokud soubor reprezentuje data vlaku. V takovém případě jsou data o vlaku v položce <see cref="TrainData"/>.
        /// </summary>
        public bool IsCancelation => CancelTrainData != null;

        /// <summary>
        /// Pokud <see cref="IsCancelation"/> = false, pak obsahuje data vlaku. Jinak null.
        /// </summary>
        public CZPTTCISMessage TrainData { get; private set; }

        /// <summary>
        /// Pokud <see cref="IsCancelation"/> = true, pak obsahuje data o zrušení vlaku. Jinak null.
        /// </summary>
        public CZCanceledPTTMessage CancelTrainData { get; private set; }

        /// <summary>
        /// Absolutní cesta k souboru, ze kterého byla instance vytvořena
        /// </summary>
        public string FileFullPath { get; private set; }

        /// <summary>
        /// Čas poslední změny souboru, ze kterého byla data načtena. Hodí se pro cachování.
        /// </summary>
        public DateTime FileLastModifiedTime { get; set; }

        /// <summary>
        /// Skupina, do které soubor patří. Nastavuje se automaticky v <see cref="TrainGroup.AddTrain"/> a <see cref="TrainGroup.RemoveTrain"/>.
        /// </summary>
        public TrainGroup OwnerGroup { get; set; }

        /// <summary>
        /// ID vlaku tvořené jako TR ID Company + Core (tzn. ID dopravce + jeho ID vlaku). Tato hodnota určuje příslušnost ke skupině
        /// (<see cref="TrainGroup"/> obsahuje vždy vlaky se shodnou touto hodnotou).
        /// </summary>
        public string TrIdCompanyAndCoreAndYear
        {
            get
            {
                if (IsCancelation)
                {
                    return CancelTrainData.GetTrainIdentifier().CompanyAndCoreAndYear;
                }
                else
                {
                    return TrainData.GetTrainIdentifier().CompanyAndCoreAndYear;
                }
            }
        }

        /// <summary>
        /// Datum vzniku souboru. Důležité při sestavování výsledných dat, pozdější data přemazávají dřívější.
        /// </summary>
        public DateTime CreationDateTime
        {
            get
            {
                if (IsCancelation)
                {
                    return CancelTrainData.CZPTTCancelation;
                }
                else
                {
                    return TrainData.CZPTTCreation;
                }
            }
        }

        /// <summary>
        /// Data o platnosti záznamu (kdy vlak jede, pokud jde o data vlaku / kdy je vlak zrušen, pokud jde o zrušení vlaku)
        /// </summary>
        public PlannedCalendar CalendarData
        {
            get
            {
                if (IsCancelation)
                {
                    return CancelTrainData.PlannedCalendar;
                }
                else
                {
                    return TrainData.CZPTTInformation.PlannedCalendar;
                }
            }
        }

        /// <summary>
        /// Počáteční datum <see cref="CalendarData"/>
        /// </summary>
        public DateTime StartDate => CalendarData.ValidityPeriod.StartDateTime;

        /// <summary>
        /// Koncové datum <see cref="CalendarData"/>
        /// </summary>
        public DateTime EndDate => CalendarData.ValidityPeriod.StartDateTime.AddDays(CalendarData.BitmapDays.Length - 1);

        /// <summary>
        /// Vypočítaná bitmapa. Vychází z toho, kdy má vlak aktivní dny (hodnota 1 v bitmapě) v <see cref="CalendarData"/> (v takových dnech je hodnota <see cref="CalendarValue.Active"/>).
        /// Ve dny, kdy je vlak neaktivní (má v bitmapě 0), je zde hodnota <see cref="CalendarValue.Inactive"/>.
        /// Navíc jsou zde dny, kdy vlak byl aktivní, ovšem byl přepsán (nebo zrušen) jinou variantu, vložena hodnota <see cref="CalendarValue.Overwritten"/>.
        /// </summary>
        /// <remarks>
        /// U záznamů o zrušení vlaku (<see cref="IsCancelation"/> = true) znamená hodnota <see cref="CalendarValue.Active"/>, že vlak je v tento den zrušen a hodnota
        /// <see cref="CalendarValue.Inactive"/>, že vlak v tento den zrušen není.
        /// </remarks>
        public CalendarValue[] BitmapEx { get; private set; }

        /// <summary>
        /// Seznam záznamů ze skupiny <see cref="TrainGroup"/>, které tento záznam přepisuje (alespoň 1 den).
        /// </summary>
        public List<SingleTrainFile> OverwrittenTrains { get; private set; }

        public SingleTrainFile(CZPTTCISMessage trainData, string fileFullPath)
        {
            TrainData = trainData;
            FileFullPath = fileFullPath;
        }

        public SingleTrainFile(CZCanceledPTTMessage cancelTrainData, string fileFullPath)
        {
            CancelTrainData = cancelTrainData;
            FileFullPath = fileFullPath;
        }

        /// <summary>
        /// Nastaví <see cref="BitmapEx"/> podle bitmapy z <see cref="CalendarData"/> (<see cref="CalendarValue.Active"/> místo 1 a <see cref="CalendarValue.Inactive"/> místo 0).
        /// </summary>
        public void InitBitmap()
        {
            OverwrittenTrains = new List<SingleTrainFile>();
            BitmapEx = new CalendarValue[CalendarData.BitmapDays.Length];
            for (int i = 0; i < BitmapEx.Length; i++)
            {
                BitmapEx[i] = CalendarData.BitmapDays[i] == '1' ? CalendarValue.Active : CalendarValue.Inactive;
            }
        }
    }
}
