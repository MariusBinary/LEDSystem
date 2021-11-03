using System;
using System.ComponentModel;
using System.Windows.Media;

namespace LEDSystem.UI.Controls.ColorPicker
{
    public class GradientStopModel : INotifyPropertyChanged, IComparable<GradientStopModel>, IEquatable<GradientStopModel>
    {
        public bool IsUserAction { get; set; } = false;
        public delegate void CallbackEventHandler(int index, double offset);
        public event CallbackEventHandler Callback;

        public int CompareTo(GradientStopModel other)
        {
            if (this.Offset == other.Offset) return 0;
            return this.Offset.CompareTo(other.Offset);
        }

        public bool Equals(GradientStopModel other)
        {
            if (this.Offset.Equals(other.Offset)) return true;
            return false;
        }

        private int _index;
        public int Index
        {
            get { return _index; }
            set
            {
                _index = value;
                RaisePropertyChanged("Index");
            }
        }

        private double _offset;
        public double Offset
        {
            get { return _offset; }
            set
            {
              
                _offset = value;

                if (IsUserAction) {
                    Callback(_index, _offset);
                    IsUserAction = true;
                } else {
                    IsUserAction = true;
                }
                RaisePropertyChanged("Offset");
            }
        }

        private Brush _backgroundBrush;
        public Brush BackgroundBrush
        {
            get { return _backgroundBrush; }
            set
            {
                _backgroundBrush = value;
                RaisePropertyChanged("BackgroundBrush");
            }
        }

        private Brush _borderBrush;
        public Brush BorderBrush
        {
            get { return _borderBrush; }
            set
            {
                _borderBrush = value;
                RaisePropertyChanged("BorderBrush");
            }
        }

        private Brush _colorBackgroundBrush;
        public Brush ColorBackgroundBrush
        {
            get { return _colorBackgroundBrush; }
            set
            {
                _colorBackgroundBrush = value;
                RaisePropertyChanged("ColorBackgroundBrush");
            }
        }

        private Brush _colorBorderBrush;
        public Brush ColorBorderBrush
        {
            get { return _colorBorderBrush; }
            set
            {
                _colorBorderBrush = value;
                RaisePropertyChanged("ColorBorderBrush");
            }
        }

        private bool _isActived = false;
        public bool IsActived
        {
            get { return _isActived; }
            set
            {
                _isActived = value;
                BackgroundBrush = Core.Utils.GetBrushFromHex(_isActived ? "#FF141415" : "#FF17181A");
                BorderBrush = Core.Utils.GetBrushFromHex(_isActived ? "#FF414A87" : "#FF2B2C33");
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
