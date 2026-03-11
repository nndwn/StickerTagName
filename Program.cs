using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using SkiaSharp;

public class PersonEntry
{
    public string? Suffix { get; set; }
    public required string Name { get; set; }
}

class Program
{
    private const int Dpi = 72;
    
    private static async Task Main(string[] args)
    {
        const int cols = 3, rows = 4;
        const int perPage = cols * rows;
        
        var border = args.Any(a=> a.Equals("-border",  StringComparison.CurrentCultureIgnoreCase));
        var names = await GetNames();
        
        var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var resultFolder = Path.Combine(picturesPath, "Result");
        Directory.CreateDirectory(resultFolder);
        
        await using var stream = File.OpenWrite(Path.Combine(resultFolder, "labels.pdf"));
        using var document = SKDocument.CreatePdf(stream);
        
        foreach (var chunk  in names.Chunk(perPage))
        {
            using var pdfCanvas = document.BeginPage(MmToPx(210),  MmToPx(297));
            pdfCanvas.Clear(SKColors.White);
            DrawLabel(pdfCanvas, chunk.ToArray(), border,cols , rows);
            document.EndPage();
        }
        
        document.Close();   
    }
    
    
    private static int MmToPx(double mm) => (int)( mm * Dpi / 25.4);

    private static void DrawLabel(SKCanvas canvas, string[] names, bool border, int cols, int rows)
    {
        var imgW = MmToPx(64);
        var imgH = MmToPx(32);
        var padX = MmToPx(3);
        var padY = MmToPx(1.5);
        var margin = MmToPx(3);

        using var cookieFont = File.OpenRead("Fonts/Cookie-Regular.ttf");
        var typeface = SKTypeface.FromStream(cookieFont) ?? SKTypeface.Default;

        var font = new SKFont
        {
            Typeface = typeface,
            Size = 16 * Dpi / 72f
        };
        
        const SKTextAlign alignText = SKTextAlign.Center;

        var paint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true,
        };

        var borderPaint = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 0.25f,
            IsAntialias = true,
        };
        
        canvas.Save();
        canvas.Translate(margin, margin);



        var index = 0;
        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                if (index >= names.Length) break;
                var text = $"{names[index]}\ndan Keluarga di tempat";
                var x = c * (imgW + padX);
                var y = r * (imgH + padY);
                
                var rect = new SKRect(x, y,x+ imgW, y + imgH);
                var centerX = rect.MidX;
                var metrics = font.Metrics;
                var lineHeight = metrics.Descent - metrics.Ascent + 4;
                var lines = text.Split('\n');
                var totalHeight = lines.Length * lineHeight;
                var starY = rect.MidY - totalHeight / 2 - metrics.Ascent;

                foreach (var line in lines)
                {
                    canvas.DrawText(line, centerX, starY, alignText, font, paint);
                    starY += lineHeight;
                }
                if(border) canvas.DrawRect(rect, borderPaint);
                index++;

            }
        }
        
        canvas.Restore();
    }

    private static async Task<string[]> GetNames()
    {
        using var reader = new StreamReader("data.csv");
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            PrepareHeaderForMatch = args => args.Header.ToLower()
        });
        var records = new List<PersonEntry>();
        await foreach (var record in csv.GetRecordsAsync<PersonEntry>())
        {
            records.Add(record);
        }

    
        return records
            .Select(r => $"{(string.IsNullOrWhiteSpace(r.Suffix) ? "" : r.Suffix + " ")}{r.Name}")
            .ToArray();
    }
}
