using System;
using System.Windows;
using System.Windows.Controls;
using LEDSystem.Core.Interfaces;
using LEDSystem.UI.Helpers;
using Newtonsoft.Json.Linq;

namespace LEDSystem.Effects
{
    // Stato: COMPLETATO (01.07.2020)
    public partial class BreathingControl : UserControl, IEffect
    {
        #region Variables
        private IHandler uiHandler;
        private string prefName = "effect1";
        private bool isUserInputAllowed = false;
        private int lightsCount = 1;
        private Tint selectedTint;
        private int maxFade = 255;
        private int minFade = 0;
        private int brightness = 0;
        private int increase = 5;
        private int colorMode = 0;
        private int lightOffset = 1;
        private Random random;
        #endregion

        #region Main
        public BreathingControl(IHandler uiHandler)
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
            // Informa gli elementi che andranno ad essere impostati di non eseguire
            // alcuna azione ad una eventuale variazione dei loro valori predefiniti.
            isUserInputAllowed = false;

            // Aggiunge le luci disponibli alla lista degli output.
            lightsCount = Core.Preferences.GetPreference<int>("config", "lightsCount");
            for (int i = 2; i < CBox_ColorMode.Items.Count; i++) {
                (CBox_ColorMode.Items[i] as ComboBoxItem).IsEnabled = lightsCount == 1 ? false : true;
            }

            // Carica le preferenze relative al colore salvato.
            selectedTint = Tint.Deserialize(Core.Preferences.GetPreference<JObject>(prefName, "colorPicked"));
            CBox_ColorPicked.Background = selectedTint.GetBrush();

            // Carica le preferenze relative alla modalità del colore.
            int tempColorMode = Core.Preferences.GetPreference<int>(prefName, "colorMode");
            if (lightsCount == 1) {
                if (tempColorMode >= 2) {
                    tempColorMode = 0;
                    Core.Preferences.SetPreference<int>(prefName, "colorMode", tempColorMode);
                }
            }
            CBox_ColorMode.SelectedIndex = tempColorMode;

            // Carica le preferenze relative al valore minimo di intensità.
            NBox_MinIntensity.Value = Core.Preferences.GetPreference<int>(prefName, "minIntensity");

            // Carica le preferenze relative al valore massimo di intensità.
            NBox_MaxIntensity.Value = Core.Preferences.GetPreference<int>(prefName, "maxIntensity");

            // Imposta la velocità del timer.
            NBox_Speed.Value = Core.Preferences.GetPreference<int>(prefName, "logicSpeed");

            // Informa gli elementi che possono eseguire le loro normali funzioni in
            // caso avvenga una variazione dei loro valori predefiniti.
            isUserInputAllowed = true;
        }
        #endregion

