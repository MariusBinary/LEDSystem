using System; 
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using LEDSystem.Core.Interfaces;
using LEDSystem.UI.Helpers;
using Newtonsoft.Json.Linq;

namespace LEDSystem.Effects
{
    // Stato: COMPLETATO (06.07.2020)
    public partial class WeatherControl : UserControl, IEffect
    {
        #region Variable
        private IHandler uiHandler;
        private string prefName = "effect8";
        private bool isUserInputAllowed = false;
        private Tint selectedTint;
        private string locationName;
        private string locationId;
        private int evaluationParameter = 0;
        private int weatherCondition = 0;
        private int minTemperature = -80;
        private int maxTemperature = 80;
        private int minPressure = 920;
        private int maxPressure = 1060;
        private int colorType = 0;
        private bool canWeatherUpadate = false;
        #endregion

        #region Main
        public WeatherControl(IHandler uiHandler)
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

            // Carica le preferenze relative alla città salvata.
            locationName = Core.Preferences.GetPreference<string>(prefName, "locationName");
            locationId = Core.Preferences.GetPreference<string>(prefName, "locationID");
            TBox_Location.Text = locationName;

            // Carica le preferenze relative alla condizione meteo selezionata.
            CBox_WeatherConditions.SelectedIndex = Core.Preferences.GetPreference<int>(prefName, "weatherCondition");

            // Carica le preferenze relative alla temperatura minima / massima.
            NBox_MinTemperature.Value = Core.Preferences.GetPreference<int>(prefName, "minTemperature");
            NBox_MaxTemperature.Value = Core.Preferences.GetPreference<int>(prefName, "maxTemperature");

            // Carica le preferenze relative alla pressione minima / massima.
            NBox_MinPressure.Value = Core.Preferences.GetPreference<int>(prefName, "minPressure");
            NBox_MaxPressure.Value = Core.Preferences.GetPreference<int>(prefName, "maxPressure");

            // Carica le preferenze relative al parametro da valutare.
            CBox_EvaluationParamenter.SelectedIndex = Core.Preferences.GetPreference<int>(prefName, "evaluationParameter");

            // Imposta la velocità del timer.
            CBox_UpdatePeriod.SelectedIndex = Core.Preferences.GetPreference<int>(prefName, "updatePeriod");

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
            if (canWeatherUpadate && !String.IsNullOrEmpty(locationName) && !String.IsNullOrEmpty(locationId)) {
                UpdateWeather();
            }
        }
        public void OnAction(Tint tint)
        {
            if (tint == null) return;
            selectedTint = tint;

            if (evaluationParameter == 0) {
                JArray weatherConditions = Core.Preferences.GetPreference<JArray>(prefName, "weatherConditions");
                weatherConditions[weatherCondition] = JObject.FromObject(selectedTint);
                CBox_ColorPicker.Background = selectedTint.GetBrush();
                Core.Preferences.SetPreference<JArray>(prefName, "weatherConditions", weatherConditions);
            } else {
                CBox_ColorPicker.Background = selectedTint.GetBrush();
                Core.Preferences.SetPreference<Tint>(prefName, "colorPicked", selectedTint);
            }

            if (canWeatherUpadate && !String.IsNullOrEmpty(locationName) && !String.IsNullOrEmpty(locationId)) {
                UpdateWeather();
            }
        }
        public void OnClosed()
        {
        }
        #endregion

