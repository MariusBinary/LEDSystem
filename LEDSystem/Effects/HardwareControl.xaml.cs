using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Management;
using System.Windows.Media;
using LEDSystem.Core.Interfaces;
using LEDSystem.UI.Helpers;

namespace LEDSystem.Effects
{
    // Stato: COMPLETATO (05.07.2020)
    public partial class HardwareControl : UserControl, IEffect
    {
        #region Variables
        private IHandler uiHandler; 
        private string prefName = "effect6";
        private bool isUserInputAllowed = false;
        private int colorMode = 0;
        private int deviceIndex = -1;
        private Tint selectedTint;
        private double[] thresholdOffsets;
        #endregion

        #region Main
        public HardwareControl(IHandler uiHandler)
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

            // Carica le preferenze relative al tipo di colore salvato.
            CBox_ColorMode.SelectedIndex = Core.Preferences.GetPreference<int>(prefName, "colorMode");

            // Carica le preferenze relative al colore salvato.
            selectedTint = Core.Preferences.GetPreference<Tint>(prefName, "colorPicked");
            UpdateBrush();

            // Carica le preferenze relative al dispositivo di input.
            CBox_InputDevice.SelectedIndex = Core.Preferences.GetPreference<int>(prefName, "deviceIndex");

            // Imposta la velocità del timer.
            uiHandler.SetInterval(500);

            // Permette agli elementi di salvare i propri valori.
            isUserInputAllowed = true;
        }
        #endregion

        #region Cpu Counters
        private bool isCpuInizialized = false;
        private PerformanceCounter cpuCounter;

        private void InizializeCpuCounter()
        {
            if (!isCpuInizialized) {
                try {
                    cpuCounter = new PerformanceCounter {
                        CategoryName = "Processor",
                        CounterName = "% Processor Time",
                        InstanceName = "_Total",
                        ReadOnly = true
                    };
                    isCpuInizialized = true;
                } catch {
                    uiHandler.OnEffectError();
                }
            }
        }
        private void FreeCpuCounter()
        {
            if (isCpuInizialized) {
                cpuCounter.Dispose();
            }
        }
        private double GetCpuUsage()
        {
            return (cpuCounter.NextValue() / 100);
        }
        #endregion

        #region Ram Counter
        private bool isRamInizialized = false;
        private PerformanceCounter ramCounter;
        private double avaibleRam = 0;

        private void InizializeRamCounter()
        {
            if (!isRamInizialized) {
                try {
                    ObjectQuery wql = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher(wql);
                    ManagementObjectCollection results = searcher.Get();
                    foreach (ManagementObject result in results) {
                        avaibleRam = (Convert.ToDouble(result["TotalVisibleMemorySize"]) / 1024);
                    }
                    ramCounter = new PerformanceCounter() {
                        CategoryName = "Memory",
                        CounterName = "Available MBytes",
                        ReadOnly = true
                    };
                    isRamInizialized = true;
                } catch {
                    uiHandler.OnEffectError();
                }
            }
        }
        private void FreeRamCounter()
        {
            if (isRamInizialized) {
                ramCounter.Dispose();
            }
        }
        private double GetRamUsage()
        {
            return ((avaibleRam - ramCounter.NextValue()) / avaibleRam);
        }
        #endregion

        #region Logic
        public void OnCreate()
        {
        }
        public void OnUpdate()
        {
            // Ottiene l'offset.
            double offset = -1;
            switch (deviceIndex)
            {
                case 0:
                    if (isCpuInizialized) {
                        offset = GetCpuUsage();
                    }
                    break;
                case 1:
                    if (isRamInizialized) {
                        offset = GetRamUsage();
                    }
                    break;
            }

            // Invia il colore alla scheda.
            if (offset >= 0) {
                if (colorMode == 1) {
                    offset = thresholdOffsets.OrderBy(x => Math.Abs((double)x - offset)).First();
                }
                Tint tint = selectedTint.GetPoint(offset);
                uiHandler.OnEffectData(0, 0, 0x01, 0xFF, tint.Red, tint.Green, tint.Blue);
            }
        }
        public void OnAction(Tint tint)
        {
            if (tint == null) return;
            selectedTint = tint;

            UpdateBrush();

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<Tint>(prefName, "colorPicked", selectedTint);
            }
        }
        public void OnClosed()
        {
            // Rilascia le risorse.
            FreeCpuCounter();
            FreeRamCounter();
        }
        #endregion

        #region Events
        private void CBox_InputDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            deviceIndex = CBox_InputDevice.SelectedIndex;

            // Inizializza il contatore selezionato.
            switch (deviceIndex) {
                case 0:
                    InizializeCpuCounter();
                    break;
                case 1:
                    InizializeRamCounter();
                    break;
            }

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<int>(prefName, "deviceIndex", deviceIndex);
            }
        }
        private void CBox_ColorMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            colorMode = CBox_ColorMode.SelectedIndex;

            if (isUserInputAllowed) {
                UpdateBrush();
                Core.Preferences.SetPreference<int>(prefName, "colorMode", colorMode);
            }
        }
        private void CBox_ColorPicker_OnPickerRequest(object sender, RoutedEventArgs e)
        {
            uiHandler.OnColorPicker(1, selectedTint);
        }
        #endregion

        #region Utils 
        private Brush GetThresholdBrush(Brush brush)
        {
            LinearGradientBrush thresholdBrush = brush.Clone() as LinearGradientBrush;
            LinearGradientBrush gradientBrush = brush as LinearGradientBrush;
            for (int i = 0; i < gradientBrush.GradientStops.Count; i++) {
                if ((i + 1) < gradientBrush.GradientStops.Count) {
                    thresholdBrush.GradientStops.Add(new GradientStop() {
                        Offset = gradientBrush.GradientStops[i + 1].Offset - 0.001,
                        Color = gradientBrush.GradientStops[i].Color
                    });
                }
            }
            return thresholdBrush;
        }
        private void UpdateBrush()
        {
            thresholdOffsets = new double[selectedTint.Points.Count];
            for (int i = 0; i < selectedTint.Points.Count; i++) {
                thresholdOffsets[i] = selectedTint.Points[i].Offset;
            }

            if (colorMode == 0) {
                CBox_ColorPicker.Background = selectedTint.GetBrush();
            } else {
                CBox_ColorPicker.Background = GetThresholdBrush(selectedTint.GetBrush());
            }
        }
        #endregion
    }
}
