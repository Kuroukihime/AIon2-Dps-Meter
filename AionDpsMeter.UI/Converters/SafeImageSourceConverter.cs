
using AionDpsMeter.Core.Data;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace AionDpsMeter.UI.Converters
{
   
    public class SafeImageSourceConverter : IValueConverter
    {
        private static readonly ConcurrentDictionary<string, BitmapImage> PackImageCache = new(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, Uri> PackUriCache = new(StringComparer.Ordinal);

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string path || string.IsNullOrWhiteSpace(path))
                return null;

            path = path.Trim();

            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return LoadFromCdn(path, parameter);
            }

            return LoadFromPack(path);
        }

        private static BitmapImage? LoadFromCdn(string url, object? notifyTarget)
        {
            var cache = SkillIconCache.Instance;

            var localPath = cache.GetLocalPathOrStartDownload(url, onDownloaded: () =>
            {
                Application.Current?.Dispatcher.BeginInvoke(() =>
                {
                    if (notifyTarget is INotifyPropertyChanged npc)
                    {
                        var field = npc.GetType()
                            .GetField("PropertyChanged",
                                System.Reflection.BindingFlags.Instance |
                                System.Reflection.BindingFlags.NonPublic);
                        var handler = field?.GetValue(npc) as PropertyChangedEventHandler;
                        handler?.Invoke(npc, new PropertyChangedEventArgs(string.Empty));
                    }
                });
            });

            if (localPath is null) return null;
            return LoadBitmapFromFile(localPath);
        }

        private static BitmapImage? LoadBitmapFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                if (bitmap.CanFreeze) bitmap.Freeze();
                return bitmap;
            }
            catch { return null; }
        }

        private static BitmapImage? LoadFromPack(string path)
        {
            try
            {
                var normalizedPath = path.StartsWith("/", StringComparison.Ordinal) ? path : "/" + path;

                if (PackImageCache.TryGetValue(normalizedPath, out var cachedBitmap))
                    return cachedBitmap;

                var uri = PackUriCache.GetOrAdd(normalizedPath,
                    static p => new Uri($"pack://application:,,,{p}", UriKind.Absolute));

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = uri;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                if (bitmap.CanFreeze) bitmap.Freeze();

                PackImageCache.TryAdd(normalizedPath, bitmap);
                return bitmap;
            }
            catch { return null; }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }
}
