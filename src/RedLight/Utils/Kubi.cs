using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;

namespace RedLight.Utils;

/// <summary>
/// A static library for generating cubemaps from equirectangular images.
/// </summary>
public static class Kubi
{
    /// <summary>
    /// Transform types for cubemap generation
    /// </summary>
    public enum Transform
    {
        /// <summary>Standard cubemap projection</summary>
        Cubemap,
        /// <summary>Equi-angular cubemap (C.Brown 2017) - optimized for VR video</summary>
        EAC,
        /// <summary>Optimized tangent transform (M.Zucker & Y.Higashi 2018)</summary>
        OTC
    }

    /// <summary>
    /// Layout options for cubemap faces
    /// </summary>
    public enum Layout
    {
        /// <summary>Separate face images</summary>
        Separate,
        /// <summary>Horizontal row: +X,-X,+Y,-Y,+Z,-Z</summary>
        Row,
        /// <summary>Vertical column: +X,-X,+Y,-Y,+Z,-Z</summary>
        Column,
        /// <summary>Vertical cross with +Y,-Y on the left</summary>
        CrossLeft,
        /// <summary>Vertical cross with +Y,-Y on the right</summary>
        CrossRight,
        /// <summary>Horizontal cross</summary>
        CrossHorizontal
    }

    /// <summary>
    /// Configuration for cubemap generation
    /// </summary>
    public class KubiOptions
    {
        /// <summary>Edge size of each cube face. If null, defaults to input width / 4</summary>
        public int? Size { get; set; }

        /// <summary>Transform type to apply</summary>
        public Transform Transform { get; set; } = Transform.Cubemap;

        /// <summary>Layout for the output cubemap</summary>
        public Layout Layout { get; set; } = Layout.Separate;

        /// <summary>Face names for separate face output (+X, -X, +Y, -Y, +Z, -Z)</summary>
        public string[] FaceNames { get; set; } = { "0", "1", "2", "3", "4", "5" };

        /// <summary>Custom face order (indices 0-5 for +X, -X, +Y, -Y, +Z, -Z)</summary>
        public int[] Order { get; set; }

        /// <summary>Rotation angles for each face (0, 90, 180, 270 degrees)</summary>
        public int[] Rotations { get; set; }

        /// <summary>Flip the result horizontally</summary>
        public bool FlipHorizontal { get; set; }

        /// <summary>Flip the result vertically</summary>
        public bool FlipVertical { get; set; }
    }

    /// <summary>
    /// Generate a cubemap from an equirectangular image
    /// </summary>
    /// <param name="inputImage">Input equirectangular image</param>
    /// <param name="options">Configuration options</param>
    /// <returns>Array of face images (6 faces) or single combined image based on layout</returns>
    /// <exception cref="ArgumentNullException">Thrown when inputImage is null</exception>
    public static Image<Rgba32>[] GenerateCubemap(Image<Rgba32> inputImage, KubiOptions options = null)
    {
        if (inputImage == null)
            throw new ArgumentNullException(nameof(inputImage));

        options ??= new KubiOptions();

        int size = options.Size ?? inputImage.Width / 4;

        // Generate coordinate maps
        var coordinateMaps = GenerateCoordinateMaps(size, options.Transform);

        // Apply custom order if specified
        if (options.Order != null && options.Order.Length == 6)
        {
            var reorderedMaps = new (float[,] x, float[,] y)[6];
            for (int i = 0; i < 6; i++)
            {
                reorderedMaps[i] = coordinateMaps[options.Order[i]];
            }
            coordinateMaps = reorderedMaps;
        }

        // Generate face images
        var faces = new Image<Rgba32>[6];
        for (int face = 0; face < 6; face++)
        {
            faces[face] = MapFace(inputImage, coordinateMaps[face].x, coordinateMaps[face].y, size);

            // Apply rotation if specified
            if (options.Rotations != null && options.Rotations.Length == 6)
            {
                ApplyRotation(faces[face], options.Rotations[face]);
            }
        }

        // Apply layout
        var result = ApplyLayout(faces, options.Layout, size);

        // Apply flipping
        if (options.FlipHorizontal || options.FlipVertical)
        {
            ApplyFlipping(result, options.FlipHorizontal, options.FlipVertical);
        }

        return result;
    }

