using System.Windows;
using System.Windows.Controls;
using LEDSystem.Core.Interfaces;
using LEDSystem.UI.Helpers;

namespace LEDSystem.Effects
{
    // Stato: COMPLETATO (01.07.2020)
    public partial class GradientControl : UserControl, IEffect
    {
        #region Variables
        private IHandler uiHandler;
        private string prefName = "effect3";
        private bool isUserInputAllowed = false;
        private int lightsCount = 1;
        private Tint selectedTint;
        private int colorMode = 0;
        private double increase = 0.01;
        private double offset = 0.0;
        #endregion

        #region Main
        public GradientControl(IHandler uiHandler)
        {
            // Imposta il gestore dell'effetto fornito come parametro
            this.uiHandler = uiHandler;
            // Inizializza i componenti grafici
            InitializeComponent();
            // Carica le preferenze dell'utente.
            LoadPreferences();
        }
        public void LoadPreferences()
        {
            // Impedisce agli elementi di salvare i propri valori.
            isUserInputAllowed = false;

            // Aggiunge le luci disponibli alla lista degli output.
            lightsCount = Core.Preferences.GetPreference<int>("config", "lightsCount");
            for (int i = 1; i < CBox_ColorMode.Items.Count; i++) {
                (CBox_ColorMode.Items[i] as ComboBoxItem).IsEnabled = lightsCount == 1 ? false : true;
            }

            // Carica le preferenze relative al colore salvato.
            selectedTint = Core.Preferences.GetPreference<Tint>(prefName, "colorPicked");
            CBox_ColorPicker.Background = selectedTint.GetBrush();

            // Carica le preferenze relative alla modalità del colore.
            int tempColorMode = Core.Preferences.GetPreference<int>(prefName, "colorMode");
            if (lightsCount == 1) {
                if (tempColorMode >= 1) {
                    tempColorMode = 0;
                    Core.Preferences.SetPreference<int>(prefName, "colorMode", tempColorMode);
                }
            }
            CBox_ColorMode.SelectedIndex = tempColorMode;

            // Imposta la velocità del timer.
            NBox_Speed.Value = Core.Preferences.GetPreference<int>(prefName, "logicSpeed");

            // Permette agli elementi di salvare i propri valori.
            isUserInputAllowed = true;
        }
        #endregion

        #region Logic
        public void OnCreate()
        {
        }
        public void OnUpdate()
        {
            offset += increase;
            if (offset >= 1.0) {
                offset = 0.0;
            }

            switch (colorMode)
            {
                case 0:
                    Tint tint = selectedTint.GetPoint(offset);
                    uiHandler.OnEffectData(0, 0, 0x01, 0xFF, tint.Red, tint.Green, tint.Blue);
                    break;
                case 1:
                    for (int i = 0; i < lightsCount; i++) {
                        double lightOffset = offset + (i * increase * 10);
                        if (lightOffset > 1.0) {
                            lightOffset = (lightOffset - 1.0);
                        }
                        Tint lightTint = selectedTint.GetPoint(lightOffset);
                        uiHandler.OnEffectData(1, i, 0x01, 0xFF, lightTint.Red, lightTint.Green, lightTint.Blue);
                    }
                    break;
            }
        }
        public void OnAction(Tint tint)
        {
            if (tint == null) return;
            selectedTint = tint;

            CBox_ColorPicker.Background = selectedTint.GetBrush();
            Core.Preferences.SetPreference<Tint>(prefName, "colorPicked", selectedTint);
        }
        public void OnClosed()
        {
        }
        #endregion

        #region Events
        private void NBox_Speed_ValueChanged(object sender, RoutedEventArgs e)
        {
            //Imposta l'intervallo del timer.
            uiHandler.SetInterval(NBox_Speed.Value);

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<int>(prefName, "logicSpeed", NBox_Speed.Value);
            }
        }
        private void CBox_ColorMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            offset = 0;
            colorMode = CBox_ColorMode.SelectedIndex;

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<int>(prefName, "colorMode", colorMode);
            }
        }
        private void CBox_ColorPicker_OnPickerRequest(object sender, RoutedEventArgs e)
        {
            uiHandler.OnColorPicker(1, selectedTint);
        }
        #endregion
    }
}
