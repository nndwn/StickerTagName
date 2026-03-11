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
    private const int Dpi = 300;
    
    private static async Task Main(string[] args)
    {
        const int cols = 3, rows = 4;
        const int perPage = cols * rows;
        
        var border = args.Any(a=> a.Equals("-border",  StringComparison.CurrentCultureIgnoreCase));
        var names = await GetNames();
      
        
        var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        var resultFolder = Path.Combine(picturesPath, "Result");

        var page = 0;
        foreach (var chunk  in names.Chunk(perPage))
        {
            var createLabel = CreateA4WithLabel(chunk, border, rows, cols);
            Directory.CreateDirectory(resultFolder);
            var outputPath = Path.Combine(resultFolder,  $"output_page{page}.png");
            await File.WriteAllBytesAsync(outputPath, createLabel);
            page++;
        }
        
    }
    
    
    private static int MmToPx(double mm) => (int)( mm * Dpi / 25.4);

    private static byte[] CreateA4WithLabel(string[] names, bool border, int rows, int cols)
    {
        var a4Width = MmToPx(210);
        var a4Height = MmToPx(297);
        var marginPx = MmToPx(3);
        var labelBytes = CreateLabel(names, border, rows, cols);
        using var labelImage = SKImage.FromEncodedData(labelBytes);

        using var surfaceA4 = SKSurface.Create(new SKImageInfo(a4Width, a4Height));
        var canvasA4 = surfaceA4.Canvas;
        canvasA4.Clear(SKColors.White);

        canvasA4.Save();
        
        float pivotX = marginPx;
        float pivotY = marginPx;
        
        canvasA4.Translate(pivotX, pivotY);


        canvasA4.RotateDegrees(90);


        canvasA4.DrawImage(labelImage, 0, -labelImage.Height);

        canvasA4.Restore();

        using var imageA4 = surfaceA4.Snapshot();
        using var data = imageA4.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static byte[] CreateLabel(string[] names, bool border , int rows, int cols )
    {
        var imgW = MmToPx(64);
        var imgH = MmToPx(32);
        var padX = MmToPx(3);
        var padY = MmToPx(1.5);

        
        var canvasW = cols * imgW + (cols - 1) * padX;
        var canvasH = rows * imgH + (rows - 1) * padY;

        using var surface = SKSurface.Create(new SKImageInfo(canvasW, canvasH));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);
        
        using var cookieFont = File.OpenRead("Fonts/Cookie-Regular.ttf");
        
        var font = new SKFont
        {
            Typeface = SKTypeface.FromStream(cookieFont),
            Size = 16 * Dpi / 72f
        };

        const SKTextAlign textAlign = SKTextAlign.Center;

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
            IsAntialias = true
        };
        
        var index = 0;
        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                if (index >= names.Length) break;
                var text = $"{names[index]}\ndan Keluarga\ndi tempat";
                var x = c * (imgW + padX);
                var y = r * (imgH + padY);

                var rect = new SKRect(x, y, x + imgW, y + imgH);
                var centerX = rect.MidX;
                var lineHeight = font.Size + 4;
                var lines = text.Split('\n');
                var totalHeight = lines.Length * lineHeight;
                var startY = rect.MidY - totalHeight / 2 + font.Size;

                foreach (var line in lines )
                {
                    canvas.DrawText(
                        text: line, 
                        x: centerX, 
                        y: startY,
                        font: font, 
                        paint:paint,
                        textAlign: textAlign);
                    startY += lineHeight;
                }

                if (border) canvas.DrawRect(rect, borderPaint);
                index++;
            }
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
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
