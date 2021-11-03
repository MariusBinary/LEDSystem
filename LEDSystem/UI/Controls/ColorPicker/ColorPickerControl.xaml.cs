using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using LEDSystem.UI.Helpers;

namespace LEDSystem.UI.Controls.ColorPicker
{
    public partial class ColorPickerControl : UserControl
    {
        #region Variables
        private Tint selectedTint;
        private Tint floatingTint;
        private Tint unloadedTint;
        private bool isPointDragAllowed = false;
        private bool isUserInputAllowed = false;
        private GradientStopCollection stopsCollection;
        private List<GradientPointControl> pointsList;
        private ObservableCollection<GradientStopModel> stopsList;
        private int pointsLimit = 25;
        private int pointsCount = 0;
        private int pointsIndex = -1;
        private int colorPickerType = 0;
        private int colorPickerTab = 0;
        private double controlWidth = 0;
        #endregion

        #region Commands
        public ICommand SelectPointCommand => new RelayCommand(id => {
            SelectPoint((int)id);
        });
        public ICommand RemovePointCommand => new RelayCommand(id => {
            RemovePoint((int)id);
        });
        public void EditPointCommand(int index, double offset)
        {
            Keyboard.ClearFocus();
            EditPoint(index, offset, false);
            SortPoints();
        }
        #endregion

