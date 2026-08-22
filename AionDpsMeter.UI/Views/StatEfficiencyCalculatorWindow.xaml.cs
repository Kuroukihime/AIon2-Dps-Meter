using System.Windows;
using System.Windows.Input;

namespace AionDpsMeter.UI
{
    public partial class StatEfficiencyCalculatorWindow : Window
    {
        public StatEfficiencyCalculatorWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
