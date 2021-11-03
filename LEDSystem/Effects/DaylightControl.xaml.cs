using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using LEDSystem.Core.Interfaces;
using LEDSystem.UI.Helpers;

namespace LEDSystem.Effects
{
    // Stato:COMPLETATO (05.07.2020)
    public partial class DaylightControl : UserControl, IEffect
    {
        #region Variable
        private IHandler uiHandler;
        private bool isUserInputAllowed = false;
        private string prefName = "effect7";
        private int selectedPreset = -1;
        private int colorTemperature = 0;
        private int customTemperature = 1500;
        private bool isPresetChanging = true;
        private bool isCustomValue = false;
        private Tint selectedTint = new Tint {
            Type = 1,
            Points = new List<Tint> {
                new Tint(255, 109, 0, 0),
                new Tint(255, 180, 107, 0.25),
                new Tint(255, 219, 186, 0.5),
                new Tint(255, 243, 239, 0.75),
                new Tint(235, 238, 255, 1),
            }
        };
        #endregion

        #region Main
        public DaylightControl(IHandler uiHandler)
        {
            // Imposta il gestore fornito al controllo.
            this.uiHandler = uiHandler;
            // Inizializza i componenti grafici.
            InitializeComponent();
            // Carica le preferenze dell'utente.
            LoadPreferences();
        }
        public void LoadPreferences()
        {
            // Impedisce agli elementi di salvare i propri valori.
            isUserInputAllowed = false;

            // Carica le preferenze relative al colore della temperatura.
            customTemperature = Core.Preferences.GetPreference<int>(prefName, "customTemperature");

            // Carica le preferenze relative al preset selezionato.
            CBox_Presets.SelectedIndex = Core.Preferences.GetPreference<int>(prefName, "selectedPreset");

            //Imposta la velocità del timer.
            uiHandler.SetInterval(5000);

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
        }
        public void OnAction(Tint tint)
        {
        }
        public void OnClosed()
        {
        }
        #endregion

        #region Events
        private void CBox_Presets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedPreset = CBox_Presets.SelectedIndex;

            isPresetChanging = true;
            switch (selectedPreset)
            {
                case 0:
                    NBox_ColorTemperature.Value = 1800;
                    break;
                case 1:
                    NBox_ColorTemperature.Value = 2700;
                    break;
                case 2:
                    NBox_ColorTemperature.Value = 3500;
                    break;
                case 3:
                    NBox_ColorTemperature.Value = 4100;
                    break;
                case 4:
                    NBox_ColorTemperature.Value = 6500;
                    break;
                case 5:
                    if (!isCustomValue) {
                        NBox_ColorTemperature.Value = customTemperature;
                    } else {
                        isCustomValue = false;
                    }
                    break;
            }
            isPresetChanging = false;

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<int>(prefName, "selectedPreset", selectedPreset);
            }
        }
        private void NBox_ColorTemperature_ValueChanged(object sender, RoutedEventArgs e)
        {
            colorTemperature = NBox_ColorTemperature.Value;

            if(!isPresetChanging) {
                isCustomValue = true;
                CBox_Presets.SelectedIndex = 5;
                customTemperature = colorTemperature;
            }

            UpdateBrush();

            if (isUserInputAllowed && selectedPreset == 5) {
                Core.Preferences.SetPreference<int>(prefName, "customTemperature", customTemperature);
            }
        }
        #endregion

        #region Utils
        private void UpdateBrush()
        {
            if (CBox_ColorPicker != null) {
                double offset = (double)((double)(colorTemperature - 1500) / 6000);
                Tint tint = selectedTint.GetPoint(offset);
                CBox_ColorPicker.Background = tint.GetBrush();
                uiHandler.OnEffectData(0, 0, 0x01, 0xFF, tint.Red, tint.Green, tint.Blue);
            }
        }
        #endregion
    }
}
