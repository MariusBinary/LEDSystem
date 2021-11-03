using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Diagnostics;
using LEDSystem.Core.Constants;
using LEDSystem.Core.Handlers;
using LEDSystem.Core.Interfaces;
using LEDSystem.Core.Models;
using LEDSystem.UI.Helpers;
using LEDSystem.UI.Controls.ColorPicker;
using LEDSystem.Core;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Reflection;
using System.Threading.Tasks;

namespace LEDSystem.UI.Pages
{
    public partial class MainWindow : Window, ISerial, IHandler
    {
        #region Variable
        private SerialHandler serialHandler;
        private SerialStatus serialStatus;
        private IEffect effectInterface;
        private UserControl effectControl;
        private TimerHandler timerHandler;
        private int sizeStatus = 0;
        private int effectIndex = -1;
        private int pageIndex = -1;
        private bool isNavbarOpen = false;
        private bool isCloseForced = false;
        private bool isUserInputAllowed;
        private Action<int> messageCallback;
        private bool isConnectionAllowed = false;
        private int connectionType;
        private int lightsCount;
        private int boardIndex;
        private string usbPort;
        private string lanIpAddress;
        private int usbBaud;
        private int lanPort;
        #endregion

        #region Main
        /// <summary>
        /// Punto di entrata principale del programma.
        /// </summary>
        public MainWindow()
        {
            // Imposta le variabili su uno stato predefinito.
            this.serialStatus = SerialStatus.DISCONNECTED;
            //Inzializza il gestore di connessione.
            this.serialHandler = new SerialHandler(this);
            // Inizializza i componenti grafici.
            InitializeComponent();
            // Aggiunge al titolo della finestra l'architettura del programma.
            this.Title += Core.Utils.GetSystemArchitecture();
            // Imposta le dimensioni della finestra.
            Size windowSize = Core.Utils.GetWindowSize();
            this.Width = windowSize.Width;
            this.Height = windowSize.Height;
            // Carica le preferenze dell'utente.
            LoadPreferences();
            // Ottiene i parametri di avvio.
            string[] args = Environment.GetCommandLineArgs();
            // Verifica sono presenti parametri di avvio validi.
            if (args.Length > 1)
            {
                if (args[1].Equals("-autorun"))
                {
                    // Spostare il programma in background in fase di avvio automatico se necessario.
                    bool shouldWindowBeMinimized = Core.Preferences.GetPreference<bool>("settings", "startMinimized");
                    if (shouldWindowBeMinimized) {
                        this.ShowInTaskbar = false;
                        this.Hide();
                    }

                    Task.Delay(5000).ContinueWith(task => {
                        // Cerca di eseguire il collegamento con la schedina.
                        int attempts = 0;
                        int maxAttempts = Core.Preferences.GetPreference<int>("settings", "startAttempts");
                        while (attempts < maxAttempts && serialStatus == SerialStatus.DISCONNECTED && isConnectionAllowed) {
                            Btn_Connect_Click(null, null);
                            Task.Delay(2500);
                            attempts += 1;
                        }

                        // Se è stato eseguito il collegamento con la schedina, selezionare
                        // l'effetto di avvio da eseguire.
                        if (serialStatus == SerialStatus.CONNECTED) {
                
                            int effectId = Core.Preferences.GetPreference<int>("settings", "startEffect");
                            Application.Current.Dispatcher.Invoke(new Action(() => { 
                                OnEffectSelected(effectId);
                            }));
                        }
                    });
                }
            }
            // Mostra la pagina di configurazione.
            MoveTo(0);
        }
        #endregion

