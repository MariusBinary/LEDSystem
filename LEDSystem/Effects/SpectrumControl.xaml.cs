using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using LEDSystem.Core.Interfaces;
using LEDSystem.UI.Helpers;
using Newtonsoft.Json.Linq;
using Un4seen.Bass;
using Un4seen.BassWasapi;

namespace LEDSystem.Effects
{
    // Stato: COMPLETATO (06.07.2020)
    public partial class SpectrumControl : UserControl, IEffect
    {
        #region Variables
        private IHandler uiHandler;
        private string prefName = "effect4";
        private bool isUserInputAllowed = false;
        private bool isColorChanging = false;
        private int lightsCount = 1;
        private Tint selectedTint;
        private List<int> inputIndexes = new List<int>();
        private List<int> inputFrequencies = new List<int>();
        private List<int> inputChannels = new List<int>();
        private int deviceIndex = -1;
        private WASAPIPROC process;
        private float[] fftBuffer = new float[8192];
        private int fftChannels = 60;
        private int fftFilter = 0;
        private int fftMultiplier = 1;
        private int[] minfilters = new int[] { 00, 19, 39 };
        private int[] maxfilters = new int[] { 19, 39, 59 };
        private int colorMode = 0;
        private int colorType = 0;
        private double increase = 0.01;
        private double offset = 0.0;
        private int peakThreshold = 115;
        private Random random;
        private int ledShiftInterval = 0;
        private int ledShiftOffset = 0;
        private bool isLedShift = false;
        private byte ledShiftR;
        private byte ledShiftG;
        private byte ledShiftB;
        private bool isLedThreshold = false;
        private bool canLedThreshold = true;
        private int randomLight = 0;
        #endregion

        #region Main
        public SpectrumControl(IHandler uiHandler)
        {
            // Imposta il gestore fornito al controllo.
            this.uiHandler = uiHandler;
            // Inizializza i componenti grafici.
            InitializeComponent();
            // Carica le preferenze dell'utente.
            LoadPreferences();
        }
        private void LoadPreferences()
        {
            // Impedisce agli elementi di salvare i propri valori.
            isUserInputAllowed = false;

            // Carica i dispositivi audio di output disponibili.
            int devicecount = BassWasapi.BASS_WASAPI_GetDeviceCount();
            for (int i = 0; i < devicecount; i++)
            {
                var device = BassWasapi.BASS_WASAPI_GetDeviceInfo(i);
                if (device.IsEnabled && device.IsLoopback)
                {
                    inputIndexes.Add(i);
                    inputFrequencies.Add(device.mixfreq);
                    inputChannels.Add(device.mixchans);
                }
            }
            BASS_DEVICEINFO info = new BASS_DEVICEINFO();
            for (int n = 1; Bass.BASS_GetDeviceInfo(n, info); n++)
            {
                CBox_InputDevice.Items.Add(info.ToString());
                if (info.IsDefault)
                {
                    CBox_InputDevice.SelectedIndex = n - 1;
                    deviceIndex = n - 1;
                }
            }

            // Carica le preferenze relative al moltiplicatore di picco.
            NBox_PeakMultiplier.Value = Core.Preferences.GetPreference<int>(prefName, "audioPeakMultiplier");

            // Carica le preferenze relative al filtro della banda.
            fftFilter = Core.Preferences.GetPreference<int>(prefName, "audioBandFilter");
            switch (fftFilter)
            {
                case 0: Rad_FilterLow.IsChecked = true;
                    break;
                case 1: Rad_FilterMedium.IsChecked = true;
                    break;
                case 2: Rad_FilterHigh.IsChecked = true;
                    break;
            }

            // Aggiunge le luci disponibli alla lista degli output.
            lightsCount = Core.Preferences.GetPreference<int>("config", "lightsCount");
            for (int i = 4; i < CBox_ColorMode.Items.Count; i++) {
                (CBox_ColorMode.Items[i] as ComboBoxItem).IsEnabled = lightsCount == 1 ? false : true;
            }

            // Carica le preferenze relative al tipo di colore salvato.
            int tempColorMode = Core.Preferences.GetPreference<int>(prefName, "colorMode");
            if (lightsCount == 1) {
                if (tempColorMode >= 4) {
                    tempColorMode = 0;
                    Core.Preferences.SetPreference<int>(prefName, "colorMode", tempColorMode);
                }
            }
            CBox_ColorMode.SelectedIndex = tempColorMode;

            // Carica le preferenze relative alla sensibilità del picco.
            NBox_PeakSensibility.Value = Core.Preferences.GetPreference<int>(prefName, "peakSensibility");

            // Imposta la velocità del timer.
            uiHandler.SetInterval(25);

            // Permette agli elementi di salvare i propri valori.
            isUserInputAllowed = true;
        }
        #endregion

