using CzpttModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace TrainsEditor.ViewModel
{
    /// <summary>
    /// ViewModel aktivit vlaku ve stanici/dopravním bodě postavený nad <see cref="CZPTTLocation.TrainActivity"/>.
    /// Slouží k vyobrazení a možnosti editace aktivit ve stanici (sdružuje info o všech aktivitách v konkrétním zastavení vlaku).
    /// </summary>
    public class TrainActivityViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Seznam aktivit vlaku ve stanici
        /// </summary>
        public List<TrainActivity> TrainActivity { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Vrací true, pokud vlak má ve stanici danou aktivitu
        /// </summary>
        /// <param name="trainActivity">Aktivita (z konstant v <see cref="CzpttModel.TrainActivity"/>)</param>
        public bool HasActivity(string trainActivity)
        {
            return TrainActivity.Any(a => a.TrainActivityType == trainActivity);
        }

        /// <summary>
        /// Nastaví hodnotu aktivity.
        /// </summary>
        /// <param name="trainActivity">Aktivita (z konstant v <see cref="CzpttModel.TrainActivity"/>)</param>
        /// <param name="value">Pokud je true, vlak bude mít nastavenu danou aktivitu (pokud ji již neměl). Pokud je false, vlak bude mít smazánu danou aktivitu (pokud ji měl)</param>
        public void SetActivity(string trainActivity, bool value)
        {
            var activity = TrainActivity.FirstOrDefault(a => a.TrainActivityType == trainActivity);
            if (activity != null && value == false)
            {
                TrainActivity.Remove(activity);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TrainActivity"));
            }
            else if (activity == null && value == true)
            {
                TrainActivity.Add(new TrainActivity() { TrainActivityType = trainActivity });
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TrainActivity"));
            }
        }

        /// <summary>
        /// Vrací true, pokud vlak má ve stanici danou aktivitu
        /// </summary>
        /// <param name="trainActivity">Aktivita (z konstant v <see cref="CzpttModel.TrainActivity"/>)</param>
        public bool this [string trainActivity]
        {
            get { return HasActivity(trainActivity); }
            set { SetActivity(trainActivity, value); }
        }

        public TrainActivityViewModel(List<TrainActivity> trainActivity)
        {
            TrainActivity = trainActivity;
        }
    }
}