        #region Events
        private void CBox_UpdatePeriod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (CBox_UpdatePeriod.SelectedIndex)
            {
                case 0:
                    uiHandler.SetInterval(900000);
                    break;
                case 1:
                    uiHandler.SetInterval(1800000);
                    break;
                case 2:
                    uiHandler.SetInterval(2700000);
                    break;
                case 3:
                    uiHandler.SetInterval(3600000);
                    break;
                case 4:
                    uiHandler.SetInterval(7200000);
                    break;
                case 5:
                    uiHandler.SetInterval(10800000);
                    break;
            }

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<int>(prefName, "updatePeriod", CBox_UpdatePeriod.SelectedIndex);
            }
        }
        private void CBox_EvaluationParamenter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            evaluationParameter = CBox_EvaluationParamenter.SelectedIndex;

            canWeatherUpadate = false;
            if (evaluationParameter == 0) {
                colorType = 0;
                Item_WeatherConditions.Visibility = Visibility.Visible;
                Item_MinTemperature.Visibility = Visibility.Collapsed;
                Item_MaxTemperature.Visibility = Visibility.Collapsed;
                Item_MaxPressure.Visibility = Visibility.Collapsed;
                Item_MinPressure.Visibility = Visibility.Collapsed;         
            } else if (evaluationParameter == 1) {
                colorType = 1;
                Item_WeatherConditions.Visibility = Visibility.Collapsed;
                Item_MinTemperature.Visibility = Visibility.Visible;
                Item_MaxTemperature.Visibility = Visibility.Visible;
                Item_MaxPressure.Visibility = Visibility.Collapsed;
                Item_MinPressure.Visibility = Visibility.Collapsed;
            } else if (evaluationParameter == 2) {
                colorType = 1;
                Item_WeatherConditions.Visibility = Visibility.Collapsed;
                Item_MinTemperature.Visibility = Visibility.Collapsed;
                Item_MaxTemperature.Visibility = Visibility.Collapsed;
                Item_MaxPressure.Visibility = Visibility.Collapsed;
                Item_MinPressure.Visibility = Visibility.Collapsed;
            } else if (evaluationParameter == 3) {
                colorType = 1;
                Item_WeatherConditions.Visibility = Visibility.Collapsed;
                Item_MinTemperature.Visibility = Visibility.Collapsed;
                Item_MaxTemperature.Visibility = Visibility.Collapsed;
                Item_MaxPressure.Visibility = Visibility.Visible;
                Item_MinPressure.Visibility = Visibility.Visible;
            }

            if (evaluationParameter == 0) {
                JArray weatherConditions = Core.Preferences.GetPreference<JArray>(prefName, "weatherConditions");
                selectedTint = weatherConditions[weatherCondition].ToObject<Tint>();
                CBox_ColorPicker.Background = selectedTint.GetBrush();
            } else {
                selectedTint = Core.Preferences.GetPreference<Tint>(prefName, "colorPicked");
                CBox_ColorPicker.Background = selectedTint.GetBrush();
            }

            if (!String.IsNullOrEmpty(locationName) && !String.IsNullOrEmpty(locationId)) {
                canWeatherUpadate = true;
                UpdateWeather();
            }

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<int>(prefName, "evaluationParameter", evaluationParameter);
            }
        }
        private void CBox_WeatherConditions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            weatherCondition = CBox_WeatherConditions.SelectedIndex;

            JArray weatherConditions = Core.Preferences.GetPreference<JArray>(prefName, "weatherConditions");
            for (int i = 0; i < weatherConditions.Count; i++) {
                if (i == weatherCondition) {
                    selectedTint = weatherConditions[i].ToObject<Tint>();
                    CBox_ColorPicker.Background = selectedTint.GetBrush();
                }
            }

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<int>(prefName, "weatherCondition", weatherCondition);
            }
        }
        private void CBox_ColorPicker_OnPickerRequest(object sender, RoutedEventArgs e)
        {
            uiHandler.OnColorPicker(colorType, selectedTint);
        }
        private void NBox_MinTemperature_ValueChanged(object sender, RoutedEventArgs e)
        {
            int currentTemperate = NBox_MinTemperature.Value;
            if (currentTemperate < maxTemperature) {
                minTemperature = currentTemperate;
            } else {
                NBox_MinTemperature.Value = minTemperature;
            }

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<int>(prefName, "minTemperature", minTemperature);
            }
        }
        private void NBox_MaxTemperature_ValueChanged(object sender, RoutedEventArgs e)
        {
            int currentTemperate = NBox_MaxTemperature.Value;
            if (currentTemperate > minTemperature) {
                maxTemperature = currentTemperate;
            } else {
                NBox_MaxTemperature.Value = maxTemperature;
            }

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<int>(prefName, "maxTemperature", maxTemperature);
            }
        }
        private void NBox_MinPressure_ValueChanged(object sender, RoutedEventArgs e)
        {
            int currentPressure = NBox_MinPressure.Value;
            if (currentPressure < maxPressure) {
                minPressure = currentPressure;
            } else {
                NBox_MinPressure.Value = minPressure;
            }

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<int>(prefName, "minPressure", minPressure);
            }
        }
        private void NBox_MaxPressure_ValueChanged(object sender, RoutedEventArgs e)
        {
            int currentPressure = NBox_MaxPressure.Value;
            if (currentPressure > minPressure) {
                maxPressure = currentPressure;
            } else {
                NBox_MaxPressure.Value = maxPressure;
            }

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<int>(prefName, "maxPressure", maxPressure);
            }
        }
        private void TBox_Location_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TBox_Location.IsEnabled = false;

                string location = TBox_Location.Text;
                string result = Get($"https://api.openweathermap.org/data/2.5/weather?q={location}&appid=df32862aab765f46e5449fe9778e2da1");

                JObject obj = JObject.Parse(result);
                if ((int)obj["cod"] == 200) {
                    locationName = (string)obj["name"];
                    locationId = (string)obj["id"];

                    Core.Preferences.SetPreference<string>(prefName, "locationName", locationName);
                    Core.Preferences.SetPreference<string>(prefName, "locationID", locationId);

                    UpdateWeather();
                }

                TBox_Location.IsEnabled = true;
            }
        }
        private void Btn_WeatherUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(locationName) && !String.IsNullOrEmpty(locationId)) {
                UpdateWeather();
            }
        }
        #endregion

        #region Utils
        private string Get(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.ContentType = "application/json";
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                HttpWebResponse httpResponse = (HttpWebResponse)request.GetResponse();
                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
        private void UpdateWeather()
        {
            // Ottiene le informazioni sul meteo.
            string result = Get($"https://api.openweathermap.org/data/2.5/weather?id={locationId}&appid=df32862aab765f46e5449fe9778e2da1");
            if (result == null) {
                uiHandler.OnEffectError();
                return;
            }

            // Decodifica le informazioni sul meteo.
            JObject obj = JObject.Parse(result);
            int currentWeather = -1;
            int weatherCode = (int)obj["weather"][0]["id"];
            double temperature = Math.Round(((double)obj["main"]["temp"]) - 273.15);
            double pressure = (double)obj["main"]["pressure"];
            double humidity = (double)obj["main"]["humidity"];
            string country = (string)obj["sys"]["country"];

            // Aggiorna il widget.
            Dispatcher.Invoke(new Action(delegate {
                if (weatherCode >= 200 && weatherCode <= 232) {
                    currentWeather = 0;
                    Img_WidgetIcon.Source = new BitmapImage(new Uri("pack://application:,,,/LEDSystem;component/Images/Weather/ic_thunderstorm.png"));
                    Tx_WidgetCondition.Text = CBox_WeatherConditions.Items[0].ToString();
                } else if (weatherCode >= 300 && weatherCode <= 321) {
                    currentWeather = 1;
                    Img_WidgetIcon.Source = new BitmapImage(new Uri("pack://application:,,,/LEDSystem;component/Images/Weather/ic_drizzle.png"));
                    Tx_WidgetCondition.Text = CBox_WeatherConditions.Items[1].ToString();
                } else if (weatherCode >= 500 && weatherCode <= 531) {
                    currentWeather = 2;
                    Img_WidgetIcon.Source = new BitmapImage(new Uri("pack://application:,,,/LEDSystem;component/Images/Weather/ic_rain.png"));
                    Tx_WidgetCondition.Text = CBox_WeatherConditions.Items[2].ToString();
                } else if (weatherCode >= 600 && weatherCode <= 622) {
                    currentWeather = 3;
                    Img_WidgetIcon.Source = new BitmapImage(new Uri("pack://application:,,,/LEDSystem;component/Images/Weather/ic_snow.png"));
                    Tx_WidgetCondition.Text = CBox_WeatherConditions.Items[3].ToString();
                } else if (weatherCode >= 701 && weatherCode <= 781) {
                    currentWeather = 4;
                    Img_WidgetIcon.Source = new BitmapImage(new Uri("pack://application:,,,/LEDSystem;component/Images/Weather/ic_atmosphere.png"));
                    Tx_WidgetCondition.Text = CBox_WeatherConditions.Items[4].ToString();
                } else if (weatherCode == 800) {
                    currentWeather = 5;
                    Img_WidgetIcon.Source = new BitmapImage(new Uri("pack://application:,,,/LEDSystem;component/Images/Weather/ic_clear.png"));
                    Tx_WidgetCondition.Text = CBox_WeatherConditions.Items[5].ToString();
                } else if (weatherCode >= 801 && weatherCode <= 804) {
                    currentWeather = 6;
                    Img_WidgetIcon.Source = new BitmapImage(new Uri("pack://application:,,,/LEDSystem;component/Images/Weather/ic_clouds.png"));
                    Tx_WidgetCondition.Text = CBox_WeatherConditions.Items[6].ToString();
                }

                TBox_Location.Text = $"{locationName}, {country.ToUpper()}";
                Tx_WidgetLocataion.Text = $"{locationName}, {country.ToUpper()}";
                Tx_WidgetTemperature.Text = temperature.ToString() + "°";
            }));

            // Invia il colore alla scheda.
            if (evaluationParameter == 0) {
                JArray weatherConditions = Core.Preferences.GetPreference<JArray>(prefName, "weatherConditions");
                for (int i = 0; i < weatherConditions.Count; i++) {
                    if (i == currentWeather) {
                        Tint tint = weatherConditions[i].ToObject<Tint>();
                        uiHandler.OnEffectData(0, 0, 0x01, 0xFF, tint.Red, tint.Green, tint.Blue);
                    }
                }
            } else if (evaluationParameter == 1) {
                double offset = Core.Utils.Map(temperature, minTemperature, maxTemperature, 0, 1);
                Tint tint = selectedTint.GetPoint(offset);
                uiHandler.OnEffectData(0, 0, 0x01, 0xFF, tint.Red, tint.Green, tint.Blue);
            } else if (evaluationParameter == 2) {
                double offset = (double)(humidity / (double)100);
                Tint tint = selectedTint.GetPoint(offset);
                uiHandler.OnEffectData(0, 0, 0x01, 0xFF, tint.Red, tint.Green, tint.Blue);
            } else if (evaluationParameter == 3) {
                double offset = Core.Utils.Map(pressure, minPressure, maxPressure, 0, 1);
                Tint tint = selectedTint.GetPoint(offset);
                uiHandler.OnEffectData(0, 0, 0x01, 0xFF, tint.Red, tint.Green, tint.Blue);
            }
        }
        #endregion
    }
}