        #region Logic
        public void OnCreate()
        {
            // Inizializza l'istanza random.
            random = new Random();

            // Configura il processo di BassDLL.
            process = new WASAPIPROC(Process);
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATETHREADS, false);
            Bass.BASS_Init(0, inputFrequencies[deviceIndex], BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);

            // Inizializza il processo WASAPI di BassDLL.
            if (BassWasapi.BASS_WASAPI_Init(inputIndexes[deviceIndex], inputFrequencies[deviceIndex], 
                inputChannels[deviceIndex], BASSWASAPIInit.BASS_WASAPI_BUFFER, 1f, 0.05f, process, IntPtr.Zero))
                BassWasapi.BASS_WASAPI_Start();
            else uiHandler.OnEffectError();
        }
        public void OnUpdate()
        {
            // Ottiene i dati dal dispositivo di output.
            int ret = BassWasapi.BASS_WASAPI_GetData(fftBuffer, (int)BASSData.BASS_DATA_FFT8192);
            if (ret < -1) return;

            // Analizza i dati e fa una media del livello delle frequenze che rientrano nella
            // banda selezionata dall'utente.
            int x = 0, y = 0, b0 = 0, average = 0;
            for (x = minfilters[fftFilter]; x < maxfilters[fftFilter]; x++)
            {
                float peak = 0;
                int b1 = (int)Math.Pow(2, x * 10.0 / (fftChannels - 1));

                if (b1 > 1023) {
                    b1 = 1023;
                }

                if (b1 <= b0) {
                    b1 = b0 + 1;
                }

                for (; b0 < b1; b0++)
                    if (peak < fftBuffer[1 + b0]) peak = fftBuffer[1 + b0];

                y = (int)(Math.Sqrt(peak) * 3 * 255 - 4);
                y = y * fftMultiplier;
                y = y > 255 ? 255 : y;
                y = y < 0 ? 0 : y;

                average += y;
            }
            average /= 20;

            // Calcola il picco di uscita.
            if (isLedThreshold) {
                isLedThreshold = false;
                canLedThreshold = false;
            } else {
                if (average <= peakThreshold && !isLedThreshold) {
                    canLedThreshold = true;
                }
                if (average >= peakThreshold && canLedThreshold) {
                    isLedThreshold = true;
                }
            }

            // Invia il colore alla scheda.
            if (!isColorChanging)
            {
                switch (colorMode)
                {
                    case 0: //Group static color (OK)
                        uiHandler.OnEffectData(0, 0, 0x02, (byte)average, 0x00, 0x00, 0x00);
                        break;
                    case 1: //Group gradient levels (OK)
                        Tint tint1 = selectedTint.GetPoint(Core.Utils.GetProportionalValue(average, 255, 1.0));
                        uiHandler.OnEffectData(0, 0, 0x01, 0xFF, tint1.Red, tint1.Green, tint1.Blue);
                        break;
                    case 2: //Group colors cycle (OK)
                        offset += increase;
                        if (offset >= 1.0) {
                            offset = 0.0;
                        }
                        Tint tint2 = selectedTint.GetPoint(offset);
                        uiHandler.OnEffectData(0, 0, 0x01, (byte)average, tint2.Red, tint2.Green, tint2.Blue);
                        break;
                    case 3: //Group with random colors on peak (OK)
                        if (isLedThreshold || isLedShift) {
                            if (isLedShift) {
                                ledShiftInterval -= 20;
                                uiHandler.OnEffectData(0, 0, 0x01, (byte)ledShiftOffset, ledShiftR, ledShiftG, ledShiftB);
                                ledShiftOffset -= 10;
                                if (ledShiftInterval <= 0) {
                                    uiHandler.OnEffectData(0, 0, 0x02, 0x00, 0x00, 0x00, 0x00);
                                    isLedShift = false;
                                    ledShiftInterval = 500;
                                    ledShiftOffset = 255;
                                }
                                if (isLedThreshold) {
                                    isLedShift = true;
                                    ledShiftInterval = 500;
                                    ledShiftOffset = 255;
                                }
                            } else {
                                isLedShift = true;
                                ledShiftR = (byte)random.Next(0, 256);
                                ledShiftG = (byte)random.Next(0, 256);
                                ledShiftB = (byte)random.Next(0, 256);
                            }
                        }
                        break;
                    case 4: //Random lights and colors on peak (OK)
                        if (isLedThreshold || isLedShift) {
                            if (isLedShift) {
                                ledShiftInterval -= 20;
                                for (int i = 0; i < lightsCount; i++) {
                                    if (i == randomLight) {
                                        uiHandler.OnEffectData(2, randomLight, 0x01, (byte)ledShiftOffset, ledShiftR, ledShiftG, ledShiftB);
                                    } else {
                                        uiHandler.OnEffectData(2, i, 0x02, 0x00, 0x00, 0x00, 0x00);
                                    }
                                }
                                uiHandler.OnEffectData(3, 0, 0x00, 0x00, 0x00, 0x00, 0x00);
                                ledShiftOffset -= 10;
                                if (ledShiftInterval <= 0) {
                                    uiHandler.OnEffectData(1, randomLight, 0x02, 0x00, 0x00, 0x00, 0x00);
                                    isLedShift = false;
                                    ledShiftInterval = 500;
                                    ledShiftOffset = 255;
                                }
                                if (isLedThreshold) {
                                    isLedShift = true;
                                    ledShiftInterval = 500;
                                    ledShiftOffset = 255;
                                    if (random.Next(0, 256) % 2 == 0) {
                                        randomLight = (byte)random.Next(0, lightsCount);
                                    }
                                }
                            } else {
                                isLedShift = true;
                                ledShiftR = (byte)random.Next(0, 256);
                                ledShiftG = (byte)random.Next(0, 256);
                                ledShiftB = (byte)random.Next(0, 256);
                                randomLight = (byte)random.Next(0, lightsCount);
                            }
                        }
                        break;
                    case 5: //Fade lights tail on peak (OK)
                        if (isLedThreshold || isLedShift) {
                            if (isLedShift) {
                                if (ledShiftOffset < lightsCount) {
                                    ledShiftInterval += 50;
                                    for (int i = 0; i < lightsCount; i++) {
                                        if (i == ledShiftOffset) {
                                            uiHandler.OnEffectData(2, i, 0x01, 0xFF, ledShiftR, ledShiftG, ledShiftB);
                                        } else {
                                            uiHandler.OnEffectData(2, i, 0x02, 0x00, 0x00, 0x00, 0x00);
                                        }
                                    }
                                    uiHandler.OnEffectData(3, 0, 0x00, 0x00, 0x00, 0x00, 0x00);
                                    if (ledShiftInterval >= 100) {
                                        ledShiftInterval = 0;
                                        ledShiftOffset += 1;
                                    }
                                } else {
                                    uiHandler.OnEffectData(0, 0, 0x02, 0x00, 0x00, 0x00, 0x00);
                                    isLedShift = false;
                                    ledShiftInterval = 0;
                                    ledShiftOffset = 0;
                                }
                            } else {
                                isLedShift = true;
                                ledShiftR = (byte)random.Next(0, 256);
                                ledShiftG = (byte)random.Next(0, 256);
                                ledShiftB = (byte)random.Next(0, 256);
                            }
                        }
                        break;
                    case 6: //Shifting lights tail on peak (OK)
                        if (isLedThreshold || isLedShift) {
                            Tint tint6 = selectedTint.GetPoint(1);
                            if (isLedShift) {
                                if (ledShiftOffset < lightsCount) {
                                    ledShiftInterval += 50;
                                    for (int i = 0; i < lightsCount; i++) {
                                        if (i == ledShiftOffset) {
                                            uiHandler.OnEffectData(2, i, 0x01, 0xFF, ledShiftR, ledShiftG, ledShiftB);
                                        }
                                    }
                                    uiHandler.OnEffectData(3, 0, 0x00, 0x00, 0x00, 0x00, 0x00);
                                    if (ledShiftInterval >= 100) {
                                        ledShiftInterval = 0;
                                        ledShiftOffset += 1;
                                    }
                                } else {
                                    isLedShift = false;
                                    ledShiftInterval = 0;
                                    ledShiftOffset = 0;
                                }
                            } else {
                                isLedShift = true;
                                ledShiftR = (byte)random.Next(0, 256);
                                ledShiftG = (byte)random.Next(0, 256);
                                ledShiftB = (byte)random.Next(0, 256);
                            }
                        }
                        break;
                    case 7: //Random patterns on peak (OK)
                        if (isLedThreshold) {
                            int colorValue = (byte)random.Next(0, 256);
                            if (colorValue % 2 == 0) {
                                ledShiftR = (byte)random.Next(0, 256);
                                ledShiftG = (byte)random.Next(0, 256);
                                ledShiftB = (byte)random.Next(0, 256);
                            }
                            for (int i = 0; i < lightsCount; i++) {
                                int lightValue = (byte)random.Next(0, 256);
                                if (lightValue % 2 == 0) {
                                    uiHandler.OnEffectData(2, i, 0x01, 0xFF, ledShiftR, ledShiftG, ledShiftB);
                                } else {
                                    uiHandler.OnEffectData(2, i, 0x02, 0x00, 0x00, 0x00, 0x00);
                                }
                            }
                            uiHandler.OnEffectData(3, 0, 0x00, 0x00, 0x00, 0x00, 0x00);
                        }
                        break;
                    case 8: //VU meter (OK)
                        int eqLightsCount = (int)Math.Ceiling((double)(average * lightsCount) / 255);
                        eqLightsCount = average <= 20 ? 0 : eqLightsCount;
                        for (int i = 0; i < lightsCount; i++) {
                            if (i < eqLightsCount) {
                                uiHandler.OnEffectData(2, i, 0x02, 0xFF, 0x00, 0x00, 0x00);
                            } else {
                                uiHandler.OnEffectData(2, i, 0x02, 0x00, 0x00, 0x00, 0x00);
                            }
                        }
                        uiHandler.OnEffectData(3, 0, 0x00, 0x00, 0x00, 0x00, 0x00);
                        break;
                }
            }

            // Aggiorna il livello del picco.
            Dispatcher.Invoke(new Action(delegate {
                Prg_PeakLevel.Value = average;
            }));
        }
        public void OnAction(Tint tint)
        {
            if (tint == null) return;

            // Impedisce agli effetti di aggiornarsi.
            isColorChanging = true;

            selectedTint = tint;
            CBox_ColorPicker.Background = selectedTint.GetBrush();

            // Salva il colore.
            JArray savedTint = Core.Preferences.GetPreference<JArray>(prefName, "colorPicked");
            savedTint[colorType] = JObject.FromObject(selectedTint);
            Core.Preferences.SetPreference<JArray>(prefName, "colorPicked", savedTint);

            if (colorMode == 0 || colorMode == 8) {
                uiHandler.OnEffectData(0, 0, 0x01, 0xFF, selectedTint.Red, selectedTint.Green, selectedTint.Blue);
            }

            // Permette agli effetti di aggiornarsi.
            isColorChanging = false;
        }
        public void OnClosed()
        {
            // Arresta il processo di BassDLL.
            BassWasapi.BASS_WASAPI_Stop(true);
            BassWasapi.BASS_WASAPI_Free();
            Bass.BASS_Free();
        }
        #endregion

