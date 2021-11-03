using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LEDSystem.UI.Helpers;

namespace LEDSystem.UI.Controls
{
    public partial class ColorBoxControl : Control
    {
        public ICommand PickerCommand => new RelayCommand(x => {
            RaiseEvent(new RoutedEventArgs(OnPickerRequestEvent));
        });

        static ColorBoxControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorBoxControl), new FrameworkPropertyMetadata(typeof(ColorBoxControl)));
        }

        public event RoutedEventHandler OnPickerRequest
        {
            add { AddHandler(OnPickerRequestEvent, value); }
            remove { RemoveHandler(OnPickerRequestEvent, value); }
        }

        private static readonly RoutedEvent OnPickerRequestEvent =
            EventManager.RegisterRoutedEvent("OnPickerRequest", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(ColorBoxControl)); 

        public event RoutedEventHandler ColorChanged
        {
            add { AddHandler(OnPickerRequestEvent, value); }
            remove { RemoveHandler(OnPickerRequestEvent, value); }
        }
    }
}
