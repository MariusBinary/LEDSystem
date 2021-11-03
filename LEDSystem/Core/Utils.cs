using System;
using System.Windows;
using System.Windows.Media;
using System.IO.Ports;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Windows.Forms;

namespace LEDSystem.Core
{
    public class Utils
    {
        #region Registy Utils
        /// <summary>
        /// Inserisce il percorso dell'applicazione all'interno del registro di sistema, in modo
        /// che venga eseguita all'avvio di Windows.
        /// </summary>
        public static void AddToStartup(string app, string path, string arg)
        {
            if (IsAddedToStartup(app)) return;

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    key.SetValue(app, "\"" + path + "\"" + " " + arg);
                }
            }
            catch
            {

            }
        }
        /// <summary>
        /// Rimuove il percorso dell'applicazione dall'interno del registro di sistema, in modo
        /// che non venga eseguita all'avvio di Windows.
        /// </summary>
        public static void RemoveFromStartup(string app)
        {
            if (!IsAddedToStartup(app)) return;

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    key.DeleteValue(app, false);
                }
            }
            catch
            {

            }
        }
        /// <summary>
        /// Verifica se il percorso dell'applicazione è già presente all'interno del registro
        /// di sistema, e se è impostato per essere eseguito all'avvio di Windows.
        /// </summary>
        public static bool IsAddedToStartup(string app)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    return key.GetValue(app) != null ? true : false;
                }
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region System Utils
        /// <summary>
        /// Restituisce l'architettura del sistema operativo sul quale è
        /// in esecuzione il programma.
        /// </summary>
        public static string GetSystemArchitecture()
        {
            return (Marshal.SizeOf(IntPtr.Zero) == 8 ? " (x64)" : " (x86)");
        }
        #endregion

        #region Controls Utils
        /// <summary>
        /// Imposta uno stato di visibilità agli elementi grafici.
        /// </summary>
        public static void SetVisibility(UIElement ui, bool status)
        {
            ui.Visibility = (status == true ? Visibility.Visible : Visibility.Collapsed);
        }
        /// <summary>
        /// Ritorna le dimensioni della finestra principale in base alle dimensioni
        /// dello schermo.
        /// </summary>
        public static Size GetWindowSize() {

            double savedWidth = Core.Preferences.GetPreference<double>("settings", "width");
            double savedHeight = Core.Preferences.GetPreference<double>("settings", "height");
            if (savedWidth != -1 && savedHeight != -1) {
                return new Size(savedWidth, savedHeight);
            }

            float height = SystemInformation.VirtualScreen.Height;
            float width = SystemInformation.VirtualScreen.Width;
            if (width <= 1920 && height <= 1080) {
                Core.Preferences.SetPreference<double>("settings", "width", 1450);
                Core.Preferences.SetPreference<double>("settings", "height", 700);
                return new Size(1450, 700);
            } else {
                Core.Preferences.SetPreference<double>("settings", "width", 2030);
                Core.Preferences.SetPreference<double>("settings", "height", 1030);
                return new Size(2030, 1030);
            }
        }
        /// <summary>
        /// Ritorna il fattore di scaling in base alle dimensioni dello schermo.
        /// </summary>
        public static int GetDefaultScale()
        {
            float height = SystemInformation.VirtualScreen.Height;
            float width = SystemInformation.VirtualScreen.Width;
            if (width <= 1920 && height <= 1080) {
                return 0;
            } else {
                return 2;
            }
        }
        #endregion

        #region Serial Utils
        /// <summary>
        /// Restituisce le porte seriali disponibili per una eventuale connessione.
        /// </summary>
        public static string[] GetAvaiblePorts()
        {
            return SerialPort.GetPortNames();
        }
        /// <summary>
        /// Restituisce l'indice numerico di una porta seriale fornita in ingresso.
        /// </summary>
        public static int GetPortIndex(string com)
        {
            int i = 0;
            var portNames = SerialPort.GetPortNames();
            foreach (var port in portNames)
                if (port.ToString().Equals(com))
                    break;
                else i++;
            return i;
        }
        /// <summary>
        /// Indica se la porta seriale fornita in ingresso è disponibile per una
        /// eventuale connessione, oppure è occupata.
        /// </summary>
        public static bool IsAvaiblePort(string com)
        {
            var portNames = SerialPort.GetPortNames();
            foreach (var port in portNames)
                if (port.ToString().Equals(com))
                    return true;
            return false;
        }
        #endregion

        #region Colors Utils
        public static Brush GetBrushFromHex(string hex)
        {
            var converter = new BrushConverter();
            return (Brush)converter.ConvertFromString(hex);
        }
        #endregion

        #region Math Utils
        /// <summary>
        /// Restituisce un valore proporzionato in base ai parametri di ingresso.
        /// </summary>
        public static double GetProportionalValue(double value, double from, double to)
        {
            return (value * to) / from;
        }
        public static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }
        public static double Map(double x, double in_min, double in_max, double out_min, double out_max)
        {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }
        #endregion
    }
}