        #region Events
        private void Rad_FilterLow_Click(object sender, RoutedEventArgs e)
        {
            if (isUserInputAllowed && Rad_FilterLow.IsChecked.Value)
            {
                fftFilter = 0;
                Core.Preferences.SetPreference<int>(prefName, "audioBandFilter", 0);
            }
        }
        private void Rad_FilterMedium_Click(object sender, RoutedEventArgs e)
        {
            if (isUserInputAllowed && Rad_FilterMedium.IsChecked.Value)
            {
                fftFilter = 1;
                Core.Preferences.SetPreference<int>(prefName, "audioBandFilter", 1);
            }
        }
        private void Rad_FilterHigh_Click(object sender, RoutedEventArgs e)
        {
            if (isUserInputAllowed && Rad_FilterHigh.IsChecked.Value)
            {
                fftFilter = 2;
                Core.Preferences.SetPreference<int>(prefName, "audioBandFilter", 2);
            }
        }
        private void NBox_PeakMultiplier_ValueChanged(object sender, RoutedEventArgs e)
        {
            fftMultiplier = NBox_PeakMultiplier.Value;
            if (isUserInputAllowed)
            {
                Core.Preferences.SetPreference<int>(prefName, "audioPeakMultiplier", NBox_PeakMultiplier.Value);
            }
        }
        private void NBox_Speed_ValueChanged(object sender, RoutedEventArgs e)
        {
            increase = (0.25 / NBox_Speed.Value);
        }
        private void NBox_PeakSensibility_ValueChanged(object sender, RoutedEventArgs e)
        {
            peakThreshold = (NBox_PeakSensibility.Value * 255) / 100;
            Line_Peak.Margin = new Thickness(((peakThreshold * 340) / 255),0,0,0);

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<int>(prefName, "peakSensibility", NBox_PeakSensibility.Value);
            }
        }
        private void CBox_InputDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUserInputAllowed)
            {
                deviceIndex = CBox_InputDevice.SelectedIndex;
                OnClosed();
                OnCreate();
            }
        }
        private void CBox_ColorMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Impedisce agli effetti di aggiornarsi.
            isColorChanging = true;

            // Carica le preferenze relative al colore salvato.
            colorMode = CBox_ColorMode.SelectedIndex;
            colorType = (colorMode == 1 || colorMode == 2 || colorMode == 6) ? 1 : 0;
            selectedTint = Core.Preferences.GetPreference<JArray>(prefName, "colorPicked")[colorType].ToObject<Tint>();
            CBox_ColorPicker.Background = selectedTint.GetBrush();

            // Reinizializza le variabili degli effetti.
            ledShiftInterval = 0;
            ledShiftOffset = 0;
            isLedShift = false;
            isLedThreshold = false;
            canLedThreshold = true;

            // Aggiorna l'interfaccia grafica in base al tipo di effetto.
            if (colorMode == 0 || colorMode == 8) {
                uiHandler.OnEffectData(0, 0, 0x01, 0xFF, selectedTint.Red, selectedTint.Green, selectedTint.Blue);
                Item_CycleSpeed.Visibility = Visibility.Collapsed;
                Item_ColorPicker.Visibility = Visibility.Visible;
                Item_PeakSensibility.Visibility = Visibility.Collapsed;
                Line_Peak.Visibility = Visibility.Collapsed;
            } else if (colorMode == 1 || colorMode == 2) {
                Item_CycleSpeed.Visibility = (colorMode == 2) ? Visibility.Visible : Visibility.Collapsed;
                Item_ColorPicker.Visibility = Visibility.Visible;
                Item_PeakSensibility.Visibility = Visibility.Collapsed;
                Line_Peak.Visibility = Visibility.Collapsed;
            } else {
                if (random == null) {
                    random = new Random();
                }
                ledShiftR = (byte)random.Next(0, 256);
                ledShiftG = (byte)random.Next(0, 256);
                ledShiftB = (byte)random.Next(0, 256);
                Item_CycleSpeed.Visibility = Visibility.Collapsed;
                Item_ColorPicker.Visibility = Visibility.Collapsed;
                Item_PeakSensibility.Visibility = Visibility.Visible;
                Line_Peak.Visibility = Visibility.Visible;
            }

            // Permette agli effetti di aggiornarsi.
            isColorChanging = false;

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<int>(prefName, "colorMode", colorMode);
            }
        }
        private void CBox_ColorPicker_OnPickerRequest(object sender, RoutedEventArgs e)
        {
            uiHandler.OnColorPicker(colorType, selectedTint);
        }
        #endregion

        #region Utils
        private int Process(IntPtr buffer, int length, IntPtr user)
        {
            return length;
        }
        #endregion
    }
}
