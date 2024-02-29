using CzpttModel;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using TrainsEditor.CommonModel;

namespace TrainsEditor.ViewModel
{
    /// <summary>
    /// ViewModel kalendáře vlaku (po dnech kdy vlak jede)
    /// </summary>
    class TrainCalendar : INotifyPropertyChanged
    {
        /// <summary>
        /// Záznam jednoho data (zda vlak v dané datum jede nebo nejede)
        /// </summary>
        public class DateRecord : INotifyPropertyChanged
        {
            public DateTime Date { get; set; }

            private bool _value;
            public bool Value
            {
                get { return _value; }
                set
                {
                    _value = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        /// <summary>
        /// Data kalendáře z XML souboru
        /// </summary>
        public PlannedCalendar CalendarData => OwnerTrain.FileData.CalendarData;

        /// <summary>
        /// Počáteční datum kalendáře
        /// </summary>
        public DateTime StartDate
        {
            get
            {
                return CalendarData.ValidityPeriod.StartDateTime;
            }
            set
            {
                CalendarData.ValidityPeriod.StartDateTime = value;
            }
        }

        /// <summary>
        /// Koncové datum kalendáře
        /// </summary>
        public DateTime EndDate
        {
            get
            {
                return CalendarData.ValidityPeriod.StartDateTime.AddDays(CalendarData.BitmapDays.Length - 1);
            }
            set
            {
                var dayCount = (value - CalendarData.ValidityPeriod.StartDateTime).Days + 1;
                if (dayCount <= CalendarData.BitmapDays.Length)
                {
                    CalendarData.BitmapDays = CalendarData.BitmapDays.Substring(0, dayCount);
                }
                else
                {
                    CalendarData.BitmapDays += new string('0', dayCount - CalendarData.BitmapDays.Length);
                }
            }
        }

        /// <summary>
        /// Záznamy všech datumů mezi <see cref="StartDate"/> a <see cref="EndDate"/> říkající, zda vlak v daný den jede nebo nejede.
        /// </summary>
        public ObservableCollection<DateRecord> DateRecords { get; private set; }

        /// <summary>
        /// Vlak, jemuž tento kalendář patří
        /// </summary>
        protected AbstractTrainFile OwnerTrain { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public TrainCalendar(AbstractTrainFile ownerTrain)
        {
            OwnerTrain = ownerTrain;
            DateRecords = new ObservableCollection<DateRecord>();
            RefreshDateRecords();
        }

        /// <summary>
        /// Obnoví hodnoty v seznamu <see cref="DateRecords"/> podle dat z kalendáře v XML souboru
        /// </summary>
        public void RefreshDateRecords()
        {
            DateRecords.Clear();
            for (int i = 0; i < CalendarData.BitmapDays.Length; i++)
            {
                var date = CalendarData.ValidityPeriod.StartDateTime.AddDays(i);
                var dateRecord = new DateRecord()
                {
                    Date = date,
                    Value = CalendarData.BitmapDays[i] != '0',
                };

                dateRecord.PropertyChanged += DateRecord_PropertyChanged;
                DateRecords.Add(dateRecord);
            }
        }

        private void DateRecord_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnDateRecordChanged();
        }

        public void OnDateRecordChanged()
        {
            CommitDateRecords();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

        /// <summary>
        /// Změní rozsah <see cref="StartDate"/> - <see cref="EndDate"/>. Pokud se rozsah zkracuje, ve dny, které jsou vypuštěny, vlak již nepojede.
        /// Pokud se rozsah rozšiřuje, jsou nově přidané dny výchozí s hodnotou, že vlak nejede.
        /// </summary>
        /// <param name="newStartDate"></param>
        /// <param name="newEndDate"></param>
        /// <param name="notify"></param>
        public void ResizeBitmap(DateTime newStartDate, DateTime newEndDate, bool notify)
        {
            if (newStartDate == StartDate && newEndDate == EndDate)
                return;

            var delta = (newStartDate - StartDate).Days;
            var newSize = (newEndDate - newStartDate).Days + 1;
            var newBitmap = new char[newSize];
            for (int i = 0; i < newSize; i++)
            {
                var origIndex = i + delta;
                if (origIndex >= 0 && origIndex < CalendarData.BitmapDays.Length)
                {
                    newBitmap[i] = CalendarData.BitmapDays[origIndex];
                }
                else
                {
                    newBitmap[i] = '0';
                }
            }

            CalendarData.ValidityPeriod.StartDateTime = newStartDate;
            CalendarData.ValidityPeriod.EndDateTime = newEndDate;
            CalendarData.BitmapDays = new string(newBitmap);
            RefreshDateRecords();

            if (notify)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
            }
        }

        protected void CommitDateRecords()
        {
            var newBitmap = new char[DateRecords.Count];
            for (int i = 0; i < DateRecords.Count; i++)
            {
                newBitmap[i] = DateRecords[i].Value ? '1' : '0';
            }

            CalendarData.BitmapDays = new string(newBitmap);
        }
    }
}
