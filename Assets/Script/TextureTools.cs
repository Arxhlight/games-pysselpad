using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extension methods for Unity's Texture2D class providing resampling and flood fill operations.
/// </summary>
/// <remarks>
/// <para><b>Resampling Methods</b> - Scale textures to arbitrary resolutions using bilinear filtering:</para>
/// <list type="bullet">
///   <item>
///     <term>ResampleAndCrop</term>
///     <description>Scales and centers the image to fill the entire target area, cropping excess.</description>
///   </item>
///   <item>
///     <term>ResampleAndLetterbox</term>
///     <description>Scales and centers the image to fit within the target area, adding letterbox padding.</description>
///   </item>
/// </list>
/// 
/// <para><b>Flood Fill Methods</b> - Fill areas using a 4-direction algorithm (north, east, south, west):</para>
/// <list type="bullet">
///   <item>
///     <term>FloodFillArea</term>
///     <description>Fills connected pixels matching the color at the start point.</description>
///   </item>
///   <item>
///     <term>FloodFillBorder</term>
///     <description>Fills connected pixels until reaching a specified border color.</description>
///   </item>
/// </list>
/// </remarks>
public static class TextureTools
{
    /// <summary>
    /// Resamples a texture to a target size using bilinear filtering.
    /// </summary>
    /// <param name="source">The source texture to be resampled.</param>
    /// <param name="targetWidth">The desired width of the resampled texture.</param>
    /// <param name="targetHeight">The desired height of the resampled texture.</param>
    /// <returns>A new Texture2D object with the resampled image.</returns>
    public static Texture2D ResampleAndCrop(this Texture2D source, int targetWidth, int targetHeight)
    {
        int sourceWidth = source.width;
        int sourceHeight = source.height;
        float sourceAspect = (float)sourceWidth / sourceHeight;
        float targetAspect = (float)targetWidth / targetHeight;
        int xOffset = 0;
        int yOffset = 0;
        float factor = 1;
        if (sourceAspect > targetAspect)
        {
            // crop width
            factor = (float)targetHeight / sourceHeight;
            xOffset = (int)((sourceWidth - sourceHeight * targetAspect) * 0.5f);
        }
        else
        {
            // crop height
            factor = (float)targetWidth / sourceWidth;
            yOffset = (int)((sourceHeight - sourceWidth / targetAspect) * 0.5f);
        }

        Color32[] data = source.GetPixels32();
        Color32[] data2 = new Color32[targetWidth * targetHeight];
        for (int y = 0; y < targetHeight; y++)
        {
            float yPos = y / factor + yOffset;
            int y1 = (int)yPos;
            if (y1 >= sourceHeight)
            {
                y1 = sourceHeight - 1;
                yPos = y1;
            }

            int y2 = y1 + 1;
            if (y2 >= sourceHeight)
                y2 = sourceHeight - 1;
            float fy = yPos - y1;
            y1 *= sourceWidth;
            y2 *= sourceWidth;
            for (int x = 0; x < targetWidth; x++)
            {
                float xPos = x / factor + xOffset;
                int x1 = (int)xPos;
                if (x1 >= sourceWidth)
                {
                    x1 = sourceWidth - 1;
                    xPos = x1;
                }

                int x2 = x1 + 1;
                if (x2 >= sourceWidth)
                    x2 = sourceWidth - 1;
                float fx = xPos - x1;
                var c11 = data[x1 + y1];
                var c12 = data[x1 + y2];
                var c21 = data[x2 + y1];
                var c22 = data[x2 + y2];
                float f11 = (1 - fx) * (1 - fy);
                float f12 = (1 - fx) * fy;
                float f21 = fx * (1 - fy);
                float f22 = fx * fy;
                float r = c11.r * f11 + c12.r * f12 + c21.r * f21 + c22.r * f22;
                float g = c11.g * f11 + c12.g * f12 + c21.g * f21 + c22.g * f22;
                float b = c11.b * f11 + c12.b * f12 + c21.b * f21 + c22.b * f22;
                float a = c11.a * f11 + c12.a * f12 + c21.a * f21 + c22.a * f22;
                int index = x + y * targetWidth;

                data2[index].r = (byte)r;
                data2[index].g = (byte)g;
                data2[index].b = (byte)b;
                data2[index].a = (byte)a;
            }
        }

        var tex = new Texture2D(targetWidth, targetHeight);
        tex.SetPixels32(data2);
        tex.Apply(true);
        return tex;
    }
    
/// <summary>
/// Resamples a texture to a target size using bilinear filtering.
/// </summary>
/// <param name="source">The source texture to be resampled.</param>
/// <param name="targetWidth">The desired width of the resampled texture.</param>
/// <param name="targetHeight">The desired height of the resampled texture.</param>
/// <returns>A new Texture2D object with the resampled image.</returns>
    public static Texture2D ResampleAndLetterbox(this Texture2D source, int targetWidth, int targetHeight,
        Color aBackground)
    {
        int sourceWidth = source.width;
        int sourceHeight = source.height;
        float sourceAspect = (float)sourceWidth / sourceHeight;
        float targetAspect = (float)targetWidth / targetHeight;
        float xOffset = 0;
        float yOffset = 0;
        float factor = 1;
        if (sourceAspect > targetAspect)
        {
            // letterbox height
            factor = (float)targetWidth / sourceWidth;
            yOffset = ((sourceHeight - sourceWidth / targetAspect) * 0.5f);
        }
        else
        {
            //letterbox width
            factor = (float)targetHeight / sourceHeight;
            xOffset = ((sourceWidth - sourceHeight * targetAspect) * 0.5f);
        }

        Color32[] data = source.GetPixels32();
        Color32[] data2 = new Color32[targetWidth * targetHeight];
        Color32 backCol = aBackground;
        for (int y = 0; y < targetHeight; y++)
        {
            float yPos = y / factor + yOffset;
            int y1 = (int)yPos;
            if ((y1 >= sourceHeight) || (y1 < 0))
            {
                int index = y * targetWidth;
                for (int x = 0; x < targetWidth; x++)
                {
                    data2[index + x] = backCol;
                }

                continue;
            }

            int y2 = y1 + 1;
            float fy = yPos - y1;
            if (y2 >= sourceHeight)
                y2 = sourceHeight - 1;
            y1 *= sourceWidth;
            y2 *= sourceWidth;

            for (int x = 0; x < targetWidth; x++)
            {
                int index = x + y * targetWidth;

                float xPos = x / factor + xOffset;
                int x1 = (int)xPos;
                if ((x1 >= sourceWidth) || (x1 < 0))
                {
                    data2[index] = backCol;
                    continue;
                }

                int x2 = x1 + 1;
                if (x2 >= sourceWidth)
                    x2 = sourceWidth - 1;

                float fx = xPos - x1;

                var c11 = data[x1 + y1];
                var c12 = data[x1 + y2];
                var c21 = data[x2 + y1];
                var c22 = data[x2 + y2];
                float f11 = (1 - fx) * (1 - fy);
                float f12 = (1 - fx) * fy;
                float f21 = fx * (1 - fy);
                float f22 = fx * fy;
                float r = c11.r * f11 + c12.r * f12 + c21.r * f21 + c22.r * f22;
                float g = c11.g * f11 + c12.g * f12 + c21.g * f21 + c22.g * f22;
                float b = c11.b * f11 + c12.b * f12 + c21.b * f21 + c22.b * f22;
                float a = c11.a * f11 + c12.a * f12 + c21.a * f21 + c22.a * f22;

                data2[index].r = (byte)r;
                data2[index].g = (byte)g;
                data2[index].b = (byte)b;
                data2[index].a = (byte)a;
            }
        }

        var tex = new Texture2D(targetWidth, targetHeight);
        tex.SetPixels32(data2);
        tex.Apply(true);
        return tex;
    }