    /// <summary>
    /// Generate a cubemap and save to files
    /// </summary>
    /// <param name="inputPath">Path to input equirectangular image</param>
    /// <param name="outputPath">Output path (directory for separate faces, file for combined)</param>
    /// <param name="options">Configuration options</param>
    public static void GenerateCubemap(string inputPath, string outputPath, KubiOptions options = null)
    {
        if (string.IsNullOrEmpty(inputPath))
            throw new ArgumentNullException(nameof(inputPath));
        if (string.IsNullOrEmpty(outputPath))
            throw new ArgumentNullException(nameof(outputPath));

        using var inputImage = Image.Load<Rgba32>(inputPath);
        var faces = GenerateCubemap(inputImage, options);

        options ??= new KubiOptions();

        if (options.Layout == Layout.Separate)
        {
            // Save separate face files
            Directory.CreateDirectory(outputPath);
            string baseName = Path.GetFileNameWithoutExtension(inputPath);
            string extension = Path.GetExtension(inputPath);

            for (int i = 0; i < faces.Length; i++)
            {
                string faceName = options.FaceNames[i];
                string faceFileName = Path.Combine(outputPath, $"{baseName}_{faceName}{extension}");
                faces[i].Save(faceFileName);
                faces[i].Dispose();
            }
        }
        else
        {
            // Save combined image
            faces[0].Save(outputPath);
            faces[0].Dispose();
        }
    }

    private static (float[,] x, float[,] y)[] GenerateCoordinateMaps(int size, Transform transform)
    {
        // Generate linear space
        float[] ls = new float[size];
        for (int i = 0; i < size; i++)
        {
            ls[i] = -1.0f + (2.0f * i) / size;
        }

        // Apply transform
        if (transform == Transform.EAC)
        {
            for (int i = 0; i < size; i++)
            {
                ls[i] = (float)Math.Tan(ls[i] / (4.0 / Math.PI));
            }
        }
        else if (transform == Transform.OTC)
        {
            for (int i = 0; i < size; i++)
            {
                ls[i] = (float)(Math.Tan(ls[i] * 0.8687) / Math.Tan(0.8687));
            }
        }

        // Create meshgrid
        float[,] xv = new float[size, size];
        float[,] yv = new float[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                xv[i, j] = ls[j];
                yv[i, j] = ls[i];
            }
        }

