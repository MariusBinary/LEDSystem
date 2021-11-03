using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace LEDSystem.UI.Controls
{
    public class NumericTextBox : TextBox
    {
        #region Variables

        private bool isUserInput = false;

        #endregion

        #region Properties

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            "Maximum", typeof(double), typeof(NumericTextBox), new PropertyMetadata(100.0, null));

        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
          "Minimum", typeof(double), typeof(NumericTextBox), new PropertyMetadata(0.0, null));

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(double), typeof(NumericTextBox), new PropertyMetadata(-1.0,
                new PropertyChangedCallback(ValuePropertyChanged)));

        private static void ValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!((NumericTextBox)d).isUserInput)
            {
                ((NumericTextBox)d).Text = ((NumericTextBox)d).Format(e.NewValue.ToString());
            }
        }

        #endregion

        #region Callbacks

        private static readonly RoutedEvent ValueChangedEvent =
            EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(NumericTextBox));

        public event RoutedEventHandler ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        public bool IsRealNumbersAllowed
        {
            get { return (bool)GetValue(IsRealNumbersAllowedProperty); }
            set { SetValue(IsRealNumbersAllowedProperty, value); }
        }

        public static readonly DependencyProperty IsRealNumbersAllowedProperty = DependencyProperty.Register(
          "IsRealNumbersAllowed", typeof(bool), typeof(NumericTextBox), new PropertyMetadata(false, null));

        #endregion

        #region Functions

        private string Format(string unformatedString)
        {
            unformatedString = unformatedString.Replace('.', ',');
            unformatedString = Math.Round(((double.Parse(unformatedString) * 100) / 1.0), 1).ToString();
            unformatedString = unformatedString.Replace(',', '.');
            return unformatedString;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.PreviewKeyDown += KeyPressed;
            this.PreviewTextInput += TextValidation;
            this.TextChanged += TextUpdated;
        }

        private void TextUpdated(object sender, TextChangedEventArgs e)
        {
            isUserInput = true;

            if (Text != String.Empty && !Text.EndsWith("."))
            {
                string unformatedString = Text;
                unformatedString = unformatedString.Replace('.', ',');
                double value = double.Parse(unformatedString);
                if (value < Minimum)
                {
                    Value = (Minimum * 1.0) / 100;
                }
                else if (value > Maximum)
                {
                    Value = (Maximum * 1.0) / 100;
                }
                else
                {
                    Value = (value * 1.0) / 100;
                }
                RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
            }

            isUserInput = false;
        }

        private void KeyPressed(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    Text = Format(Value.ToString());
                    CaretIndex = Text.Length;
                    BindingExpression be = GetBindingExpression(ValueProperty);
                    be.UpdateSource();
                    e.Handled = true;
                    break;
                case Key.Space:
                    e.Handled = true;
                    break;
                case Key.Up:
                    if (Value + 0.01 <= (Maximum * 1.0) / 100) Value += 0.01;
                    else Value = (Maximum * 1.0) / 100;
                    CaretIndex = Text.Length;
                    e.Handled = true;
                    break;
                case Key.Down:
                    if (Value - 0.01 >= (Minimum * 1.0) / 100) Value -= 0.01;
                    else Value = (Minimum * 1.0) / 100;
                    CaretIndex = Text.Length;
                    e.Handled = true;
                    break;
            }
        }

        private void TextValidation(object sender, TextCompositionEventArgs e)
        {
            if (Text.Length <= 12 || SelectionLength > 0)
            {
                Regex regex = new Regex(@"^\d*\.?\d*$");
                e.Handled = !(regex.IsMatch(e.Text) && (e.Text == "." ? Text.Count(f => f == '.') == 0 : true));
            }
            else
            {
                e.Handled = true;
            }
        }

        #endregion
    }
}
