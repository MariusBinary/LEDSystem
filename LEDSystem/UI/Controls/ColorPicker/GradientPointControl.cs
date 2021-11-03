using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LEDSystem.UI.Controls.ColorPicker
{
    public class GradientPointControl : Control
    {
        #region Variables

        public double Offset { get; set; }
        public Path _arrowDown { get; set; }
        public Path _arrowUp { get; set; }
        public TextBlock _text { get; set; }
        private bool _isActived;

        private int _index;
        public int Index
        {
            get { return _index; }
            set
            {
                _index = value;
                if (_text != null)
                {
                    this._text.Text = Index.ToString();
                }
            }
        }

        public bool IsActived
        {
            get { return _isActived; }
            set
            {
                _isActived = value;
                if (_arrowUp != null && _arrowDown != null)
                {
                    if (_isActived)
                    {
                        _arrowUp.Visibility = Visibility.Visible;
                        _arrowDown.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        _arrowUp.Visibility = Visibility.Collapsed;
                        _arrowDown.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        #endregion

        #region Main
        static GradientPointControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GradientPointControl), new FrameworkPropertyMetadata(typeof(GradientPointControl)));
        }

        public override void OnApplyTemplate()
        {
            this._arrowUp = this.GetTemplateChild("PART_Up") as Path;
            this._arrowDown = this.GetTemplateChild("PART_Down") as Path;
            this._text = this.GetTemplateChild("PART_Index") as TextBlock;

            if (_isActived)
            {
                _arrowUp.Visibility = Visibility.Visible;
                _arrowDown.Visibility = Visibility.Visible;
            }
            else
            {
                _arrowUp.Visibility = Visibility.Collapsed;
                _arrowDown.Visibility = Visibility.Collapsed;
            }
            this._text.Text = Index.ToString();
            base.OnApplyTemplate();
        }

        public GradientPointControl(Brush brush)
        {
            this.Background = brush;
        }
        #endregion
    }
}