        // Calculate coordinate transformations
        float[,] x0 = new float[size, size];
        float[,] y0 = new float[size, size];
        float[,] x1 = new float[size, size];
        float[,] y1 = new float[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                float x = xv[i, j];
                float y = yv[i, j];

                x0[i, j] = (float)Math.Atan(x);
                y0[i, j] = (float)Math.Atan2(y, Math.Sqrt(1 + x * x));
                x1[i, j] = (float)Math.Atan2(x, y);
                y1[i, j] = (float)Math.Atan(Math.Sqrt(y * y + x * x));
            }
        }

        float piot = (float)(Math.PI / 2);

        // Normalize and create face mappings
        var faces = new (float[,] x, float[,] y)[6];

        // Face 0: +X
        faces[0] = CreateFaceMapping(x0, y0, 3, 1, piot, size);
        // Face 1: -X  
        faces[1] = CreateFaceMapping(x0, y0, 1, 1, piot, size);
        // Face 2: +Y
        faces[2] = CreateFaceMapping(x1, y1, -2, 0, piot, size, true);
        // Face 3: -Y
        faces[3] = CreateFaceMapping(x1, y1, 4, 2, piot, size, true, true);
        // Face 4: +Z
        faces[4] = CreateFaceMapping(x0, y0, 2, 1, piot, size);
        // Face 5: -Z
        faces[5] = CreateFaceMapping(x0, y0, 0, 1, piot, size, false, false, true);

        return faces;
    }

    private static (float[,] x, float[,] y) CreateFaceMapping(float[,] srcX, float[,] srcY,
        float offsetX, float offsetY, float normalizer, int size,
        bool modX = false, bool invertY = false, bool modXOnly = false)
    {
        float[,] x = new float[size, size];
        float[,] y = new float[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                float xVal = srcX[i, j] / normalizer + offsetX;
                float yVal = srcY[i, j] / normalizer + offsetY;

                if (modX || modXOnly)
                {
                    xVal = ((xVal % 4) + 4) % 4; // Ensure positive modulo
                }

                if (invertY)
                {
                    yVal = 2 - yVal;
                }

                if (modXOnly)
                {
                    yVal = srcY[i, j] / normalizer + offsetY;
                }

                x[i, j] = xVal;
                y[i, j] = yVal;
            }
        }

        return (x, y);
    }

    private static Image<Rgba32> MapFace(Image<Rgba32> source, float[,] mapX, float[,] mapY, int size)
    {
        var result = new Image<Rgba32>(size, size);

        int srcWidth = source.Width;
        int srcHeight = source.Height;

        result.ProcessPixelRows(dstAccessor =>
        {
            for (int y = 0; y < size; y++)
            {
                var dstRow = dstAccessor.GetRowSpan(y);

                for (int x = 0; x < size; x++)
                {
                    float srcX = mapX[y, x] * srcWidth / 4.0f;
                    float srcY = mapY[y, x] * srcHeight / 2.0f;

                    // Clamp coordinates
                    srcX = Math.Max(0, Math.Min(srcWidth - 1, srcX));
                    srcY = Math.Max(0, Math.Min(srcHeight - 1, srcY));

                    // Sample the source image using indexer access
                    Rgba32 sampledPixel = SampleBilinear(source, srcX, srcY);
                    dstRow[x] = sampledPixel;
                }
            }
        });

        return result;
    }

    private static Rgba32 SampleBilinear(Image<Rgba32> source, float x, float y)
    {
        int x1 = (int)Math.Floor(x);
        int y1 = (int)Math.Floor(y);
        int x2 = Math.Min(x1 + 1, source.Width - 1);
        int y2 = Math.Min(y1 + 1, source.Height - 1);

        float fx = x - x1;
        float fy = y - y1;

        // Use indexer to access pixels directly
        var p11 = source[x1, y1];
        var p12 = source[x2, y1];
        var p21 = source[x1, y2];
        var p22 = source[x2, y2];

        // Bilinear interpolation
        float r = p11.R * (1 - fx) * (1 - fy) + p12.R * fx * (1 - fy) +
                 p21.R * (1 - fx) * fy + p22.R * fx * fy;
        float g = p11.G * (1 - fx) * (1 - fy) + p12.G * fx * (1 - fy) +
                 p21.G * (1 - fx) * fy + p22.G * fx * fy;
        float b = p11.B * (1 - fx) * (1 - fy) + p12.B * fx * (1 - fy) +
                 p21.B * (1 - fx) * fy + p22.B * fx * fy;
        float a = p11.A * (1 - fx) * (1 - fy) + p12.A * fx * (1 - fy) +
                 p21.A * (1 - fx) * fy + p22.A * fx * fy;

        return new Rgba32((byte)Math.Clamp(r, 0, 255),
                          (byte)Math.Clamp(g, 0, 255),
                          (byte)Math.Clamp(b, 0, 255),
                          (byte)Math.Clamp(a, 0, 255));
    }

    private static void ApplyRotation(Image<Rgba32> image, int rotation)
    {
        switch (rotation)
        {
            case 90:
                image.Mutate(x => x.Rotate(90));
                break;
            case 180:
                image.Mutate(x => x.Rotate(180));
                break;
            case 270:
                image.Mutate(x => x.Rotate(270));
                break;
        }
    }

    private static Image<Rgba32>[] ApplyLayout(Image<Rgba32>[] faces, Layout layout, int size)
    {
        if (layout == Layout.Separate)
            return faces;

        Image<Rgba32> combined = null;

        switch (layout)
        {
            case Layout.Row:
                combined = new Image<Rgba32>(size * 6, size);
                for (int i = 0; i < 6; i++)
                {
                    combined.Mutate(ctx => ctx.DrawImage(faces[i], new Point(i * size, 0), 1.0f));
                    faces[i].Dispose();
                }
                break;

            case Layout.Column:
                combined = new Image<Rgba32>(size, size * 6);
                for (int i = 0; i < 6; i++)
                {
                    combined.Mutate(ctx => ctx.DrawImage(faces[i], new Point(0, i * size), 1.0f));
                    faces[i].Dispose();
                }
                break;

            case Layout.CrossLeft:
                combined = CreateCrossLayout(faces, size, true);
                break;

            case Layout.CrossRight:
                combined = CreateCrossLayout(faces, size, false);
                break;

            case Layout.CrossHorizontal:
                combined = CreateHorizontalCrossLayout(faces, size);
                break;
        }

        return new[] { combined };
    }

    private static Image<Rgba32> CreateCrossLayout(Image<Rgba32>[] faces, int size, bool leftCross)
    {
        var combined = new Image<Rgba32>(size * 4, size * 3);

        if (leftCross)
        {
            // CrossL layout: +Y,-Y on the left
            combined.Mutate(ctx =>
            {
                ctx.DrawImage(faces[1], new Point(0, size), 1.0f);     // -X
                ctx.DrawImage(faces[4], new Point(size, size), 1.0f);  // +Z
                ctx.DrawImage(faces[0], new Point(size * 2, size), 1.0f); // +X
                ctx.DrawImage(faces[5], new Point(size * 3, size), 1.0f); // -Z
                ctx.DrawImage(faces[2], new Point(size, 0), 1.0f);     // +Y
                ctx.DrawImage(faces[3], new Point(size, size * 2), 1.0f); // -Y
            });
        }
        else
        {
            // CrossR layout: +Y,-Y on the right
            combined.Mutate(ctx =>
            {
                ctx.DrawImage(faces[5], new Point(0, size), 1.0f);     // -Z
                ctx.DrawImage(faces[1], new Point(size, size), 1.0f);  // -X
                ctx.DrawImage(faces[4], new Point(size * 2, size), 1.0f); // +Z
                ctx.DrawImage(faces[0], new Point(size * 3, size), 1.0f); // +X
                ctx.DrawImage(faces[2], new Point(size * 2, 0), 1.0f);     // +Y
                ctx.DrawImage(faces[3], new Point(size * 2, size * 2), 1.0f); // -Y
            });
        }

        // Dispose original faces
        foreach (var face in faces)
            face.Dispose();

        return combined;
    }

    private static Image<Rgba32> CreateHorizontalCrossLayout(Image<Rgba32>[] faces, int size)
    {
        var combined = new Image<Rgba32>(size * 3, size * 4);

        combined.Mutate(ctx =>
        {
            ctx.DrawImage(faces[1], new Point(0, size), 1.0f);     // -X
            ctx.DrawImage(faces[4], new Point(size, size), 1.0f);  // +Z
            ctx.DrawImage(faces[0], new Point(size * 2, size), 1.0f); // +X

            // Rotate -Z 180 degrees for horizontal cross
            var rotatedZ = faces[5].Clone();
            rotatedZ.Mutate(x => x.Rotate(180));
            ctx.DrawImage(rotatedZ, new Point(size, size * 3), 1.0f);
            rotatedZ.Dispose();

            ctx.DrawImage(faces[2], new Point(size, 0), 1.0f);     // +Y
            ctx.DrawImage(faces[3], new Point(size, size * 2), 1.0f); // -Y
        });

        // Dispose original faces
        foreach (var face in faces)
            face.Dispose();

        return combined;
    }

    private static void ApplyFlipping(Image<Rgba32>[] images, bool flipHorizontal, bool flipVertical)
    {
        foreach (var image in images)
        {
            if (flipHorizontal && flipVertical)
            {
                image.Mutate(x => x.Rotate(180));
            }
            else if (flipHorizontal)
            {
                image.Mutate(x => x.Flip(FlipMode.Horizontal));
            }
            else if (flipVertical)
            {
                image.Mutate(x => x.Flip(FlipMode.Vertical));
            }
        }
    }
}