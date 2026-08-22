using System.Windows;
using System.Windows.Controls;
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

        private void NumericInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            bool allowNegative = IsNegativeAllowed(textBox);
            var normalizedInput = NormalizeIncomingText(textBox, e.Text, allowNegative);
            var proposed = GetProposedText(textBox, normalizedInput);
            if (!IsValidNumericInput(proposed, allowNegative))
            {
                e.Handled = true;
                return;
            }

            if (!string.Equals(normalizedInput, e.Text, StringComparison.Ordinal))
            {
                InsertText(textBox, normalizedInput);
                e.Handled = true;
            }
        }

        private void NumericInput_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            bool allowNegative = IsNegativeAllowed(textBox);

            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            var pastedText = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
            var normalizedText = NormalizeIncomingText(textBox, pastedText, allowNegative);
            var proposed = GetProposedText(textBox, normalizedText);
            if (!IsValidNumericInput(proposed, allowNegative))
            {
                e.CancelCommand();
                return;
            }

            if (!string.Equals(normalizedText, pastedText, StringComparison.Ordinal))
            {
                e.CancelCommand();
                InsertText(textBox, normalizedText);
            }
        }

        private static string NormalizeIncomingText(TextBox textBox, string text, bool allowNegative)
        {
            var normalized = text.Replace(',', '.');
            if (normalized == ".")
            {
                var current = textBox.Text ?? string.Empty;
                bool replacingWholeText = textBox.SelectionLength == current.Length;
                if (textBox.SelectionStart == 0 && (replacingWholeText || current.Length == 0))
                    return "0.";

                if (allowNegative && textBox.SelectionStart == 1 && current == "-")
                    return "0.";
            }

            return normalized;
        }

        private static void InsertText(TextBox textBox, string text)
        {
            var start = textBox.SelectionStart;
            var length = textBox.SelectionLength;
            var current = textBox.Text ?? string.Empty;
            textBox.Text = current.Remove(start, length).Insert(start, text);
            textBox.SelectionStart = start + text.Length;
            textBox.SelectionLength = 0;
        }

        private static string GetProposedText(TextBox textBox, string newText)
        {
            var current = textBox.Text ?? string.Empty;
            var start = textBox.SelectionStart;
            var length = textBox.SelectionLength;
            return current.Remove(start, length).Insert(start, newText);
        }

        private static bool IsNegativeAllowed(TextBox textBox)
            => string.Equals(textBox.Tag as string, "AllowNegative", StringComparison.Ordinal);

        private static bool IsValidNumericInput(string text, bool allowNegative)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            if (allowNegative && text == "-")
                return true;

            int dotCount = 0;
            int minusCount = 0;
            foreach (var ch in text)
            {
                if (char.IsDigit(ch))
                    continue;

                if (allowNegative && ch == '-')
                {
                    minusCount++;
                    if (minusCount > 1 || text[0] != '-')
                        return false;

                    continue;
                }

                if (ch == '.')
                {
                    dotCount++;
                    if (dotCount > 1)
                        return false;

                    continue;
                }

                return false;
            }

            return true;
        }
    }
}