    private struct Point
    {
        public short x;
        public short y;

        public Point(short aX, short aY)
        {
            x = aX;
            y = aY;
        }

        public Point(int aX, int aY) : this((short)aX, (short)aY)
        {
        }
    }
/// <summary>
/// Flood fills an area of the texture matching the specified color.
/// </summary>
/// <param name="aTex">The texture to flood fill.</param>
/// <param name="aX">The starting x-coordinate of the flood fill.</param>
/// <param name="aY">The starting y-coordinate of the flood fill.</param>
/// <param name="aFillColor">The color to fill with.</param>
/// <param name="tolerance">Color matching tolerance (0-1). Higher = more lenient matching.</param>
    public static void FloodFillArea(this Texture2D aTex, int aX, int aY, Color aFillColor, float tolerance = 0.05f)
    {
        int w = aTex.width;
        int h = aTex.height;
        Color[] colors = aTex.GetPixels();
        Color refCol = colors[aX + aY * w];
        Queue<Point> nodes = new Queue<Point>();
        nodes.Enqueue(new Point(aX, aY));
        while (nodes.Count > 0)
        {
            Point current = nodes.Dequeue();
            for (int i = current.x; i < w; i++)
            {
                Color C = colors[i + current.y * w];
                if (!ColorsMatch(C, refCol, tolerance) || ColorsMatch(C, aFillColor, tolerance))
                    break;
                colors[i + current.y * w] = aFillColor;
                if (current.y + 1 < h)
                {
                    C = colors[i + current.y * w + w];
                    if (ColorsMatch(C, refCol, tolerance) && !ColorsMatch(C, aFillColor, tolerance))
                        nodes.Enqueue(new Point(i, current.y + 1));
                }

                if (current.y - 1 >= 0)
                {
                    C = colors[i + current.y * w - w];
                    if (ColorsMatch(C, refCol, tolerance) && !ColorsMatch(C, aFillColor, tolerance))
                        nodes.Enqueue(new Point(i, current.y - 1));
                }
            }

            for (int i = current.x - 1; i >= 0; i--)
            {
                Color C = colors[i + current.y * w];
                if (!ColorsMatch(C, refCol, tolerance) || ColorsMatch(C, aFillColor, tolerance))
                    break;
                colors[i + current.y * w] = aFillColor;
                if (current.y + 1 < h)
                {
                    C = colors[i + current.y * w + w];
                    if (ColorsMatch(C, refCol, tolerance) && !ColorsMatch(C, aFillColor, tolerance))
                        nodes.Enqueue(new Point(i, current.y + 1));
                }

                if (current.y - 1 >= 0)
                {
                    C = colors[i + current.y * w - w];
                    if (ColorsMatch(C, refCol, tolerance) && !ColorsMatch(C, aFillColor, tolerance))
                        nodes.Enqueue(new Point(i, current.y - 1));
                }
            }
        }

        aTex.SetPixels(colors);
    }

/// <summary>
/// Flood fills an area of the texture until reaching a specified border color.
/// </summary>
/// <param name="aTex">The texture to flood fill.</param>
/// <param name="aX">The starting x-coordinate of the flood fill.</param>
/// <param name="aY">The starting y-coordinate of the flood fill.</param>
/// <param name="aFillColor">The color to fill with.</param>
/// <param name="aBorderColor">The border color to stop at.</param>
/// <param name="tolerance">Color matching tolerance (0-1). Higher = more lenient matching.</param>
public static void FloodFillBorder(this Texture2D aTex, int aX, int aY, Color aFillColor, Color aBorderColor, float tolerance = 0.05f)
{
    int w = aTex.width;
    int h = aTex.height;
    Color[] colors = aTex.GetPixels();
    byte[] checkedPixels = new byte[colors.Length];
    Queue<Point> nodes = new Queue<Point>();
    nodes.Enqueue(new Point(aX, aY));
    
    while (nodes.Count > 0)
    {
        Point current = nodes.Dequeue();

        for (int i = current.x; i < w; i++)
        {
            if (checkedPixels[i + current.y * w] > 0 || ColorsMatch(colors[i + current.y * w], aBorderColor, tolerance))
                break;
            colors[i + current.y * w] = aFillColor;
            checkedPixels[i + current.y * w] = 1;
            
            if (current.y + 1 < h)
            {
                if (checkedPixels[i + current.y * w + w] == 0 && !ColorsMatch(colors[i + current.y * w + w], aBorderColor, tolerance))
                    nodes.Enqueue(new Point(i, current.y + 1));
            }

            if (current.y - 1 >= 0)
            {
                if (checkedPixels[i + current.y * w - w] == 0 && !ColorsMatch(colors[i + current.y * w - w], aBorderColor, tolerance))
                    nodes.Enqueue(new Point(i, current.y - 1));
            }
        }

        for (int i = current.x - 1; i >= 0; i--)
        {
            if (checkedPixels[i + current.y * w] > 0 || ColorsMatch(colors[i + current.y * w], aBorderColor, tolerance))
                break;
            colors[i + current.y * w] = aFillColor;
            checkedPixels[i + current.y * w] = 1;
            
            if (current.y + 1 < h)
            {
                if (checkedPixels[i + current.y * w + w] == 0 && !ColorsMatch(colors[i + current.y * w + w], aBorderColor, tolerance))
                    nodes.Enqueue(new Point(i, current.y + 1));
            }

            if (current.y - 1 >= 0)
            {
                if (checkedPixels[i + current.y * w - w] == 0 && !ColorsMatch(colors[i + current.y * w - w], aBorderColor, tolerance))
                    nodes.Enqueue(new Point(i, current.y - 1));
            }
        }
    }

    aTex.SetPixels(colors);
}

/// <summary>
/// Checks if two colors match within a tolerance threshold.
/// </summary>
private static bool ColorsMatch(Color a, Color b, float tolerance)
{
    return Mathf.Abs(a.r - b.r) <= tolerance &&
           Mathf.Abs(a.g - b.g) <= tolerance &&
           Mathf.Abs(a.b - b.b) <= tolerance &&
           Mathf.Abs(a.a - b.a) <= tolerance;
}
}