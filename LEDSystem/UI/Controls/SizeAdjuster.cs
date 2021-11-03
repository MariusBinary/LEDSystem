using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace LEDSystem.UI.Controls
{
    public class SizeAdjuster : Decorator
    {
        public SizeAdjuster()
        {
            this.Loaded += (s, e) =>
            {
                int scaleIndex = Core.Preferences.GetPreference<int>("settings", "scaling");
                if (scaleIndex == -1) {
                    scaleIndex = Core.Utils.GetDefaultScale();
                }

                double scaleRatio = Core.Utils.Map(Math.Round((double)scaleIndex, 2), 0, 4, 0.8, 1.2);
                ApplyDPI(new ScaleTransform(scaleRatio, scaleRatio));
            };
        }

        public void ApplyDPI(ScaleTransform scaleTransform)
        {
            this.LayoutTransform = scaleTransform;
        }
    }
}