        #region Main
        /// <summary>
        /// Punto di ingresso del controllo.
        /// </summary>
        public ColorPickerControl(Window context, int mode, Tint tint)
        {
            // Copia i parametri all'interno della classe.
            colorPickerType = mode;
            floatingTint = new Tint();
            unloadedTint = tint;

            // Inizializza l'interfaccia grafica.
            InitializeComponent();
            context.KeyDown += UserControl_KeyDown;

            // Imposta il tipo di layout in base al colore da gestire.
            if (mode == 0)
            {
                ColumnDefinition hiddenColumn1 = new ColumnDefinition();
                hiddenColumn1.Width = new GridLength(0, GridUnitType.Pixel);
                Tab_Main.ColumnDefinitions[1] = hiddenColumn1;
                ColumnDefinition hiddenColumn2 = new ColumnDefinition();
                hiddenColumn2.Width = new GridLength(0, GridUnitType.Pixel);
                Tab_Main.ColumnDefinitions[2] = hiddenColumn2;
                RowDefinition hiddenRow1 = new RowDefinition();
                hiddenRow1.Height = new GridLength(0, GridUnitType.Pixel);
                Tab_Main.RowDefinitions[0] = hiddenRow1;
                RowDefinition hiddenRow2 = new RowDefinition();
                hiddenRow2.Height = new GridLength(0, GridUnitType.Pixel);
                Tab_Main.RowDefinitions[1] = hiddenRow2;
            }
        }
        /// <summary>
        /// Carica il colore.
        /// </summary>
        private void LoadColor(Tint tint, int mode)
        {
            if (tint != null)
            {
                Tint clonedTint = tint.Clone();
                switch (mode)
                {
                    case 0:
                        selectedTint = clonedTint;
                        break;
                    case 1:
                        selectedTint = new Tint();
                        selectedTint.Type = mode;
                        selectedTint.Points = new List<Tint>();
                        LoadPoints(clonedTint.Points);
                        break;
                }
            }
            else
            {
                switch (mode)
                {
                    case 0:
                        selectedTint = new Tint();
                        selectedTint.Type = mode;
                        break;
                    case 1:
                        selectedTint = new Tint();
                        selectedTint.Type = mode;
                        selectedTint.Points = new List<Tint>();
                        break;
                }
            }
        }
        /// <summary>
        /// Cambia l'editor di colore tra RGB, HSV e HEX.
        /// </summary>
        private void ChangeTab(int tab)
        {
            // Rendi invisibili tutte le tabelle.
            Tab_RGB.Visibility = Visibility.Collapsed;
            Tab_HSV.Visibility = Visibility.Collapsed;
            Tab_HEX.Visibility = Visibility.Collapsed;

            // Aggiorna i controlli utente.
            UpdateUiControls(tab);

            // Rendi visible solo la tabella richiesta.
            switch (tab)
            {
                // Editor RGB
                case 0:
                    Tab_RGB.Visibility = Visibility.Visible;
                    break;
                // Editor HSV
                case 1:
                    Tab_HSV.Visibility = Visibility.Visible;
                    break;
                // Editor HEX
                case 2:
                    Tab_HEX.Visibility = Visibility.Visible;
                    break;
            }

            colorPickerTab = tab;
        }
        /// <summary>
        /// Aggiorna il colore selezionato.
        /// </summary>
        private void RefreshTint(bool forceUiRefresh = false)
        {
            if (colorPickerType == 0)
            {
                Ctrl_SelectedColor.Fill = selectedTint.GetBrush();
                if (colorPickerTab == 1 && forceUiRefresh == false)
                {
                    Color hueColor = selectedTint.GetRGBFromHue();
                    ((LinearGradientBrush)Seek_Saturation.Background).GradientStops[1].Color = hueColor;
                    ((LinearGradientBrush)Seek_Value.Background).GradientStops[1].Color = hueColor;
                }
            }
            else if (colorPickerType == 1)
            {
                if (pointsCount == 0 || pointsIndex == -1)
                {
                    Ctrl_SelectedColor.Fill = floatingTint.GetBrush();
                    if (colorPickerTab == 1 && forceUiRefresh == false)
                    {
                        Color hueColor = floatingTint.GetRGBFromHue();
                        ((LinearGradientBrush)Seek_Saturation.Background).GradientStops[1].Color = hueColor;
                        ((LinearGradientBrush)Seek_Value.Background).GradientStops[1].Color = hueColor;
                    }
                }
                else
                {
                    Ctrl_SelectedColor.Fill = selectedTint.Points[pointsIndex].GetBrush();
                    pointsList[pointsIndex].Background = selectedTint.Points[pointsIndex].GetBrush();
                    stopsCollection[pointsIndex].Color = selectedTint.Points[pointsIndex].GetColor();
                    stopsList[pointsIndex].ColorBackgroundBrush = selectedTint.Points[pointsIndex].GetBrush();
                    stopsList[pointsIndex].ColorBorderBrush = selectedTint.Points[pointsIndex].GetBrush(120);
                    if (colorPickerTab == 1 && forceUiRefresh == false)
                    {
                        Color hueColor = selectedTint.Points[pointsIndex].GetRGBFromHue();
                        ((LinearGradientBrush)Seek_Saturation.Background).GradientStops[1].Color = hueColor;
                        ((LinearGradientBrush)Seek_Value.Background).GradientStops[1].Color = hueColor;
                    }
                }
            }

            // Aggiorna l'interfaccia grafica se richiesto.
            if (forceUiRefresh)
            {
                UpdateUiControls(colorPickerTab);
            }
        }
        /// <summary>
        /// Ritorna il colore attivo sull'editor.
        /// </summary>
        private Tint GetEditableTint()
        {
            if (colorPickerType == 0)
            {
                return selectedTint;
            }
            else if (colorPickerType == 1)
            {
                if (pointsCount == 0 || pointsIndex == -1)
                {
                    return floatingTint;
                }
                else
                {
                    return selectedTint.Points[pointsIndex];
                }
            }

            return null;
        }
        /// <summary>
        /// Aggiorna il valore dei controlli grafici RGB, HSV o HEX.
        /// </summary>
        private void UpdateUiControls(int type)
        {
            isUserInputAllowed = false;

            switch (type)
            {
                case 0:
                    if (colorPickerType == 0)
                    {
                        Seek_Red.Value = selectedTint.Red;
                        Seek_Green.Value = selectedTint.Green;
                        Seek_Blue.Value = selectedTint.Blue;
                    }
                    else if (colorPickerType == 1)
                    {
                        if (pointsCount == 0 || pointsIndex == -1)
                        {
                            Seek_Red.Value = floatingTint.Red;
                            Seek_Green.Value = floatingTint.Green;
                            Seek_Blue.Value = floatingTint.Blue;
                        }
                        else
                        {
                            Seek_Red.Value = selectedTint.Points[pointsIndex].Red;
                            Seek_Green.Value = selectedTint.Points[pointsIndex].Green;
                            Seek_Blue.Value = selectedTint.Points[pointsIndex].Blue;
                        }
                    }
                    break;
                case 1:
                    if (colorPickerType == 0)
                    {
                        Seek_Hue.Value = selectedTint.Hue;
                        Seek_Saturation.Value = selectedTint.Saturation;
                        Seek_Value.Value = selectedTint.Value;
                        Color hueColor = selectedTint.GetRGBFromHue();
                        ((LinearGradientBrush)Seek_Saturation.Background).GradientStops[1].Color = hueColor;
                        ((LinearGradientBrush)Seek_Value.Background).GradientStops[1].Color = hueColor;
                    }
                    else if (colorPickerType == 1)
                    {
                        if (pointsCount == 0 || pointsIndex == -1)
                        {
                            Seek_Hue.Value = floatingTint.Hue;
                            Seek_Saturation.Value = floatingTint.Saturation;
                            Seek_Value.Value = floatingTint.Value;
                            Color hueColor = floatingTint.GetRGBFromHue();
                            ((LinearGradientBrush)Seek_Saturation.Background).GradientStops[1].Color = hueColor;
                            ((LinearGradientBrush)Seek_Value.Background).GradientStops[1].Color = hueColor;
                        }
                        else
                        {
                            Seek_Red.Value = selectedTint.Points[pointsIndex].Red;
                            Seek_Green.Value = selectedTint.Points[pointsIndex].Green;
                            Seek_Blue.Value = selectedTint.Points[pointsIndex].Blue;
                            Seek_Hue.Value = selectedTint.Points[pointsIndex].Hue;
                            Seek_Saturation.Value = selectedTint.Points[pointsIndex].Saturation;
                            Seek_Value.Value = selectedTint.Points[pointsIndex].Value;
                            Color hueColor = selectedTint.Points[pointsIndex].GetRGBFromHue();
                            ((LinearGradientBrush)Seek_Saturation.Background).GradientStops[1].Color = hueColor;
                            ((LinearGradientBrush)Seek_Value.Background).GradientStops[1].Color = hueColor;
                        }
                    }
                    break;
                case 2:
                    if (colorPickerType == 0)
                    {
                        TBox_HEX.Text = selectedTint.HEX;
                    }
                    else if (colorPickerType == 1)
                    {
                        if (pointsCount == 0 || pointsIndex == -1)
                        {
                            TBox_HEX.Text = floatingTint.HEX;
                        }
                        else
                        {
                            TBox_HEX.Text = selectedTint.Points[pointsIndex].HEX;
                        }
                    }
                    break;
            }

            isUserInputAllowed = true;
        }
        /// <summary>
        /// Ritorna il colore principale.
        /// </summary>
        public Tint GetTint()
        {
            return selectedTint;
        }
        #endregion