        #region Logic
        public void OnCreate()
        {
            random = new Random();
        }
        public void OnUpdate()
        {
            brightness += increase;
            if (brightness <= minFade) {
                brightness = minFade;
                increase = -increase;
            } else if (brightness >= maxFade) {
                brightness = maxFade;
                increase = -increase;
            }

            switch (colorMode)
            {
                case 0:
                    uiHandler.OnEffectData(0, 0, 0x02, (byte)brightness, 0x00, 0x00, 0x00);
                    break;
                case 1:
                    if (brightness == minFade) {
                        uiHandler.OnEffectData(0, 0, 0x01, (byte)brightness, (byte)random.Next(0, 256), (byte)random.Next(0, 256), (byte)random.Next(0, 256));
                    } else {
                        uiHandler.OnEffectData(0, 0, 0x02, (byte)brightness, 0x00, 0x00, 0x00);
                    }
                    break;
                case 2:
                    uiHandler.OnEffectData(1, lightOffset, 0x02, (byte)brightness, 0x00, 0x00, 0x00);
                    if (brightness == minFade) {
                        if (lightOffset + 1 < lightsCount) {
                            lightOffset += 1;
                        } else {
                            lightOffset = 0;
                        }
                        uiHandler.OnEffectData(1, lightOffset, 0x02, (byte)brightness, 0x00, 0x00, 0x00);
                    }
                    break;
                case 3:
                    uiHandler.OnEffectData(1, lightOffset, 0x02, (byte)brightness, 0x00, 0x00, 0x00);
                    if (brightness == minFade) {
                        if (lightOffset + 1 < lightsCount) {
                            lightOffset += 1;
                        } else {
                            lightOffset = 0;
                        }
                        uiHandler.OnEffectData(1, lightOffset, 0x01, (byte)brightness, (byte)random.Next(0, 256), (byte)random.Next(0, 256), (byte)random.Next(0, 256));
                    }
                    break;
                case 4:
                    uiHandler.OnEffectData(1, lightOffset, 0x02, (byte)brightness, 0x00, 0x00, 0x00);
                    if (brightness == minFade) {
                        lightOffset = random.Next(0, lightsCount);
                        uiHandler.OnEffectData(1, lightOffset, 0x02, (byte)brightness, 0x00, 0x00, 0x00);
                    }
                    break;
                case 5:
                    uiHandler.OnEffectData(1, lightOffset, 0x02, (byte)brightness, 0x00, 0x00, 0x00);
                    if (brightness == minFade) {
                        lightOffset = random.Next(0, lightsCount);
                        uiHandler.OnEffectData(1, lightOffset, 0x01, (byte)brightness, (byte)random.Next(0, 256), (byte)random.Next(0, 256), (byte)random.Next(0, 256));
                    }
                    break;
            }
        }
        public void OnAction(Tint tint)
        {
            if (tint == null) return;
            selectedTint = tint;

            CBox_ColorPicked.Background = selectedTint.GetBrush();
            Core.Preferences.SetPreference<Tint>(prefName, "colorPicked", selectedTint);

            // Invia il colore selezionato alla scheda.
            CBox_ColorMode_SelectionChanged(null, null);
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
        private void NBox_MinIntensity_ValueChanged(object sender, RoutedEventArgs e)
        {
            // Imposta il valore minimo di fade.
            int currentFade = ((NBox_MinIntensity.Value * 255) / 100);
            if (currentFade < maxFade) {
                minFade = currentFade;
            } else {
                NBox_MinIntensity.Value = (minFade * 100) / 255;
            }

            if (isUserInputAllowed)
            {
                Core.Preferences.SetPreference<int>(prefName, "minIntensity", (minFade * 100) / 255);
            }
        }
        private void NBox_MaxIntensity_ValueChanged(object sender, RoutedEventArgs e)
        {
            // Imposta il valore massimo di fade.
            int currentFade = ((NBox_MaxIntensity.Value * 255) / 100);
            if (currentFade > minFade) {
                maxFade = currentFade;
            } else {
                NBox_MaxIntensity.Value = (maxFade * 100) / 255;
            }

            if (isUserInputAllowed)
            {
                Core.Preferences.SetPreference<int>(prefName, "maxIntensity", (maxFade * 100) / 255);
            }
        }
        private void CBox_ColorMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            colorMode = -1;
            int tempColorMode = CBox_ColorMode.SelectedIndex;
            if (tempColorMode == 0 || tempColorMode == 2 || tempColorMode == 4) {
                Item_ColorPicker.Visibility = Visibility.Visible;
                uiHandler.OnEffectData(0, 0, 0x01, 0x00, selectedTint.Red, selectedTint.Green, selectedTint.Blue);
            } else {
                Item_ColorPicker.Visibility = Visibility.Collapsed;
                uiHandler.OnEffectData(0, 0, 0x01, 0x00, 0x00, 0x00, 0x00);
            }

            brightness = 0;
            increase = 5;
            colorMode = tempColorMode;

            if (isUserInputAllowed) 
            {
                Core.Preferences.SetPreference<int>(prefName, "colorMode", colorMode);
            }
        }
        private void CBox_ColorPicked_OnPickerRequest(object sender, RoutedEventArgs e)
        {
            uiHandler.OnColorPicker(0, selectedTint);
        }
        #endregion
    }
}