        #region Tasks
        /// <summary>
        /// Ripristinare le preferenze dell'utente nella pagina di configurazione e 
        /// delle impostazioni dell'applicazione.
        /// </summary>
        private void LoadPreferences()
        {
            isUserInputAllowed = false;

            // Carica la configurazione della scheda.
            TBox_Name.Text = Core.Preferences.GetPreference<string>("config", "name");
            CBox_Board.SelectedIndex = Core.Preferences.GetPreference<int>("config", "board");
            NBox_UsbBaud.Value = Core.Preferences.GetPreference<int>("config", "usbBaud");
            CBox_UsbPort.ItemsSource = Core.Utils.GetAvaiblePorts();
            string defPort = Core.Preferences.GetPreference<string>("config", "usbPort");
            CBox_UsbPort.SelectedIndex = Core.Utils.IsAvaiblePort(defPort) ? Core.Utils.GetPortIndex(defPort) : -1;
            TBox_LanIpAddress.Text = Core.Preferences.GetPreference<string>("config", "lanIpAddress");
            NBox_LanPort.Value = Core.Preferences.GetPreference<int>("config", "lanPort");
            NBox_LightsCount.Value = Core.Preferences.GetPreference<int>("config", "lightsCount");

            // Carica lo stato delle impostazioni del programma.
            CBox_MinimizedToTry.IsChecked = Core.Preferences.GetPreference<bool>("settings", "minimizedToTry");
            CBox_StartAtStartup.IsChecked = Core.Preferences.GetPreference<bool>("settings", "startAtStartup");
            CBox_StartMinimized.IsChecked = Core.Preferences.GetPreference<bool>("settings", "startMinimized");
            CBox_Scaling.SelectedIndex = Core.Preferences.GetPreference<int>("settings", "scaling");
            NBox_StartAttempts.Value = Core.Preferences.GetPreference<int>("settings", "startAttempts");
            Item_pStartEffect.IsEnabled = CBox_StartAtStartup.IsChecked == true ? true : false;
            Item_pStartEffect.Opacity = Item_pStartEffect.IsEnabled ? 1 : 0.6;
            Item_pStartTimeout.IsEnabled = CBox_StartAtStartup.IsChecked == true ? true : false;
            Item_pStartTimeout.Opacity = Item_pStartTimeout.IsEnabled ? 1 : 0.6;
            Item_pStartMinimized.IsEnabled = CBox_StartAtStartup.IsChecked == true ? true : false;
            Item_pStartMinimized.Opacity = Item_pStartMinimized.IsEnabled ? 1 : 0.6;

            // Carica la lista degli effetti di avvio.
            EffectModel[] effects = App.Current.FindResource("effects") as EffectModel[];
            foreach (EffectModel effect in effects) {
                CBox_StartEffect.Items.Add(effect.Name);
            }
            CBox_StartEffect.SelectedIndex = Core.Preferences.GetPreference<int>("settings", "startEffect");

            // Carica le versione dell'applicazione.
            string[] version = Assembly.GetExecutingAssembly().GetName().Version.ToString().Split('.');
            Tb_AppVersion.Text = $"{version[0]}.{version[1]}";

            // Se è la prima volta che il programma viene aperto, impostare una valore di scaling.
            if (CBox_Scaling.SelectedIndex == -1) {
                CBox_Scaling.SelectedIndex = Core.Utils.GetDefaultScale();
                Core.Preferences.SetPreference<int>("settings", "scaling", CBox_Scaling.SelectedIndex);
            }

            isUserInputAllowed = true;
        }
        /// <summary>
        /// Cambia la pagina visualizzata dall'utente e ne carica eventuali impostazioni
        /// prima che avvenga effettivamente il cambio.
        /// </summary>
        private void MoveTo(int index)
        {
            // Annulla l'azione se la pagine indicata è già visibile.
            if (index == pageIndex)
                return;

            // Imposta tutte le pagine su uno stato di non visibilità tranne la pagina richiesta.
            var items = new Grid[] { Tab_Configuration, Tab_Effect, Tab_Settings };
            for (int i = 0; i < 3; i++) {
                items[i].Visibility = i == index ? Visibility.Visible : Visibility.Collapsed;
            }

            // Salva la pagina indicata.
            pageIndex = index;
        }
        /// <summary>
        /// Cambia lo stato del menu laterale di navigazione, e ne varia il contenitore.
        /// </summary>
        private void MenuTo(int index)
        {
            switch (index)
            {
                case 0:
                    if (sizeStatus == 0)
                    {
                        Tab_Main.ColumnDefinitions[0].Width = new GridLength(100, GridUnitType.Pixel);
                        Nav_Large.Visibility = Visibility.Collapsed;
                        Nav_Small.Visibility = Visibility.Visible;

                        var childrenList = Nav_Large.Children.Cast<UIElement>().ToArray();
                        Tab_Nav_Container.Children.Clear();
                        foreach (var c in childrenList)
                        {
                            Nav_Large.Children.Remove(c);
                            Tab_Nav_Container.Children.Add(c);
                        }

                        sizeStatus = 1;
                    }
                    break;
                case 1:
                    if (sizeStatus == 1)
                    {
                        Tab_Main.ColumnDefinitions[0].Width = new GridLength(390, GridUnitType.Pixel);
                        Nav_Small.Visibility = Visibility.Collapsed;
                        Nav_Large.Visibility = Visibility.Visible;

                        if (isNavbarOpen)
                        {
                            Tab_Nav_Container.Visibility = Visibility.Collapsed;
                            Tab_Overlay.Visibility = Visibility.Collapsed;
                        }

                        var childrenList = Tab_Nav_Container.Children.Cast<UIElement>().ToArray();
                        Nav_Large.Children.Clear();
                        foreach (var c in childrenList)
                        {
                            Tab_Nav_Container.Children.Remove(c);
                            Nav_Large.Children.Add(c);
                        }

                        sizeStatus = 0;
                    }
                    break;

            }
        }
        /// <summary>
        /// Mostra una notifica di sistema con il contenuto riferito all'indice fornito.
        /// </summary>
        private void ShowAlert(int index)
        {
            switch(index)
            {
                // External
                case 0:
                    TBar_Icon.ShowBalloonTip((string)FindResource("alertInternalErrorTitle"), 
                        (string)FindResource("alertInternalErrorDescription"), Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
                    break;
                case 1:
                    TBar_Icon.ShowBalloonTip((string)FindResource("alertConnectionErrorTitle"), 
                        String.Format((string)FindResource("alertConnectionErrorDescription"), CBox_UsbPort.SelectedValue), Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
                    break;
                // Internal
                case 2:
                    Tab_Alert.Visibility = Visibility.Visible;
                    Tb_dAlertTitle.Text = (string)FindResource("alertResetConfirmTitle");
                    Tb_dAlertDescription.Text = (string)FindResource("alertResetConfirmDescription");
                    messageCallback = new Action<int>((res) => {
                        if (res == 1) {
                            Preferences.ResetPreferences();
                            Process.Start(Application.ResourceAssembly.Location);
                            Application.Current.Shutdown();
                        }
                    });
                    break;
            }
        }
        #endregion

        #region Serial Callback
        public void OnBoardConnect()
        {
            // Imposta uno stato di connessione
            serialStatus = SerialStatus.CONNECTED;

            // Aggiorna l'interfaccia utente
            Tx_hStatus.Text = FindResource("menuConnected") as string;
            Tx_hStatus.Foreground = Core.Utils.GetBrushFromHex("#6A9D69");
            Btn_Connect.Content = FindResource("configurationDisconnectButton") as string;
            Btn_Connect.Background = Core.Utils.GetBrushFromHex("#9D6969");
            Btn_Reset.IsEnabled = false;
            Tab_ConfigurationContent.IsEnabled = false;

            // Rendi accessibile la lista degli effetti
            Tab_Effects.IsEnabled = true;
        }
        public void OnBoardDisconnect()
        {
            // Imposta uno stato di disconnessione
            serialStatus = SerialStatus.DISCONNECTED;

            // Aggiorna l'interfaccia utente
            Tx_hStatus.Text = FindResource("menuDisconnected") as string;
            Tx_hStatus.Foreground = Core.Utils.GetBrushFromHex("#9D6969");
            Btn_Connect.Content = FindResource("configurationConnectButton") as string;
            Btn_Connect.Background = Core.Utils.GetBrushFromHex("#6A9D69");
            Btn_Reset.IsEnabled = true;
            Tab_ConfigurationContent.IsEnabled = true;

            // Rendi inacessibile la lista degli effetti
            Tab_Effects.IsEnabled = false;
        }
        public void OnBoardError()
        {
            // Imposta uno stato di disconnessione
            serialStatus = SerialStatus.DISCONNECTED;

            // Aggiorna l'interfaccia utente
            Tx_hStatus.Text = FindResource("menuDisconnected") as string;
            Tx_hStatus.Foreground = Core.Utils.GetBrushFromHex("#9D6969");
            Btn_Connect.Content = FindResource("configurationConnectButton") as string;
            Btn_Connect.Background = Core.Utils.GetBrushFromHex("#6A9D69");
            Btn_Reset.IsEnabled = true;
            Tab_ConfigurationContent.IsEnabled = true;

            // Arresta l'effetto in esecuzione
            OnEffectSelected(-1);

            // Rendi inacessibile la lista degli effetti
            Tab_Effects.IsEnabled = false;

            // Mostra una notifica di errore
            ShowAlert(1);
        }
        #endregion

        #region Effect Callback
        public void OnEffectData(int type, int index, byte code, byte alpha, byte red, byte green, byte blue)
        {
            //Verifica se la scheda è collegata
            if (serialStatus == SerialStatus.CONNECTED)
            {
                // Invia alla scheda i valori dei pin da utilizzare
                serialHandler.Write(type, index, code, alpha, red, green, blue);
            }
        }
        public void OnEffectError()
        {
            this.Dispatcher.Invoke(new Action(delegate {
                // Imposta uno stato di disconnessione
                serialStatus = SerialStatus.DISCONNECTED;

                // Arresta l'effetto in esecuzione
                OnEffectSelected(-1);

                // Mostra una notifica di errore
                ShowAlert(0);
            }));
        }
        public void OnColorPicker(int type, Tint tint)
        {
            //Aggiorna l'interfaccia utente
            var colorPicker = new ColorPickerControl(this, type, tint);
            Content_ColorPicker.Content = colorPicker;
            Tab_GradientColorPicker.Visibility = Visibility.Visible;
        }
        public void OnConfirmRequest(Action<int> callback)
        {
            ShowAlert(3);
            messageCallback = callback;
        }
        /// <summary>
        /// Avvia il timer che gestisce il loop dell'effetto corrente.
        /// </summary>
        public void StartTimer()
        {
            timerHandler.Start();
        }
        /// <summary>
        /// Arresta il timer che gestisce il loop dell'effetto corrente.
        /// </summary>
        public void StopTimer()
        {
            timerHandler.Stop();
        }
        /// <summary>
        /// Imposta un nuovo intervallo di tempo per il timer .
        /// </summary>
        public void SetInterval(int interval)
        {
            timerHandler.SetInterval(interval);
        }
        #endregion

        #region Window Callback
        /// <summary>
        /// Gestisce tutte le chiamate di tipo 'SizeChanged' da parte della finestra principale.
        /// </summary>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Aggiorna lo stato del menu
            MenuTo(this.ActualWidth <= 1300 ? 0 : 1);
        }
        /// <summary>
        /// Gestisce tutte le chiamate di tipo 'Closing' da parte della finestra principale.
        /// </summary>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (isCloseForced) {
                e.Cancel = false;
                return;
            }

