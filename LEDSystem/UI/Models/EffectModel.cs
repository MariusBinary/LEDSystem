using System.ComponentModel;

namespace LEDSystem.Core.Models
{
    public class EffectModel : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Details { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public string Author { get; set; }
        public string Path { get; set; }
        public string SponsorToolTip { get; set; }
        public string SponsorImage { get; set; }
        private bool _isActived = false;
        public bool IsActived
        {
            get { return _isActived; }
            set
            {
                _isActived = value;
                RaisePropertyChanged("IsActived");
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }
}
