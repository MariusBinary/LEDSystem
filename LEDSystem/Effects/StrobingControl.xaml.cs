using System.Windows;
using System.Windows.Controls;
using LEDSystem.Core.Interfaces;
using LEDSystem.UI.Helpers;

namespace LEDSystem.Effects
{
    // Stato: COMPLETATO (01.07.2020)
    public partial class StrobingControl : UserControl, IEffect
    {
        #region Variable
        private IHandler uiHandler;
        private bool isUserInputAllowed = false;
        private string prefName = "effect2";
        private Tint selectedTint;
        private bool blinkStatus = false;
        #endregion

        #region Main
        public StrobingControl(IHandler uiHandler)
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

            // Carica le preferenze relative al colore salvato.
            selectedTint = Core.Preferences.GetPreference<Tint>(prefName, "colorPicked");
            CBox_ColorPicked.Background = selectedTint.GetBrush();

            // Invia il colore alla scheda.
            uiHandler.OnEffectData(0, 0, 0x01, 0xFF, selectedTint.Red, selectedTint.Green, selectedTint.Blue);

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
            uiHandler.OnEffectData(0, 0, 0x02, (blinkStatus ? (byte)0xFF : (byte)0x00), 0x00, 0x00, 0x00);
            blinkStatus = !blinkStatus;
        }
        public void OnAction(Tint tint)
        {
            if (tint == null) return;
            selectedTint = tint;

            CBox_ColorPicked.Background = selectedTint.GetBrush();
            Core.Preferences.SetPreference<Tint>(prefName, "colorPicked", selectedTint);

            // Invia il colore alla scheda.
            uiHandler.OnEffectData(0, 0, 0x01, 0xFF, selectedTint.Red, selectedTint.Green, selectedTint.Blue);
        }
        public void OnClosed()
        {
  
        }
        #endregion

        #region Events
        private void NBox_Speed_ValueChanged(object sender, RoutedEventArgs e)
        {
            // Imposta la velocità del timer.
            uiHandler.SetInterval(NBox_Speed.Value);

            if (isUserInputAllowed)
            {
                Core.Preferences.SetPreference<int>(prefName, "logicSpeed", NBox_Speed.Value);
            }
        }
        private void CBox_ColorPicked_OnPickerRequest(object sender, RoutedEventArgs e)
        {
            uiHandler.OnColorPicker(0, selectedTint);
        }
        #endregion
    }
}