            // Verifica l'azione da eseguire alla pressione del pulsante di chiusura
            if (Core.Preferences.GetPreference<bool>("settings", "minimizedToTry"))
            {
                // Cancella la chiusura del programma
                e.Cancel = true;

                // Sposta il programma in background
                this.ShowInTaskbar = false;
                this.Hide();
            }
            else
            {
                // Cancella la chiusura del programma
                e.Cancel = true;

                if (effectIndex != -1 && timerHandler != null) {
                    timerHandler.Stop();
                }

                // Continua la chiusura del programma
                e.Cancel = false;
            }

            // Salva le dimensioni della finestra.
            Core.Preferences.SetPreference<double>("settings", "width", this.ActualWidth);
            Core.Preferences.SetPreference<double>("settings", "height", this.ActualHeight);
        }
        #endregion

        #region Binding Callback
        /// <summary>
        /// Riceve la chiamata quando un elemento della lista degli effetti viene selezionato
        /// ed effettua a sua volta un'altra chiamata alla funzione che si occupa di attivare
        /// gli effetti, con uno specifico id.
        /// </summary>
        public ICommand EffectCommand => new RelayCommand(id => { OnEffectSelected((int)id); });
        /// <summary>
        /// Cambia l'effetto in esecuzione e aggiorna la corrispondente interfaccia.
        /// </summary>
        public void OnEffectSelected(int id)
        {
            // Mostra la pagina dell'effetto se nascosta oppure mostra la pagina
            // di configurazione se l'id richiesto non corrisponde a nessun effetto
            if (pageIndex != 1) 
                MoveTo(id != -1 ? 1 : 0);

            // Annulla l'azione se l'effetto richiesto è già in esecuzione
            if (effectIndex == id) 
                return;

            // Arresta il timer di gestione
            if (timerHandler != null) 
                timerHandler.Stop();

            // Aggiorna l'interfaccia utente
            if (effectIndex != -1) {
                (List_Effects.Items[effectIndex] as EffectModel).IsActived = false;
                effectInterface.OnAction(null);
                Core.Utils.SetVisibility(Tab_GradientColorPicker, false);
            }

            // Verifica se è l'id non corrisponde a nessun effetto
            if (id == -1) {
                effectInterface = null;
                effectControl = null;
                timerHandler = null;
                effectIndex = -1;
                return;
            };

            // Inizializza un timer che gestira alcuni effetti
            timerHandler = new TimerHandler();

            // Cerca la corrispondeza della classe dell'effetto in base al suo identificativo
            switch (id)
            {
                case 0: 
                    effectControl = new Effects.StaticControl(this);
                    break;
                case 1: // Breathing
                    effectControl = new Effects.BreathingControl(this);
                    break;
                case 2: // Strobing
                    effectControl = new Effects.StrobingControl(this);
                    break;
                case 3: // Gradient
                    effectControl = new Effects.GradientControl(this);
                    break;
                case 4: // Spectrum
                    effectControl = new Effects.SpectrumControl(this);
                    break;
                case 5: // Screen
                    effectControl = new Effects.ScreenControl(this);
                    break;
                case 6: // Hardware
                    effectControl = new Effects.HardwareControl(this);
                    break;
                case 7: // Daylight
                    effectControl = new Effects.DaylightControl(this);
                    break;
                case 8: // Weather
                    effectControl = new Effects.WeatherControl(this);
                    break;
            }

            // Imposta l'interfaccia di gestione dell'effetto 
            effectInterface = effectControl as IEffect;

            // Applica l'interfaccia utente dell'effetto 
            Tmp_Effect.Content = effectControl;

            // Fornisce al timer le attività che dovrà eseguire
            timerHandler.Create(effectInterface.OnCreate, effectInterface.OnUpdate, effectInterface.OnClosed);

            // Aggiorna l'interfaccia utente
            Tb_EffectName.Text = (FindResource("effects") as EffectModel[])[id].Name;
            Tb_EffectDetails.Text = (FindResource("effects") as EffectModel[])[id].Description;
            (List_Effects.Items[id] as EffectModel).IsActived = true;

            // Aggiorna i dettagli dello sponsor
            Btn_EffectService.ToolTip = (List_Effects.Items[id] as EffectModel).SponsorToolTip;
            if ((List_Effects.Items[id] as EffectModel).SponsorImage != null) {
                Btn_EffectService.Visibility = Visibility.Visible;
                Img_EffectService.Source = new BitmapImage(new Uri((List_Effects.Items[id] as EffectModel).SponsorImage));
            }
            else {
                Btn_EffectService.Visibility = Visibility.Collapsed;
                Img_EffectService.Source = null;
            }

            // Imposta l'identificativo dell'effetto come corrente
            effectIndex = id;
        }
        #endregion