        #region Points Control
        /// <summary>
        /// Aggiunge un punto.
        /// </summary>
        private void AddPoint(double offset, Tint tint)
        {
            // Se il numero massimo di punti è stato raggiunto, annullare la funzione.
            if (pointsCount >= pointsLimit)
                return;

            // Calcola l'indice che il punto dovrà avere in base al suo offset e ai punti già presenti.
            // Incrementa eventuali punti di uno, se questi hanno un indice maggiore di quello calcolato.
            // Incrementa l'indice del punto selezionato se questo ha indice maggiore di quello calcolato.
            int index = -1;
            if (pointsCount > 0)
            {
                for (int i = 0; i < pointsCount; i++)
                {
                    if (pointsList[i].Offset >= offset && index == -1)
                    {
                        index = i;
                    }
                    if (index != -1)
                    {
                        pointsList[i].Index += 1;
                        stopsList[i].Index += 1;
                    }
                }
                if (index == -1)
                {
                    index = pointsCount;
                }
                if (pointsIndex >= index)
                {
                    pointsIndex += 1;
                }
            }
            else
            {
                index = 0;
            }

            // Crea un punto di colore.
            var gradientPoint = new GradientPointControl(tint.GetBrush());
            gradientPoint.MouseLeftButtonDown += Point_MouseLeftButtonDown;
            gradientPoint.Index = index;
            gradientPoint.Offset = offset;
            gradientPoint.IsActived = true;
            pointsList.Insert(index, gradientPoint);

            // Crea un elemento di colore.
            var gradientItem = new GradientStopModel();
            gradientItem.ColorBackgroundBrush = tint.GetBrush();
            gradientItem.ColorBorderBrush = tint.GetBrush(120);
            gradientItem.Index = index;
            gradientItem.IsUserAction = false;
            gradientItem.Callback += EditPointCommand;
            gradientItem.Offset = offset;
            gradientItem.IsActived = true;
            stopsList.Insert(index, gradientItem);

            // Crea uno stop di colore.
            var gradientStop = new GradientStop(tint.GetColor(), offset);
            stopsCollection.Insert(index, gradientStop);

            // Modifica la posizione del colore.
            selectedTint.Points.Insert(index, tint);
            selectedTint.Points[index].Offset = offset;

            // Aggiunge il punto di colore al contenitore.
            Canvas.SetLeft(gradientPoint, ((offset * controlWidth) / 1.0) - 5);
            GradientFrame.Children.Add(gradientPoint);

            // Rimuove l'avviso riguardante la mancanza di punti di colore.
            Tab_GradientEmpty.Visibility = Visibility.Collapsed;

            // Incrementa il numero totale dei punti.
            pointsCount += 1;

            // Imposta l'indice del punto appena creato come selezionato.
            SelectPoint(index);
        }
        /// <summary>
        /// Rimuove un punto.
        /// </summary>
        private void RemovePoint(int index)
        {
            // Se non sono presenti punti da rimuovere, annullare la funzione.
            if (pointsCount <= 0)
                return;

            // Se questo è l'ultimo punto presente, copiare il suo colore all'interno
            // del colore temporaneo.
            if (pointsCount - 1 <= 0)
            {
                floatingTint = selectedTint.Points[index].Clone();
            }

            // Rimuove il punto di colore.
            stopsCollection.RemoveAt(index);
            selectedTint.Points.RemoveAt(index);
            GradientFrame.Children.Remove(pointsList[index]);
            pointsList.RemoveAt(index);
            stopsList.RemoveAt(index);

            // Decrementa il numero totale dei punti.
            pointsCount -= 1;

            // Riordina l'indice degli elementi nelle liste.
            for (int i = index; i < pointsCount; i++)
            {
                pointsList[i].Index -= 1;
                stopsList[i].Index -= 1;
            }

            // Cerca un eventuale punto da selezionare.
            int nextIndex = -1;
            if (pointsCount > 0)
            {
                if (index == 0 && pointsIndex == 0)
                {
                    nextIndex = 0;
                    pointsIndex = -1;
                }
                else if (index == 0 && pointsIndex > 0)
                {
                    nextIndex = pointsIndex - 1;
                    pointsIndex -= 1;
                }
                else if (index == pointsCount && pointsIndex == index)
                {
                    nextIndex = pointsCount - 1;
                    pointsIndex = -1;
                }
                else if (index == pointsCount && pointsIndex < index)
                {
                    nextIndex = pointsIndex;
                    pointsIndex = -1;
                }
                else if (pointsIndex > index && pointsIndex == pointsCount)
                {
                    nextIndex = pointsCount - 1;
                    pointsIndex = -1;
                }
                else if (pointsIndex > index && pointsIndex < pointsCount)
                {
                    nextIndex = pointsIndex - 1;
                    pointsIndex = -1;
                }
                else if (pointsIndex <= index)
                {
                    nextIndex = pointsIndex;
                    pointsIndex = -1;
                }
            }
            else
            {
                pointsIndex = -1;
                Tab_GradientEmpty.Visibility = Visibility.Visible;
            }

            // Imposta l'indice del punto successivo a quello appena eliminato.
            SelectPoint(nextIndex);
        }
        /// <summary>
        /// Seleziona un punto.
        /// </summary>
        private void SelectPoint(int index)
        {
            // Deseleziona il vecchio punto se esistente.
            if (pointsIndex != -1)
            {
                pointsList[pointsIndex].IsActived = false;
                stopsList[pointsIndex].IsActived = false;
            }

            // Seleziona il nuovo punto se esistente.
            if (index != -1)
            {
                pointsList[index].IsActived = true;
                stopsList[index].IsActived = true;
            }

            // Imposta il nuovo punto come selezionato.
            pointsIndex = index;

            // Aggiorna i controlli grafici.
            RefreshTint(true);
        }
        /// <summary>
        /// Aggiorna la posizione di un punto.
        /// </summary>
        private void EditPoint(int index, double offset, bool isUserCommand = true)
        {
            pointsList[index].Offset = offset;
            if (isUserCommand) {
                stopsList[index].IsUserAction = false;
                stopsList[index].Offset = offset;
            }
            stopsCollection[index].Offset = offset;
            selectedTint.Points[index].Offset = offset;
            Canvas.SetLeft(pointsList[index], ((offset * controlWidth) / 1.0) - 5);
        }
        /// <summary>
        /// Carica un colore gradiente.
        /// </summary>
        private void LoadPoints(List<Tint> points)
        {
            // Se non sono presenti punti da aggiungere, annullare la funzione.
            if (points.Count <= 0)
                return;

            // Aggiunge i punti forniti singolarmente.
            for (int i = 0; i < points.Count; i++)
            {
                AddPoint(points[i].Offset, points[i]);
            }

            // Scorre verso il basso la lista degli stop fino all'ultimo elemento.
            int itemsCount = List_GradientStops.Items.Count;
            var lastItem = List_GradientStops.Items[itemsCount - 1];
            List_GradientStops.ScrollIntoView(lastItem);
        }
        /// <summary>
        /// Aggiorna la posizione dei punti.
        /// </summary>
        private void ResizePoints()
        {
            // Se non sono presenti punti da ridimensionare, annullare la funzione.
            if (pointsCount <= 0)
                return;

            GradientFrame.Children.Clear();
            foreach (GradientPointControl point in pointsList)
            {
                double realOffset = (point.Offset * controlWidth) / 1.0;
                Canvas.SetLeft(point, realOffset);
                GradientFrame.Children.Add(point);
            }
        }
        /// <summary>
        /// Ordina i punti in ordine crescente.
        /// </summary>
        private void SortPoints() 
        {
            // Se non sono presenti punti da ordinare, annullare la funzione.
            if (pointsList.Count <= 0)
                return;

            // Ordina la lista degli stop.
            int ptr = 0;
            List<GradientStopModel> sorted = stopsList.OrderBy(x => x).ToList();
            while (ptr < sorted.Count)
            {
                if (!stopsList[ptr].Equals(sorted[ptr]))
                {
                    GradientStopModel t = stopsList[ptr];
                    stopsList.RemoveAt(ptr);
                    stopsList.Insert(sorted.IndexOf(t), t);
                }
                else
                {
                    ptr++;
                }
            }

            // Ordina il resto delle liste.
            int oldIndex = pointsIndex;
            int pointsCount = pointsList.Count;
            for (int i = 0; i < pointsCount; i++)
            {
                for (int j = i + 1; j < pointsCount; j++)
                {
                    if (pointsList[i].Offset > pointsList[j].Offset)
                    {
                        var tempPoint = pointsList[i];
                        pointsList[i] = pointsList[j];
                        pointsList[j] = tempPoint;

                        var tempStop = stopsCollection[i];
                        stopsCollection[i] = stopsCollection[j];
                        stopsCollection[j] = tempStop;

                        var tempTint = selectedTint.Points[i];
                        selectedTint.Points[i] = selectedTint.Points[j];
                        selectedTint.Points[j] = tempTint;
                    }
                }
                if (pointsList[i].Index == oldIndex)
                {
                    pointsIndex = i;
                }
                pointsList[i].Index = i;
                stopsList[i].Index = i;
            }
        }
        #endregion

