using System;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TrainsEditor.CommonModel;

namespace TrainsEditor.ViewModel
{
    /// <summary>
    /// Reprezentuje obrázek (bitmapu) časové osy, kdy je daný vlak v provozu. Barvy značí možnosti dle enumu <see cref="CalendarValue"/>
    /// </summary>
    class CalendarVisualBitmap : INotifyPropertyChanged
    {
        /// <summary>
        /// Barva značící, že vlak v daný den jede (u cancel záznamů se nepoužívá)
        /// </summary>
        public static readonly Color OperatesColor = Color.FromRgb(34, 177, 76);

        /// <summary>
        /// Barva značící, že vlak v daný den nejede (u cancel záznamů že není rušen)
        /// </summary>
        public static readonly Color DoesNotOperateColor = Color.FromRgb(176, 176, 176);

        /// <summary>
        /// Barva značící, že vlak v daný den by normálně jel, ale existuje novější verze téhož vlaku, která jede a tento soubor tím přepisuje (nebo ruší)
        /// </summary>
        public static readonly Color OverwrittenColor = Color.FromRgb(255, 127, 39);

        /// <summary>
        /// Barva značící, že jde o cancel soubor a vlak je v tento den zrušen
        /// </summary>
        public static readonly Color CanceledColor = Color.FromRgb(255, 0, 0);

        /// <summary>
        /// Objekt reprezentující grafický prvek (brush podle bitmapy)
        /// </summary>
        public Brush Brush { get; private set; }

        /// <summary>
        /// Počáteční datum zobrazení časové osy
        /// </summary>
        public DateTime VisualStartDate { get; private set; }

        /// <summary>
        /// Koncové datum zobrazení časové osy
        /// </summary>
        public DateTime VisualEndDate { get; private set; }

        /// <summary>
        /// Vlak, jemuž časová osa patří
        /// </summary>
        public AbstractTrainFile OwnerTrain { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public CalendarVisualBitmap(AbstractTrainFile train, DateTime firstDay, DateTime lastDay)
        {
            OwnerTrain = train;
            ResetDateInterval(firstDay, lastDay);
        }

        /// <summary>
        /// Změní interval <see cref="VisualStartDate"/> a <see cref="VisualEndDate"/> a celý vizuál překreslí
        /// </summary>
        /// <param name="firstDay"></param>
        /// <param name="lastDay"></param>
        public void ResetDateInterval(DateTime firstDay, DateTime lastDay)
        {
            VisualStartDate = firstDay;
            VisualEndDate = lastDay;
            RefreshVisual();
        }

        /// <summary>
        /// Překreslí vizuál podle dat v kalendáři vlaku
        /// </summary>
        public void RefreshVisual()
        {
            var nDays = (VisualEndDate - VisualStartDate).Days + 1;
            var startMonth = VisualStartDate.Month + VisualStartDate.Year * 12;
            var nBitmapDays = nDays + (VisualEndDate.Month + VisualEndDate.Year * 12 - startMonth);
            var trainStartDateDelta = (VisualStartDate - OwnerTrain.FileData.StartDate).Days;
            var todayIndex = (DateTime.Now.Date - VisualStartDate).Days;
            var bmp = new WriteableBitmap(nBitmapDays, 1, 300, 300, PixelFormats.Bgr32, null);
            var pixels = new byte[4 * nBitmapDays];
            for (int i = 0; i < 4 * nBitmapDays; i++)
                pixels[i] = 255;

            for (int i = 0; i < nDays; i++)
            {
                var date = VisualStartDate.AddDays(i);
                var bitmapIndex = i + (date.Month + date.Year * 12 - startMonth);
                var trainCalendarIndex = i + trainStartDateDelta;
                Color color = DoesNotOperateColor;
                if (trainCalendarIndex >= 0 && trainCalendarIndex < OwnerTrain.FileData.BitmapEx.Length)
                {
                    if (OwnerTrain.FileData.BitmapEx[trainCalendarIndex] == CalendarValue.Active)
                    {
                        if (OwnerTrain is TrainCancelationFile)
                        {
                            color = CanceledColor;
                        }
                        else
                        {
                            color = OperatesColor;
                        }
                    }
                    else if (OwnerTrain.FileData.BitmapEx[trainCalendarIndex] == CalendarValue.Overwritten)
                    {
                        color = OverwrittenColor;
                    }
                }

                pixels[bitmapIndex * 4] = color.B;
                pixels[bitmapIndex * 4 + 1] = color.G;
                pixels[bitmapIndex * 4 + 2] = color.R;
            }

            bmp.WritePixels(new System.Windows.Int32Rect(0, 0, nBitmapDays, 1), pixels, 4 * nBitmapDays, 0);

            Brush = new ImageBrush(bmp);
            Brush.Freeze();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Brush"));
        }
    }
}