        #region Window Controls
        private void Btn_Connect_Click(object sender, RoutedEventArgs e)
        {
            switch (serialStatus)
            {
                case SerialStatus.CONNECTED:
                    // Arresta l'effetto in esecuzione.
                    if (effectIndex != -1) {
                        OnEffectSelected(-1);
                    }
                    // Chiude la connessione attraverso la porta.
                    serialHandler.Disconnect();
                    break;
                case SerialStatus.DISCONNECTED:
                    // Avvia una connessione con la scheda.
                    if (connectionType == 0) {
                        if (!String.IsNullOrEmpty(usbPort)) {
                            serialHandler.Connect(connectionType, usbPort, usbBaud);
                        }
                    } else if (connectionType == 1) {
                        if (!String.IsNullOrEmpty(lanIpAddress)) {
                            serialHandler.Connect(connectionType, lanIpAddress, lanPort);
                        }
                    }
                    break;
            }
        }
        private void Btn_Reset_Click(object sender, RoutedEventArgs e)
        {
            ShowAlert(2);
        }
        private void Btn_ToConfiguration_Click(object sender, RoutedEventArgs e)
        {
            // Mostra la pagina di configurazione
            MoveTo(0);
        }
        private void Btn_ToSettings_Click(object sender, RoutedEventArgs e)
        {
            // Mostra la pagina delle impostazioni
            MoveTo(2);
        }
        private void Btn_Menu_Click(object sender, RoutedEventArgs e)
        {
            Tab_Nav_Container.Visibility = Tab_Nav_Container.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            Tab_Overlay.Visibility = Tab_Nav_Container.Visibility == Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;
            isNavbarOpen = Tab_Overlay.Visibility == Visibility.Visible ? true : false;
        }
        private void Btn_ColorPickerCancel_Click(object sender, RoutedEventArgs e)
        {
            // Chiude l'editor del colore
            effectInterface.OnAction(null);
            Core.Utils.SetVisibility(Tab_GradientColorPicker, false);
        }
        private void Btn_ColorPickerSelect_Click(object sender, RoutedEventArgs e)
        {
            // Chiude l'editor del colore
            effectInterface.OnAction((Content_ColorPicker.Content as ColorPickerControl).GetTint());
            Core.Utils.SetVisibility(Tab_GradientColorPicker, false);
        }
        private void Btn_About_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://mariusbinary.altervista.org/documentation.php?id=1");
        }
        private void Btn_AlertCancel_Click(object sender, RoutedEventArgs e)
        {
            messageCallback(0);
            Tab_Alert.Visibility = Visibility.Collapsed;
        }
        private void Btn_AlertOk_Click(object sender, RoutedEventArgs e)
        {
            messageCallback(1);
            Tab_Alert.Visibility = Visibility.Collapsed;
        }
        private void CBox_Board_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            boardIndex = CBox_Board.SelectedIndex;
            connectionType = boardIndex;

            if (boardIndex == 0) {
                Item_BoardLanIpAddress.Visibility = Visibility.Collapsed;
                Item_BoardLanPort.Visibility = Visibility.Collapsed;
                Item_BoardUsbBaud.Visibility = Visibility.Visible;
                Item_BoardUsbPort.Visibility = Visibility.Visible;
                Btn_Connect.IsEnabled = !String.IsNullOrEmpty(usbPort);
                isConnectionAllowed = !String.IsNullOrEmpty(usbPort);
            } else if (boardIndex == 1) {
                Item_BoardUsbBaud.Visibility = Visibility.Collapsed;
                Item_BoardUsbPort.Visibility = Visibility.Collapsed;
                Item_BoardLanIpAddress.Visibility = Visibility.Visible;
                Item_BoardLanPort.Visibility = Visibility.Visible;
                Btn_Connect.IsEnabled = !String.IsNullOrEmpty(lanIpAddress);
                isConnectionAllowed = !String.IsNullOrEmpty(lanIpAddress);
            }

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<int>("config", "board", boardIndex);
            }
        }
        private void CBox_UsbPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            usbPort = CBox_UsbPort.SelectedValue.ToString();
            if (connectionType == 0) {
                Btn_Connect.IsEnabled = !String.IsNullOrEmpty(usbPort);
                isConnectionAllowed = !String.IsNullOrEmpty(usbPort);
            }

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<string>("config", "usbPort", usbPort);
            }
        }
        private void CBox_StartAtStartup_Click(object sender, RoutedEventArgs e)
        {
            // Verifica se la preferenza è stata abilitata
            if (CBox_StartAtStartup.IsChecked == true)
            {
                // Aggiunge il programma per l'avvio con il sistema operativo
                string executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                Core.Utils.AddToStartup("LEDSystem", executablePath, "-autorun");
            }
            else
            {
                // Rimuove il programma dall'avvio con il sistema operativo
                Core.Utils.RemoveFromStartup("LEDSystem");
            }

            // Carica in memoria la preferenza dell'utente
            Core.Preferences.SetPreference<bool>("settings", "startAtStartup", CBox_StartAtStartup.IsChecked == true);

            // Aggiorna lo stato delle preferenze dipendenti
            Item_pStartEffect.IsEnabled = CBox_StartAtStartup.IsChecked == true ? true : false;
            Item_pStartEffect.Opacity = Item_pStartEffect.IsEnabled ? 1 : 0.6;
            Item_pStartTimeout.IsEnabled = CBox_StartAtStartup.IsChecked == true ? true : false;
            Item_pStartTimeout.Opacity = Item_pStartTimeout.IsEnabled ? 1 : 0.6;
            Item_pStartMinimized.IsEnabled = CBox_StartAtStartup.IsChecked == true ? true : false;
            Item_pStartMinimized.Opacity = Item_pStartMinimized.IsEnabled ? 1 : 0.6;
        }
        private void CBox_StartEffect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Carica in memoria la preferenza dell'utente
            Core.Preferences.SetPreference<int>("settings", "startEffect", CBox_StartEffect.SelectedIndex);
        }
        private void CBox_StartMinimized_Click(object sender, RoutedEventArgs e)
        {
            // Carica in memoria la preferenza dell'utente
            Core.Preferences.SetPreference<bool>("settings", "startMinimized", CBox_StartMinimized.IsChecked == true);
        }
        private void CBox_MinimizedToTry_Click(object sender, RoutedEventArgs e)
        {
            // Carica in memoria la preferenza dell'utente
            Core.Preferences.SetPreference<bool>("settings", "minimizedToTry", CBox_MinimizedToTry.IsChecked == true);
        }
        private void CBox_Scaling_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUserInputAllowed) {
                var scaler = Tab_Adjuster.LayoutTransform as ScaleTransform;

                DoubleAnimation animator = new DoubleAnimation() {
                    Duration = new Duration(TimeSpan.FromMilliseconds(250)),
                };

                animator.To = Core.Utils.Map(Math.Round((double)CBox_Scaling.SelectedIndex, 2), 0, 4, 0.8, 1.2);

                scaler.BeginAnimation(ScaleTransform.ScaleXProperty, animator);
                scaler.BeginAnimation(ScaleTransform.ScaleYProperty, animator);

                Core.Preferences.SetPreference<int>("settings", "scaling", CBox_Scaling.SelectedIndex);
            }
        }
        private void NBox_StartAttempts_ValueChanged(object sender, RoutedEventArgs e)
        {
            // Carica in memoria la preferenza dell'utente
            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<int>("settings", "startAttempts", NBox_StartAttempts.Value);
            }
        }
        private void NBox_UsbBaud_ValueChanged(object sender, RoutedEventArgs e)
        {
            usbBaud = NBox_UsbBaud.Value;

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<int>("config", "usbBaud", usbBaud);
            }
        }
        private void NBox_LightsCount_ValueChanged(object sender, RoutedEventArgs e)
        {
            lightsCount = NBox_LightsCount.Value;
            serialHandler.SetLights(lightsCount);

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<int>("config", "lightsCount", lightsCount);
            }
        }
        private void NBox_LanPort_ValueChanged(object sender, RoutedEventArgs e)
        {
            lanPort = NBox_LanPort.Value;

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<int>("config", "lanPort", lanPort);
            }
        }
        private void TBox_Name_TextChanged(object sender, TextChangedEventArgs e)
        {
            Tx_hName.Text = TBox_Name.Text;

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<string>("config", "name", TBox_Name.Text);
            }
        }
        private void TBox_LanIpAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            lanIpAddress = TBox_LanIpAddress.Text;
            if (connectionType == 1) {
                Btn_Connect.IsEnabled = !String.IsNullOrEmpty(lanIpAddress);
                isConnectionAllowed = !String.IsNullOrEmpty(lanIpAddress);
            }

            if (isUserInputAllowed) {
                Core.Preferences.SetPreference<string>("config", "lanIpAddress", lanIpAddress);
            }
        }
        #endregion

        #region Taskbar Controls
        private void TBar_Icon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            this.ShowInTaskbar = true;
            this.Show();
        }
        private void Menu_Open_Click(object sender, RoutedEventArgs e)
        {
            this.ShowInTaskbar = true;
            this.Show();
        }
        private void Menu_Dashboard_Click(object sender, RoutedEventArgs e)
        {
            Menu_Open_Click(null, null);
            MoveTo(0);
        }
        private void Menu_Effects_Click(object sender, RoutedEventArgs e)
        {
            Menu_Open_Click(null, null);

            if (effectIndex == -1) {
                MoveTo(0);
            }else {
                MoveTo(1);
            }
        }
        private void Menu_Settings_Click(object sender, RoutedEventArgs e)
        {
            Menu_Open_Click(null, null);
            MoveTo(2);
        }
        private void Menu_Ouit_Click(object sender, RoutedEventArgs e)
        {
            isCloseForced = true;
            this.Close();
        }
        #endregion
    }
}