        #region Callback Events
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (colorPickerType == 1)
            {
                controlWidth = GradientFrame.ActualWidth;
                pointsList = new List<GradientPointControl>();
                stopsList = new ObservableCollection<GradientStopModel>();
                stopsCollection = new GradientStopCollection();
                var linearGradient = new LinearGradientBrush();
                linearGradient.StartPoint = new Point(0, 0);
                linearGradient.EndPoint = new Point(1, 0);
                linearGradient.GradientStops = stopsCollection;
                Tab_GradientPresenter.Background = linearGradient;
                List_GradientStops.ItemsSource = stopsList;
            }

            // Carica il colore fornito.
            LoadColor(unloadedTint, colorPickerType);

            // Aggiorna i controlli in base al colore selezionato.
            RefreshTint(true);
        }
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            controlWidth = GradientFrame.ActualWidth;
            ResizePoints();
        }
        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    RemovePoint(pointsIndex);
                    break;
            }
        }
        private void Point_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectPoint(((GradientPointControl)sender).Index);
            Mouse.Capture(GradientFrame);
            isPointDragAllowed = true;
        }
        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isPointDragAllowed)
            {
                isPointDragAllowed = false;
                Mouse.Capture(null);
                SortPoints();
            }
            else
            {
                double pointX = Mouse.GetPosition(GradientFrame).X;
                double offset = (pointX * 1.0) / controlWidth;

                if (pointsCount > 0)
                {
                    AddPoint(offset, selectedTint.GetPoint(offset));
                }
                else
                {
                    AddPoint(offset, GetEditableTint());
                }
            }
        }
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPointDragAllowed)
            {
                double pointX = Mouse.GetPosition(GradientFrame).X;
                double offset = (pointX * 1.0) / controlWidth;
                if (pointX >= 0 && pointX <= controlWidth)
                {
                    EditPoint(pointsIndex, offset);
                }
            }
        }
        private void Btn_RGB_Click(object sender, RoutedEventArgs e)
        {
            ChangeTab(0);
        }
        private void Btn_HSV_Click(object sender, RoutedEventArgs e)
        {
            ChangeTab(1);
        }
        private void Btn_HEX_Click(object sender, RoutedEventArgs e)
        {
            ChangeTab(2);
        }
        private void TBox_HEX_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!Regex.IsMatch(TBox_HEX.Text, @"^#([A-Fa-f0-9]{8}|[A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$") && TBox_HEX.Text.Length <= 9)
                return;

            if (isUserInputAllowed)
            {
                Tint tint = GetEditableTint();
                tint.HEX = TBox_HEX.Text;
                RefreshTint();
            }
        }
        private void Seek_Red_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUserInputAllowed)
            {
                Tint tint = GetEditableTint();
                tint.Red = (byte)Seek_Red.Value;
                RefreshTint();
            }
        }
        private void Seek_Green_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUserInputAllowed)
            {
                Tint tint = GetEditableTint();
                tint.Green = (byte)Seek_Green.Value;
                RefreshTint();
            }
        }
        private void Seek_Blue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUserInputAllowed)
            {
                Tint tint = GetEditableTint();
                tint.Blue = (byte)Seek_Blue.Value;
                RefreshTint();
            }
        }
        private void Seek_Hue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUserInputAllowed)
            {
                Tint tint = GetEditableTint();
                tint.Hue = (double)Seek_Hue.Value;
                RefreshTint();
            }
        }
        private void Seek_Saturation_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUserInputAllowed)
            {
                Tint tint = GetEditableTint();
                tint.Saturation = (byte)Seek_Saturation.Value;
                RefreshTint();
            }
        }
        private void Seek_Value_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUserInputAllowed)
            {
                Tint tint = GetEditableTint();
                tint.Value = (byte)Seek_Value.Value;
                RefreshTint();
            }
        }
        private void ColorFragment_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isUserInputAllowed)
            {
                Tint tint = GetEditableTint();
                tint.HEX = (sender as Path).Fill.ToString().Replace("#FF", "#");
                RefreshTint(true);
            }
        }
        #endregion
    }
}
