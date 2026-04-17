using AionDpsMeter.Core.Data;
using AionDpsMeter.Services.Models;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AionDpsMeter.UI.Controls
{
    public sealed class DpsChartControl : FrameworkElement
    {
        public static readonly DependencyProperty DataPointsProperty =
            DependencyProperty.Register(nameof(DataPoints), typeof(IReadOnlyList<DpsDataPoint>),
                typeof(DpsChartControl), new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BuffTimelineProperty =
            DependencyProperty.Register(nameof(BuffTimeline), typeof(IReadOnlyList<BuffTimelineEntry>),
                typeof(DpsChartControl), new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TotalDurationSecProperty =
            DependencyProperty.Register(nameof(TotalDurationSec), typeof(double),
                typeof(DpsChartControl), new FrameworkPropertyMetadata(0.0,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public IReadOnlyList<DpsDataPoint>?     DataPoints   { get => (IReadOnlyList<DpsDataPoint>?)GetValue(DataPointsProperty);     set => SetValue(DataPointsProperty, value); }
        public IReadOnlyList<BuffTimelineEntry>? BuffTimeline { get => (IReadOnlyList<BuffTimelineEntry>?)GetValue(BuffTimelineProperty); set => SetValue(BuffTimelineProperty, value); }
        public double TotalDurationSec                        { get => (double)GetValue(TotalDurationSecProperty);                       set => SetValue(TotalDurationSecProperty, value); }

        // ── Layout constants ──────────────────────────────────────────────────
        private const double ChartPaddingLeft   = 54;
        private const double ChartPaddingRight  = 12;
        private const double ChartPaddingTop    = 14;
        private const double XAxisLabelH        = 16;
        private const double TimelineRowH       = 22;
        private const double TimelineTopPad     = 4;
        private const double IconSize           = 16;
        private const double BarHeight          = 10;
        private const double MinChartH          = 80;

        // ── Colour palette ────────────────────────────────────────────────────
        private static readonly Color[] BuffPalette =
        [
            Color.FromRgb(0xFF, 0xD7, 0x00), // gold
            Color.FromRgb(0xC0, 0x71, 0xFF), // violet
            Color.FromRgb(0x00, 0xBF, 0xFF), // sky-blue
            Color.FromRgb(0xFF, 0x69, 0xB4), // hot-pink
            Color.FromRgb(0x7F, 0xFF, 0x00), // chartreuse
            Color.FromRgb(0xFF, 0x45, 0x00), // orange-red
            Color.FromRgb(0x00, 0xFF, 0xD0), // aqua
            Color.FromRgb(0xFF, 0xA5, 0x00), // orange
            Color.FromRgb(0xAD, 0xFF, 0x2F), // green-yellow
            Color.FromRgb(0xFF, 0x63, 0x47), // tomato
            Color.FromRgb(0x87, 0xCE, 0xFA), // light-blue
            Color.FromRgb(0xFF, 0xC0, 0xCB), // pink
        ];

        // ── Static frozen brushes / pens ──────────────────────────────────────
        private static readonly Pen   CumulativePen = MakePen(Color.FromRgb(0x3D, 0xC9, 0xB0), 2.0);
        private static readonly Pen   PerSecPen     = MakePen(Color.FromRgb(0xFF, 0x9A, 0x3C), 1.5);
        private static readonly Pen   GridPen       = MakePen(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF), 1.0);
        private static readonly Pen   AxisPen       = MakePen(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF), 1.0);
        private static readonly Pen   CrosshairPen;
        private static readonly SolidColorBrush BgBrush        = Frozen(Color.FromArgb(0x18, 0x00, 0x00, 0x00));
        private static readonly SolidColorBrush LabelBrush     = Frozen(Color.FromArgb(0xCC, 0xCC, 0xCC, 0xCC));
        private static readonly SolidColorBrush FillCum        = Frozen(Color.FromArgb(0x28, 0x3D, 0xC9, 0xB0));
        private static readonly SolidColorBrush FillPerSec     = Frozen(Color.FromArgb(0x18, 0xFF, 0x9A, 0x3C));
        private static readonly SolidColorBrush TooltipBg      = Frozen(Color.FromArgb(0xE0, 0x0D, 0x11, 0x17));
        private static readonly SolidColorBrush TooltipBorder  = Frozen(Color.FromArgb(0xFF, 0x30, 0x36, 0x3D));
        private static readonly SolidColorBrush InactiveRail   = Frozen(Color.FromArgb(0x40, 0x80, 0x80, 0x80));
        private static readonly Typeface Mono = new("Consolas");
        private static readonly Typeface Ui   = new("Segoe UI");

        static DpsChartControl()
        {
            var dashPen = new Pen(new SolidColorBrush(Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF)), 1)
            {
                DashStyle = new DashStyle([4, 3], 0)
            };
            dashPen.Freeze();
            CrosshairPen = dashPen;
        }

        // ── Mouse tracking ────────────────────────────────────────────────────
        private Point _mouse = new(-1, -1);

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _mouse = e.GetPosition(this);
            InvalidateVisual();
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            _mouse = new(-1, -1);
            InvalidateVisual();
        }

        // ── Render ────────────────────────────────────────────────────────────
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double totalW = ActualWidth;
            double totalH = ActualHeight;
            if (totalW < 30 || totalH < 30) return;

            var points   = DataPoints;
            var timeline = BuffTimeline;
            double dur   = TotalDurationSec;

            // Group buff rows upfront (needed for height calc)
            var buffRows = GroupBuffRows(timeline);
            int rowCount = buffRows.Count;

            double timelineH = rowCount > 0
                ? Math.Min(totalH - MinChartH - ChartPaddingTop - XAxisLabelH,
                           TimelineTopPad + rowCount * TimelineRowH)
                : 0;
            timelineH = Math.Max(0, timelineH);

            double chartBottom = totalH - timelineH - XAxisLabelH;
            double chartH      = chartBottom - ChartPaddingTop;
            double chartW      = totalW - ChartPaddingLeft - ChartPaddingRight;
            double x0          = ChartPaddingLeft;
            double y0          = ChartPaddingTop;

            // Global background (also enables mouse hit-test)
            dc.DrawRectangle(BgBrush, null, new Rect(0, 0, totalW, totalH));

            bool hasData = points is { Count: >= 2 } && dur > 0;

            double yMax = 1;
            if (hasData)
            {
                foreach (var p in points!)
                {
                    if (p.PerSecondDamage > yMax) yMax = p.PerSecondDamage;
                    if (p.CumulativeDps   > yMax) yMax = p.CumulativeDps;
                }
            }

            // Grid + Y labels
            DrawGrid(dc, x0, y0, chartW, chartH, yMax);

            if (!hasData)
            {
                DrawNoData(dc, totalW, y0 + chartH / 2);
            }
            else
            {
                // Fill
                DrawFill(dc, points!, dur, x0, y0, chartW, chartH, yMax, cumulative: true);
                DrawFill(dc, points!, dur, x0, y0, chartW, chartH, yMax, cumulative: false);

                // Lines
                DrawLine(dc, points!, dur, x0, y0, chartW, chartH, yMax, CumulativePen, cumulative: true);
                DrawLine(dc, points!, dur, x0, y0, chartW, chartH, yMax, PerSecPen,     cumulative: false);

                // Hover crosshair + tooltip
                if (_mouse.X >= x0 && _mouse.X <= x0 + chartW && _mouse.Y >= y0 && _mouse.Y <= chartBottom)
                    DrawHover(dc, points!, dur, x0, y0, chartW, chartH, yMax, chartBottom, totalW);
            }

            // Axes
            dc.DrawLine(AxisPen, new Point(x0, y0),          new Point(x0, chartBottom));
            dc.DrawLine(AxisPen, new Point(x0, chartBottom),  new Point(x0 + chartW, chartBottom));

            // X labels
            DrawXLabels(dc, dur, x0, chartBottom, chartW);

            // Legend
            DrawLegend(dc, totalW, y0);

            // Buff timeline
            if (timelineH > 0 && rowCount > 0)
            {
                double tlY = totalH - timelineH;
                DrawBuffTimeline(dc, buffRows, dur, x0, tlY, chartW, timelineH, totalW);
            }
        }

        // ── Chart drawing helpers ─────────────────────────────────────────────

        private static void DrawNoData(DrawingContext dc, double cx, double cy)
        {
            var ft = MakeText("No combat data", 11);
            dc.DrawText(ft, new Point(cx - ft.Width / 2, cy - ft.Height / 2));
        }

        private static void DrawGrid(DrawingContext dc, double x0, double y0, double w, double h, double yMax)
        {
            for (int i = 0; i <= 5; i++)
            {
                double fy = i / 5.0;
                double y  = y0 + h - fy * h;
                dc.DrawLine(GridPen, new Point(x0, y), new Point(x0 + w, y));

                var ft = MakeText(FormatDmg((long)(fy * yMax)), 8.5);
                dc.DrawText(ft, new Point(x0 - ft.Width - 3, y - ft.Height / 2));
            }
        }

        private static void DrawLine(DrawingContext dc, IReadOnlyList<DpsDataPoint> pts,
            double dur, double x0, double y0, double w, double h, double yMax, Pen pen, bool cumulative)
        {
            Point? prev = null;
            foreach (var p in pts)
            {
                var pt = DataPt(p, dur, x0, y0, w, h, yMax, cumulative);
                if (prev.HasValue) dc.DrawLine(pen, prev.Value, pt);
                prev = pt;
            }
        }

        private static void DrawFill(DrawingContext dc, IReadOnlyList<DpsDataPoint> pts,
            double dur, double x0, double y0, double w, double h, double yMax, bool cumulative)
        {
            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                bool started = false;
                double lastX = x0;
                foreach (var p in pts)
                {
                    var pt = DataPt(p, dur, x0, y0, w, h, yMax, cumulative);
                    if (!started) { ctx.BeginFigure(new Point(pt.X, y0 + h), true, true); started = true; }
                    ctx.LineTo(pt, true, false);
                    lastX = pt.X;
                }
                if (started) ctx.LineTo(new Point(lastX, y0 + h), true, false);
            }
            geo.Freeze();
            dc.DrawGeometry(cumulative ? FillCum : FillPerSec, null, geo);
        }

        private void DrawHover(DrawingContext dc, IReadOnlyList<DpsDataPoint> pts,
            double dur, double x0, double y0, double w, double h, double yMax,
            double chartBottom, double totalW)
        {
            // Find nearest data point by X
            double tHovered = (_mouse.X - x0) / w * dur;
            int idx = 0;
            double minDist = double.MaxValue;
            for (int i = 0; i < pts.Count; i++)
            {
                double d = Math.Abs(pts[i].SecondOffset - tHovered);
                if (d < minDist) { minDist = d; idx = i; }
            }
            var dp = pts[idx];

            double px = x0 + (dp.SecondOffset / dur) * w;

            // Vertical crosshair
            dc.DrawLine(CrosshairPen, new Point(px, y0), new Point(px, chartBottom));

            // Dots on each line
            var cumPt    = DataPt(dp, dur, x0, y0, w, h, yMax, cumulative: true);
            var perSecPt = DataPt(dp, dur, x0, y0, w, h, yMax, cumulative: false);
            DrawDot(dc, cumPt,    Color.FromRgb(0x3D, 0xC9, 0xB0));
            DrawDot(dc, perSecPt, Color.FromRgb(0xFF, 0x9A, 0x3C));

            // Tooltip text
            int sec     = (int)dp.SecondOffset;
            string time = FormatTimeFull(sec);
            string cumS = FormatDmg((long)dp.CumulativeDps) + " DPS";
            string perS = FormatDmg(dp.PerSecondDamage)     + " /sec";

            var ftTime = MakeText(time, 8.5);
            var ftCum  = MakeTextColored($"-- {cumS}",  9, Color.FromRgb(0x3D, 0xC9, 0xB0));
            var ftPerS = MakeTextColored($"-- {perS}", 9, Color.FromRgb(0xFF, 0x9A, 0x3C));

            double boxW = Math.Max(ftTime.Width, Math.Max(ftCum.Width, ftPerS.Width)) + 14;
            double boxH = ftTime.Height + ftCum.Height + ftPerS.Height + 16;

            double bx = px + 8;
            if (bx + boxW > totalW - ChartPaddingRight) bx = px - boxW - 8;
            double by = Math.Max(y0, Math.Min(_mouse.Y - boxH / 2, chartBottom - boxH));

            dc.DrawRoundedRectangle(TooltipBg, new Pen(TooltipBorder, 1), new Rect(bx, by, boxW, boxH), 4, 4);

            // Coloured indicator squares instead of bullet chars
            double lineY = by + 5;
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x3D, 0xC9, 0xB0)), null, new Rect(bx + 7, lineY + ftTime.Height + 4, 8, 8));
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0xFF, 0x9A, 0x3C)), null, new Rect(bx + 7, lineY + ftTime.Height + ftCum.Height + 8, 8, 8));

            dc.DrawText(ftTime, new Point(bx + 7, lineY));

            var ftCumClean  = MakeTextColored(cumS,  9, Color.FromRgb(0x3D, 0xC9, 0xB0));
            var ftPerSClean = MakeTextColored(perS, 9, Color.FromRgb(0xFF, 0x9A, 0x3C));
            dc.DrawText(ftCumClean,  new Point(bx + 18, lineY + ftTime.Height + 2));
            dc.DrawText(ftPerSClean, new Point(bx + 18, lineY + ftTime.Height + ftCumClean.Height + 6));

            // Recalc box using clean texts
            boxW = Math.Max(ftTime.Width, Math.Max(ftCumClean.Width, ftPerSClean.Width)) + 28;
            // Already drawn the rect — just make sure it's wide enough next render; no action needed
        }

        private static void DrawDot(DrawingContext dc, Point pt, Color c)
        {
            dc.DrawEllipse(new SolidColorBrush(c), new Pen(Brushes.White, 1), pt, 4, 4);
        }

        private static void DrawXLabels(DrawingContext dc, double dur, double x0, double chartBottom, double w)
        {
            if (dur <= 0) return;
            int step = dur > 120 ? 30 : dur > 60 ? 10 : 5;
            for (int s = 0; s <= (int)dur; s += step)
            {
                double x  = x0 + (s / dur) * w;
                var ft = MakeText(FormatTimeFull(s), 8);
                dc.DrawText(ft, new Point(x - ft.Width / 2, chartBottom + 2));
            }
        }

        private static void DrawLegend(DrawingContext dc, double totalW, double y0)
        {
            double x = totalW - ChartPaddingRight - 195;
            DrawLegendItem(dc, x,        y0 + 1, CumulativePen, "Cumulative DPS");
            DrawLegendItem(dc, x + 100,  y0 + 1, PerSecPen,     "Per-second dmg");
        }

        private static void DrawLegendItem(DrawingContext dc, double x, double y, Pen pen, string label)
        {
            dc.DrawLine(pen, new Point(x, y + 5), new Point(x + 16, y + 5));
            dc.DrawText(MakeText(label, 8.5), new Point(x + 19, y));
        }

        // ── Buff timeline ─────────────────────────────────────────────────────

        private record BuffRow(int BuffId, string Name, string? Icon, int ColorIdx, List<(double S, double E)> Segs);

        private static List<BuffRow> GroupBuffRows(IReadOnlyList<BuffTimelineEntry>? timeline)
        {
            if (timeline is null or { Count: 0 }) return [];
            var rows    = new List<BuffRow>();
            var idxMap  = new Dictionary<int, int>();
            int colorI  = 0;
            foreach (var e in timeline)
            {
                if (!idxMap.TryGetValue(e.BuffId, out int ri))
                {
                    ri = rows.Count;
                    idxMap[e.BuffId] = ri;
                    rows.Add(new BuffRow(e.BuffId, e.BuffName, e.BuffIcon, colorI++ % BuffPalette.Length, []));
                }
                rows[ri].Segs.Add((e.StartSec, e.EndSec));
            }
            return rows;
        }

        private void DrawBuffTimeline(DrawingContext dc, List<BuffRow> rows,
            double dur, double x0, double tlY, double w, double tlH, double totalW)
        {
            if (dur <= 0) return;

            // Section background
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(0x28, 0x00, 0x00, 0x00)), null,
                new Rect(0, tlY, totalW, tlH));

            // Separator line
            dc.DrawLine(AxisPen, new Point(x0, tlY), new Point(x0 + w, tlY));

            double rowY = tlY + TimelineTopPad;
            int    visibleRows = (int)Math.Floor((tlH - TimelineTopPad) / TimelineRowH);

            for (int ri = 0; ri < Math.Min(rows.Count, visibleRows); ri++)
            {
                var row   = rows[ri];
                double ry = rowY + ri * TimelineRowH;
                var    c  = BuffPalette[row.ColorIdx];

                // Alternating row stripe
                byte alpha = (byte)(ri % 2 == 0 ? 0x14 : 0x00);
                if (alpha > 0)
                    dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(alpha, 0xFF, 0xFF, 0xFF)), null,
                        new Rect(x0, ry, w, TimelineRowH));

                // Inactive "rail" — thin horizontal line across the full duration
                double railY = ry + (TimelineRowH - 2) / 2.0;
                dc.DrawRectangle(InactiveRail, null, new Rect(x0, railY, w, 2));

                // Active segments — solid bar with border
                var barFill   = new SolidColorBrush(Color.FromArgb(0xCC, c.R, c.G, c.B));
                var barBorder = new Pen(new SolidColorBrush(Color.FromArgb(0xFF, c.R, c.G, c.B)), 1);
                barFill.Freeze();
                barBorder.Freeze();

                double barY = ry + (TimelineRowH - BarHeight) / 2.0;
                foreach (var (s, e) in row.Segs)
                {
                    double bx = x0 + (s / dur) * w;
                    double bw = Math.Max(3, (e - s) / dur * w);
                    dc.DrawRoundedRectangle(barFill, barBorder, new Rect(bx, barY, bw, BarHeight), 2, 2);
                }

                // Icon — fixed in left margin
                double iconX = x0 - IconSize - 4;
                double iconY = ry + (TimelineRowH - IconSize) / 2.0;
                DrawBuffIcon(dc, row.Icon, iconX, iconY, c);

                // Buff name tooltip if mouse is over this row
                if (_mouse.Y >= ry && _mouse.Y < ry + TimelineRowH &&
                    _mouse.X >= x0 && _mouse.X <= x0 + w)
                {
                    DrawBuffTooltip(dc, row, dur, x0, w, ry, totalW);
                }
            }

            // Show row count hint if clipped
            if (rows.Count > visibleRows)
            {
                var ft = MakeText($"+{rows.Count - visibleRows} more buffs", 8);
                dc.DrawText(ft, new Point(x0 + 4, tlY + tlH - ft.Height - 2));
            }
        }

        private static void DrawBuffTooltip(DrawingContext dc, BuffRow row,
            double dur, double x0, double w, double rowY, double totalW)
        {
            double activeSec = row.Segs.Sum(seg => seg.E - seg.S);
            string uptimePct = dur > 0 ? $"{activeSec / dur * 100:F0}%" : "-";

            var ftName   = MakeText(row.Name, 9);
            var ftDetail = MakeText($"x{row.Segs.Count}  uptime {FormatTimeFull((int)activeSec)} ({uptimePct})", 8);

            double boxW = Math.Max(ftName.Width, ftDetail.Width) + 14;
            double boxH = ftName.Height + ftDetail.Height + 12;
            double bx   = Math.Min(totalW - boxW - 4, x0 + 4);
            double by   = rowY - boxH - 2;

            dc.DrawRoundedRectangle(TooltipBg, new Pen(TooltipBorder, 1), new Rect(bx, by, boxW, boxH), 4, 4);
            dc.DrawText(ftName,   new Point(bx + 7, by + 4));
            dc.DrawText(ftDetail, new Point(bx + 7, by + 4 + ftName.Height + 2));
        }

        private static void DrawBuffIcon(DrawingContext dc, string? iconPath, double x, double y, Color fallbackColor)
        {
            var img = iconPath is not null ? LoadImage(iconPath) : null;
            if (img is not null)
            {
                dc.DrawImage(img, new Rect(x, y, IconSize, IconSize));
            }
            else
            {
                // Fallback: coloured square
                dc.DrawRoundedRectangle(
                    new SolidColorBrush(Color.FromArgb(0xCC, fallbackColor.R, fallbackColor.G, fallbackColor.B)),
                    null, new Rect(x, y, IconSize, IconSize), 2, 2);
            }
        }

        // ── Image loading ─────────────────────────────────────────────────────
        private static BitmapSource? LoadImage(string path)
        {
            try
            {
                if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    var local = SkillIconCache.Instance.GetLocalPathOrStartDownload(path, null);
                    if (local is null || !File.Exists(local)) return null;
                    return LoadFile(local);
                }
                var uri = path.StartsWith('/') ? $"pack://application:,,,{path}" : $"pack://application:,,,/{path}";
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource    = new Uri(uri);
                bmp.CacheOption  = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch { return null; }
        }

        private static BitmapSource? LoadFile(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource   = new Uri(filePath, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch { return null; }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static Point DataPt(DpsDataPoint p, double dur,
            double x0, double y0, double w, double h, double yMax, bool cumulative)
        {
            double xf  = p.SecondOffset / dur;
            double val = cumulative ? p.CumulativeDps : p.PerSecondDamage;
            double yf  = yMax > 0 ? val / yMax : 0;
            return new Point(x0 + xf * w, y0 + h - yf * h);
        }

        private static Pen MakePen(Color c, double thickness)
        {
            var p = new Pen(new SolidColorBrush(c), thickness);
            p.Freeze();
            return p;
        }

        private static SolidColorBrush Frozen(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        private static FormattedText MakeText(string text, double size) =>
            new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                Mono, size, LabelBrush, 96);

        private static FormattedText MakeTextColored(string text, double size, Color c) =>
            new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                Ui, size, new SolidColorBrush(c), 96);

        private static string FormatDmg(long v)
        {
            if (v >= 1_000_000) return $"{v / 1_000_000.0:F1}M";
            if (v >= 1_000)     return $"{v / 1_000.0:F1}K";
            return v.ToString();
        }

        private static string FormatTimeFull(int sec)
        {
            int m = sec / 60, s = sec % 60;
            return m > 0 ? $"{m}:{s:D2}" : $"0:{s:D2}";
        }
    }
}
