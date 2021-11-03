using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;
using LEDSystem.UI.Helpers;

namespace LEDSystem.UI.Controls
{
    public partial class NumericUpDownControl : Control
    {
        #region Variables
        private TextBox tbValue = null;
        private TextBlock tbUnit = null;
        private Regex numMatch = new Regex(@"^[0-9]*$");
        private bool isOverMaximum = false;
        private bool isUnderMinimum = false;
        private bool isUserInput = true;
        #endregion

        #region Main
        public ICommand UpCommand => new RelayCommand(x => {
            if (Value < Maximum && !isOverMaximum) {
                Validate(Value + 1);
            }
        });

        public ICommand DownCommand => new RelayCommand(x => {
            if (Value > Minimum && !isUnderMinimum) {
                Validate(Value - 1);
            }
        });

        static NumericUpDownControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericUpDownControl), new FrameworkPropertyMetadata(typeof(NumericUpDownControl)));
        }
        public override void OnApplyTemplate()
        {
            this.tbValue = this.GetTemplateChild("PART_Value") as TextBox;
            this.tbUnit = this.GetTemplateChild("PART_Unit") as TextBlock;

            this.LostFocus += Control_LostFocus;
            this.tbValue.TextChanged += Tb_Value_TextChanged;
            this.tbValue.PreviewTextInput += Tb_Value_PreviewTextInput;
            this.tbValue.PreviewKeyDown += Tb_Value_PreviewKeyDown;

            this.tbUnit.Text = Unit.ToString();
            Validate(Value, false, true, false);

            base.OnApplyTemplate();
        }
        public void Validate(int value, bool updateValue = true, bool updateText = true, bool updateCallback = true)
        {
            // Impedisce ai controlli di convalidare gli input.
            isUserInput = false;

            if (updateValue) {
                Value = value;
            }

            if (updateText && tbValue != null) {
                int caretIndex = tbValue.CaretIndex;
                int textLenght = tbValue.Text.Length;
                bool isCaretAtEnd = (caretIndex == tbValue.Text.Length) ? true : false;

                tbValue.Text = value.ToString();

                if (isCaretAtEnd) {
                    tbValue.CaretIndex = tbValue.Text.Length;
                } else if (textLenght > tbValue.Text.Length) {
                    if (caretIndex - 1 >= 0) {
                        tbValue.CaretIndex = caretIndex - 1;
                    } else {
                        tbValue.CaretIndex = caretIndex;
                    }
                } else {
                    tbValue.CaretIndex = caretIndex;
                }
            }

            if (updateCallback) {
                RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
            }

            // Permette ai controlli di convalidare gli input.
            isUserInput = true;
        }
        #endregion

        #region Events
        private void Control_LostFocus(object sender, RoutedEventArgs e)
        {
            if (isOverMaximum) {
                Validate(Maximum);
            } else if (isUnderMinimum) {
                Validate(Minimum);
            } else if (String.IsNullOrEmpty(tbValue.Text)) {
                Validate(Value);
            }
        }
        private void Tb_Value_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            string text = tbValue.Text.Insert(tbValue.CaretIndex, e.Text);
            e.Handled = !numMatch.IsMatch(text);
        }
        private void Tb_Value_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Incrementa il valore.
            if (e.IsDown && (e.Key == Key.Up) && Value < Maximum && !isOverMaximum) {
                Validate(Value + 1);
            }
            // Decrementa il valore.
            if (e.IsDown && (e.Key == Key.Down) && Value > Minimum && !isUnderMinimum) {
                Validate(Value - 1);
            }
            // Convalida il numero inserito.
            if (e.Key == Key.Enter)
            {
                if (isOverMaximum) {
                    Validate(Maximum);
                } else if (isUnderMinimum) {
                    Validate(Minimum);
                } else if (String.IsNullOrEmpty(tbValue.Text)) {
                    Validate(Value);
                } else {
                    Validate(Convert.ToInt32(tbValue.Text));
                }
            }
        }
        private void Tb_Value_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!String.IsNullOrEmpty(tbValue.Text)) {
                long num = Convert.ToInt64(tbValue.Text);
                isUnderMinimum = (num < Minimum);
                isOverMaximum = (num > Maximum);

                if (!isUnderMinimum && !isOverMaximum && isUserInput) {
                    Validate(Convert.ToInt32(num), true, false, true);
                }
            }
        }

        #endregion

        #region Dependencies

        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(NumericUpDownControl),
                new PropertyMetadata(0, new PropertyChangedCallback(OnValuePropertyChanged)));

        private static void OnValuePropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            NumericUpDownControl numericBox = target as NumericUpDownControl;
            if (numericBox.isUserInput) {
                numericBox.Validate(Convert.ToInt32(e.NewValue), false, true, true);
            }
        }

        private static readonly RoutedEvent ValueChangedEvent =
            EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(NumericUpDownControl));

        public event RoutedEventHandler ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }
        
        public string Unit
        {
            get { return (string)GetValue(UnitProperty); }
            set { SetValue(UnitProperty, value); }
        }

        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register("Unit", typeof(string), typeof(NumericUpDownControl),
                new PropertyMetadata("-", new PropertyChangedCallback(OnUnitPropertyChanged)));

        private static void OnUnitPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            NumericUpDownControl numericBox = target as NumericUpDownControl;
            if (numericBox.tbUnit != null) {
                numericBox.tbUnit.Text = e.NewValue.ToString();
            }
        }

        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(int), typeof(NumericUpDownControl),
                new UIPropertyMetadata(100));

        public int Minimum
        {
            get { return (int)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(int), typeof(NumericUpDownControl),
                new UIPropertyMetadata(0));

        #endregion
    }
}
