using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using LEDSystem.Core.Interfaces;
using LEDSystem.UI.Helpers;
using Newtonsoft.Json.Linq;

namespace LEDSystem.Effects
{
    // Stato: COMPLETATO (01.07.2020)
    public partial class StaticControl : UserControl, IEffect
    {
        #region Variables
        private IHandler uiHandler;
        private string prefName = "effect0";
        private bool isUserInputAllowed = false;
        private bool isIntensityChangeAllowed = true;
        private int lightsCount = 1;
        private int selectedLight = 0;
        private Tint selectedTint;
        private int intensity;
        #endregion

        #region Main
        public StaticControl(IHandler uiHandler)
        {     
            // Imposta il gestore dell'effetto fornito come parametro
            this.uiHandler = uiHandler;
            // Inizializza i componenti dell'interfaccia grafica
            InitializeComponent();
            // Carica le preferenze dell'utente
            LoadPreferences();
        }    
        public void LoadPreferences()
        {
            // Impedisce agli elementi di salvare i propri valori.
            isUserInputAllowed = false;

            // Aggiunge le luci disponibli alla lista degli output.
            lightsCount = Core.Preferences.GetPreference<int>("config", "lightsCount");
            for (int i = 0; i < lightsCount + 1; i++)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Style = FindResource(i == 0 ? "singleLedItem" : "multipleLedItem") as Style;
                item.Content = (i == 0) ? (string)FindResource("effectAllLights") : $"{(string)FindResource("effectSingleLight")} {i}";
                CBox_SelectedLight.Items.Add(item);
            }

            // Imposta la luce selezionata.
            selectedLight = Core.Preferences.GetPreference<int>(prefName, "selectedLight");
            if (selectedLight > lightsCount) {
                selectedLight = 0;
            }
            CBox_SelectedLight.SelectedIndex = selectedLight;

            //Imposta la velocità del timer.
            uiHandler.SetInterval(5000);

            // Permette agli elementi di salvare i propri valori.
            isUserInputAllowed = true;
        }
        public void ChangeLight(int index)
        {
            // Impedisce al controllo di intensità di inviare il colore.
            isIntensityChangeAllowed = false;

            selectedTint = null;
            JArray lights = Core.Preferences.GetPreference<JArray>(prefName, "lights");

            if (index >= 1)
            {
                // Se il numero di colori salvati è inferiore a quello delle luci disponibili,
                // creare dei nuovi colori clonando quello di tutte le luci, fino a raggiungere 
                // il numero di luci disponibili.
                int unavaibleTints = lightsCount - (lights.Count - 1);
                if (unavaibleTints > 0)
                {
                    for (int i = 0; i < unavaibleTints; i++)
                    {
                        lights.Add(JToken.FromObject(lights[0]));
                    }

                    // Salvare i nuovi colori.
                    Core.Preferences.SetPreference<JArray>(prefName, "lights", lights);
                }

                // Analizzare tutti i colori disponibili e scegliere quello che corrisponde
                // all'indice fornito.
                for (int i = 1; i < lightsCount + 1; i++)
                {
                    JObject obj = (JObject)lights[i];
                    if (i == index)
                    {
                        selectedTint = obj["colorPicked"].ToObject<Tint>();
                        Seek_Intensity.Value = obj["intensity"].ToObject<int>();
                    }

                    // Impostare il colore delle luci.
                    Tint tempTint = obj["colorPicked"].ToObject<Tint>();
                    byte tempIntensity = Convert.ToByte(obj["intensity"].ToObject<int>());
                    uiHandler.OnEffectData(2, i - 1, 0x01, tempIntensity, tempTint.Red, tempTint.Green, tempTint.Blue);
                }

                // Inviare il colore alla scheda.
                uiHandler.OnEffectData(3, 0, 0x00, 0x00, 0x00, 0x00, 0x00);
            }
            else
            {
                selectedTint = lights[0]["colorPicked"].ToObject<Tint>();
                Seek_Intensity.Value = lights[0]["intensity"].ToObject<int>();

                // Inviare il colore alla scheda.
                uiHandler.OnEffectData(0, 0, 0x01, (byte)intensity, selectedTint.Red, selectedTint.Green, selectedTint.Blue);
            }

            // Aggiornare l'interfaccia grafica con il nuovo colore.
            CBox_ColorPicked.Background = selectedTint.GetBrush();
            (Seek_Intensity.Background as LinearGradientBrush).GradientStops[1].Color = selectedTint.GetColor();

            // Permette al controllo di intensità di inviare il colore.
            isIntensityChangeAllowed = true;
        }
        #endregion

        #region Handler
        public void OnCreate()
        {

        }
        public void OnUpdate()
        {
        }
        public void OnAction(Tint tint)
        {
            if (tint == null) return;
            selectedTint = tint;

            // Salvare il colore.
            JArray lights = Core.Preferences.GetPreference<JArray>(prefName, "lights");
            lights[selectedLight]["colorPicked"] = JToken.FromObject(selectedTint);
            Core.Preferences.SetPreference<JArray>(prefName, "lights", lights);

            // Aggiornare l'interfaccia grafica con il nuovo colore.
            CBox_ColorPicked.Background = selectedTint.GetBrush();
            (Seek_Intensity.Background as LinearGradientBrush).GradientStops[1].Color = selectedTint.GetColor();

            // Inviare il colore alla scheda.
            if (selectedLight == 0) {
                uiHandler.OnEffectData(0, 0, 0x01, (byte)intensity, selectedTint.Red, selectedTint.Green, selectedTint.Blue);
            } else {
                uiHandler.OnEffectData(1, selectedLight - 1, 0x01, (byte)intensity, selectedTint.Red, selectedTint.Green, selectedTint.Blue);
            }
        }
        public void OnClosed()
        {
        }
        #endregion

        #region Controls 
        private void CBox_SelectedLight_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedLight = CBox_SelectedLight.SelectedIndex;
            ChangeLight(selectedLight);

            if (isUserInputAllowed)
            {
                Core.Preferences.SetPreference<int>(prefName, "selectedLight", selectedLight);
            }
        }
        private void CBox_ColorPicked_OnPickerRequest(object sender, RoutedEventArgs e)
        {
            uiHandler.OnColorPicker(0, selectedTint);
        }
        private void Seek_Intensity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Aggiorna il valore dell'intensità
            intensity = Convert.ToInt32(Seek_Intensity.Value);

            if (isIntensityChangeAllowed)
            {
                // Inviare il colore alla scheda.
                if (selectedLight == 0) {
                    uiHandler.OnEffectData(0, 0, 0x02, (byte)intensity, 0x00, 0x00, 0x00);
                } else {
                    uiHandler.OnEffectData(1, selectedLight - 1, 0x02, (byte)intensity, 0x00, 0x00, 0x00);
                }
            }
        }
        private void Seek_Intensity_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (isUserInputAllowed)
            {
                JArray lights = Core.Preferences.GetPreference<JArray>(prefName, "lights");
                lights[selectedLight]["intensity"] = intensity;
                Core.Preferences.SetPreference<JArray>(prefName, "lights", lights);
            }
        }
        #endregion
    }
}
