using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

class Analyze
{
    static void Main()
    {
        Console.WriteLine("=== Screen Analysis ===");
        Console.WriteLine("");

        var bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
        Console.WriteLine("Screen: " + bounds.Width + "x" + bounds.Height);
        Bitmap bmp = new Bitmap(bounds.Width, bounds.Height);
        Graphics g = Graphics.FromImage(bmp);
        g.CopyFromScreen(0, 0, 0, 0, bounds.Size);
        g.Dispose();

        Console.WriteLine("Searching for RED (R>180, G<100, B<100) pixels...");
        int foundRed = 0;
        for (int y = 500; y < 580; y++)
        {
            for (int x = 920; x < 1000; x++)
            {
                var p = bmp.GetPixel(x, y);
                if (p.R > 180 && p.G < 100 && p.B < 100)
                {
                    foundRed++;
                    if (foundRed <= 5)
                        Console.WriteLine("  RED at (" + x + "," + y + "): RGB(" + p.R + "," + p.G + "," + p.B + ")");
                }
            }
        }
        Console.WriteLine("  Total red pixels: " + foundRed);

        Console.WriteLine("");
        Console.WriteLine("Searching for DARK (R+G+B < 300) pixels...");
        int foundDark = 0;
        for (int y = 500; y < 580; y++)
        {
            for (int x = 920; x < 1000; x++)
            {
                var p = bmp.GetPixel(x, y);
                int sum = p.R + p.G + p.B;
                if (sum < 300)
                {
                    foundDark++;
                    if (foundDark <= 5)
                        Console.WriteLine("  DARK at (" + x + "," + y + "): RGB(" + p.R + "," + p.G + "," + p.B + ") sum=" + sum);
                }
            }
        }
        Console.WriteLine("  Total dark pixels: " + foundDark);

        Console.WriteLine("");
        Console.WriteLine("Full scan of window area (928,508)-(992,572):");
        for (int y = 508; y < 572; y += 8)
        {
            for (int x = 928; x < 992; x += 8)
            {
                var p = bmp.GetPixel(x, y);
                Console.WriteLine("  (" + x + "," + y + "): RGB(" + p.R + "," + p.G + "," + p.B + ")");
            }
        }

        bmp.Dispose();
        Console.WriteLine("");
        Console.WriteLine("Done.");
    }
}
