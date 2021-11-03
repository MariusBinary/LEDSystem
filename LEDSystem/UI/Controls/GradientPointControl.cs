using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LEDSystem.UI.Controls
{
    public class GradientPointControl : Control
    {
        #region Variables
        public int Index { get; set; }
        public double Offset { get; set; }
        #endregion

        #region Main
        static GradientPointControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GradientPointControl), new FrameworkPropertyMetadata(typeof(GradientPointControl)));
        }
        public GradientPointControl(Brush brush)
        {
            this.Background = brush;
        }
        #endregion
    }
}
