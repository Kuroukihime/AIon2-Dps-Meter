using AionDpsMeter.Services.Services.Update;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace AionDpsMeter.UI
{
    public partial class WhatsNewWindow : Window
    {
        private readonly ReleaseInfo _release;
        private readonly UpdateCheckerService _updateChecker;
        private CancellationTokenSource? _cts;

        public WhatsNewWindow(ReleaseInfo release, UpdateCheckerService updateChecker)
        {
            InitializeComponent();
            _release       = release;
            _updateChecker = updateChecker;

            DataContext = new WhatsNewViewModel(release.Name);
            NotesRichTextBox.Document = MarkdownToFlowDocument(release.Body);

            if (string.IsNullOrEmpty(release.ZipUrl))
                UpdateButton.IsEnabled = false;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void OpenGithubButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_release.HtmlUrl))
                Process.Start(new ProcessStartInfo(_release.HtmlUrl) { UseShellExecute = true });
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateButton.IsEnabled     = false;
            OpenGithubButton.IsEnabled = false;
            ProgressPanel.Visibility   = Visibility.Visible;
            StatusText.Visibility      = Visibility.Collapsed;

            _cts = new CancellationTokenSource();

            try
            {
                var progress = new Progress<int>(p =>
                {
                    DownloadProgressBar.Value = p;
                    ProgressText.Text = $"{p}%";
                });

                var zipPath = await _updateChecker.DownloadReleaseAsync(_release, progress, _cts.Token);

                ProgressPanel.Visibility = Visibility.Collapsed;
                SetStatus("Launching updater...", "#A5D6A7");

                LaunchUpdater(zipPath);

                await Task.Delay(800);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Application.Current.MainWindow?.DataContext is ViewModels.MainViewModel vm)
                        vm.Dispose();
                    Application.Current.Shutdown();
                });
            }
            catch (OperationCanceledException)
            {
                SetStatus("Update cancelled.", "#888888");
                UpdateButton.IsEnabled     = true;
                OpenGithubButton.IsEnabled = true;
                ProgressPanel.Visibility   = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ProgressPanel.Visibility   = Visibility.Collapsed;
                SetStatus($"Error: {ex.Message}", "#F44747");
                UpdateButton.IsEnabled     = true;
                OpenGithubButton.IsEnabled = true;
            }
        }

        private void LaunchUpdater(string zipPath)
        {
            var exeDir     = AppContext.BaseDirectory;
            var updaterExe = Path.Combine(exeDir, "AionDpsMeter.Updater.exe");
            var mainExe    = Path.GetFileName(Environment.ProcessPath ?? "AionDpsMeter.exe");
            var pid        = Environment.ProcessId;

            Process.Start(new ProcessStartInfo(updaterExe)
            {
                Arguments       = $"\"{zipPath}\" \"{exeDir.TrimEnd('\\')}\" \"{mainExe}\" {pid}",
                UseShellExecute = false,
                CreateNoWindow  = false
            });
        }

        private void SetStatus(string text, string hexColor)
        {
            StatusText.Text       = text;
            StatusText.Foreground = new SolidColorBrush(
                (Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor));
            StatusText.Visibility = Visibility.Visible;
        }

        // ── Markdown renderer ──────────────────────────────────────────────

        private static FlowDocument MarkdownToFlowDocument(string markdown)
        {
            var doc = new FlowDocument
            {
                PagePadding   = new Thickness(0),
                LineHeight    = double.NaN,
                TextAlignment = TextAlignment.Left
            };

            if (string.IsNullOrWhiteSpace(markdown))
            {
                doc.Blocks.Add(new Paragraph(new Run("No release notes provided.")));
                return doc;
            }

            var lines = markdown.Replace("\r\n", "\n").Split('\n');

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd();

                if (line.StartsWith("### "))      { doc.Blocks.Add(Heading(line[4..], 13)); continue; }
                if (line.StartsWith("## "))       { doc.Blocks.Add(Heading(line[3..], 15)); continue; }
                if (line.StartsWith("# "))        { doc.Blocks.Add(Heading(line[2..], 17)); continue; }

                if (Regex.IsMatch(line, @"^[\-\*\+] "))
                {
                    var p = new Paragraph { Margin = new Thickness(12, 1, 0, 1) };
                    p.Inlines.Add(new Run("• ") { Foreground = new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E)) });
                    AddInlineMarkdown(p.Inlines, line[2..]);
                    doc.Blocks.Add(p);
                    continue;
                }

                if (Regex.IsMatch(line, @"^[-_\*]{3,}$"))
                {
                    doc.Blocks.Add(new BlockUIContainer(new System.Windows.Controls.Separator
                    {
                        Background = new SolidColorBrush(Color.FromRgb(0x21, 0x26, 0x2D)),
                        Margin     = new Thickness(0, 6, 0, 6)
                    }));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    doc.Blocks.Add(new Paragraph { Margin = new Thickness(0, 2, 0, 0) });
                    continue;
                }

                var para = new Paragraph { Margin = new Thickness(0, 1, 0, 1) };
                AddInlineMarkdown(para.Inlines, line);
                doc.Blocks.Add(para);
            }

            return doc;
        }

        private static Paragraph Heading(string text, double size) => new(new Run(text)
        {
            FontSize   = size,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x79, 0xC0, 0xFF))
        })
        { Margin = new Thickness(0, size > 14 ? 10 : 8, 0, 2) };

        private static void AddInlineMarkdown(InlineCollection inlines, string text)
        {
            var pattern = @"(\*\*(.+?)\*\*|\*(.+?)\*|`(.+?)`)";
            var parts   = Regex.Split(text, pattern);

            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;

                if (part.StartsWith("**") && part.EndsWith("**") && part.Length > 4)
                    inlines.Add(new Run(part[2..^2]) { FontWeight = FontWeights.Bold });
                else if (part.StartsWith("`") && part.EndsWith("`") && part.Length > 2)
                    inlines.Add(new Run(part[1..^1])
                    {
                        FontFamily = new FontFamily("Consolas"),
                        FontSize   = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xCE, 0x91, 0x78)),
                        Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x25, 0x30))
                    });
                else if (part.StartsWith("*") && part.EndsWith("*") && part.Length > 2)
                    inlines.Add(new Run(part[1..^1]) { FontStyle = FontStyles.Italic });
                else if (!Regex.IsMatch(part, pattern))
                    inlines.Add(new Run(part));
            }
        }
    }

    internal sealed class WhatsNewViewModel
    {
        public string Title { get; }
        public WhatsNewViewModel(string releaseName) =>
            Title = $"What's New — {releaseName}";
    }
}
