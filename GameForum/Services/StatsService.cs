using Bogus;
using ScottPlot;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Linq;

namespace GameForum.Services;

public class StatsService
{
    public record Fixture(DateTime Date,
        int UsersOnline,
        int Messages,
        int NewTopics,
        int Attachments,
        double BounceRate);

    public class Result
    {
        public required string LineChartPath { get; init; }
        public required string BarChartPath { get; init; }
        public required string PieChartPath { get; init; }
        public required List<Fixture> Data { get; init; }
    }

    private readonly IWebHostEnvironment _env;
    private readonly object _sync = new();

    public StatsService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public Result Generate()
    {
        var fixtures = GenerateFixtures(50);

        // Ensure output folder wwwroot/stats
        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var outDir = Path.Combine(webRoot, "stats");
        Directory.CreateDirectory(outDir);

        // 1) Line chart: UsersOnline by date
        var linePng = Path.Combine(outDir, "users_online.png");
        SaveLineChart(fixtures, linePng, title: "Пользователи онлайн по дням");
        ApplyWatermark(linePng, "GameForum", 0.18f);

        // 2) Bar chart: Messages per date
        var barPng = Path.Combine(outDir, "messages.png");
        SaveBarChart(fixtures, barPng, title: "Сообщения по дням");
        ApplyWatermark(barPng, "GameForum", 0.18f);

        // 3) Pie chart: NewTopics by weekday
        var piePng = Path.Combine(outDir, "topics_by_weekday.png");
        SavePieChart(fixtures, piePng, title: "Новые темы по дням недели");
        ApplyWatermark(piePng, "GameForum", 0.18f);

        // Public URLs
        var lineUrl = "/stats/users_online.png";
        var barUrl = "/stats/messages.png";
        var pieUrl = "/stats/topics_by_weekday.png";

        return new Result
        {
            LineChartPath = lineUrl,
            BarChartPath = barUrl,
            PieChartPath = pieUrl,
            Data = fixtures
        };
    }

    private static List<Fixture> GenerateFixtures(int count)
    {
        var startDate = DateTime.Today.AddDays(-count + 1);
        var faker = new Faker<Fixture>("ru")
            .CustomInstantiator(f => new Fixture(
                Date: startDate.AddDays(f.IndexFaker),
                UsersOnline: f.Random.Int(10, 500),
                Messages: f.Random.Int(0, 1000),
                NewTopics: f.Random.Int(0, 50),
                Attachments: f.Random.Int(0, 200),
                BounceRate: Math.Round(f.Random.Double(10, 90), 2)
            ));
        return faker.Generate(count);
    }

    private static void SaveLineChart(List<Fixture> data, string path, string title)
    {
        var plt = new ScottPlot.Plot(800, 500);
        var xs = data.Select(d => d.Date.ToOADate()).ToArray();
        var ys = data.Select(d => (double)d.UsersOnline).ToArray();
        var sp = plt.AddScatter(xs, ys, color: System.Drawing.Color.DeepSkyBlue, markerSize: 3);
        plt.Title(title);
        plt.XAxis.DateTimeFormat(true);
        plt.YLabel("Пользователи");
        plt.XLabel("Дата");
        lock (path)
        {
            plt.SaveFig(path);
        }
    }

    private static void SaveBarChart(List<Fixture> data, string path, string title)
    {
        var plt = new ScottPlot.Plot(800, 500);
        var xs = data.Select(d => d.Date.ToOADate()).ToArray();
        var ys = data.Select(d => (double)d.Messages).ToArray();
        var bar = plt.AddBar(ys, xs);
        bar.BarWidth = 0.6;
        bar.FillColor = System.Drawing.Color.FromArgb(60, 150, 250);
        plt.Title(title);
        plt.XAxis.DateTimeFormat(true);
        plt.YLabel("Сообщения");
        plt.XLabel("Дата");
        lock (path)
        {
            plt.SaveFig(path);
        }
    }

    private static void SavePieChart(List<Fixture> data, string path, string title)
    {
        var byWeekday = data
            .GroupBy(d => d.Date.DayOfWeek)
            .OrderBy(g => g.Key)
            .Select(g => (Label: g.Key.ToString(), Value: (double)g.Sum(x => x.NewTopics)))
            .ToArray();

        var plt = new ScottPlot.Plot(700, 500);
        var pie = plt.AddPie(byWeekday.Select(x => x.Value).ToArray());
        pie.SliceLabels = byWeekday.Select(x => x.Label).ToArray();
        pie.ShowPercentages = true;
        plt.Title(title);
        lock (path)
        {
            plt.SaveFig(path);
        }
    }

    private static void ApplyWatermark(string imagePath, string text, float opacity)
    {
        // Load image
        using var image = Image.Load<Rgba32>(imagePath);

        // Choose font (prefer Arial/DejaVu; fallback to first available system font)
        Font font;
        try
        {
            font = SystemFonts.CreateFont("Arial", Math.Max(24, image.Width / 12f), FontStyle.Bold);
        }
        catch
        {
            try
            {
                font = SystemFonts.CreateFont("DejaVu Sans", Math.Max(24, image.Width / 12f), FontStyle.Bold);
            }
            catch
            {
                var fam = SystemFonts.Families.FirstOrDefault();
                var size = Math.Max(24, image.Width / 12f);
                font = fam != null ? fam.CreateFont(size, FontStyle.Bold) : SystemFonts.CreateFont("Arial", size, FontStyle.Bold);
            }
        }

        // Create options
        var color = Color.White.WithAlpha(Math.Clamp(opacity, 0.15f, 0.5f));
        var shadowColor = Color.Black.WithAlpha(Math.Clamp(opacity + 0.1f, 0.2f, 0.6f));
        var options = new DrawingOptions
        {
            GraphicsOptions = new GraphicsOptions
            {
                Antialias = true,
                AlphaCompositionMode = PixelAlphaCompositionMode.SrcOver,
                BlendPercentage = 1.0f
            }
        };

        // Centered watermark with shadow for visibility
        var center = new PointF(image.Width / 2f, image.Height / 2f);
        // Measure to center by adjusting origin using alignment via TextOptions is more complex; using offsets with Shadow
        var shadowOffset = new PointF(center.X + 2, center.Y + 2);
        image.Mutate(ctx =>
        {
            // shadow
            ctx.DrawText(options, text, font, shadowColor, shadowOffset);
            // main
            ctx.DrawText(options, text, font, color, center);
        });

        image.Save(imagePath);
    }
}
