using System;
using System.Collections.Generic;
using System.IO;
using Python.Runtime;
using Serilog;

namespace RedLight.Utils;

public static class Kubi
{
    public enum Transform
    {
        None,
        EAC,    // Equi-angular cubemap
        OTC     // Optimized tangent transform
    }

    public enum Layout
    {
        Separate,   // Individual face files (default)
        Row,        // Horizontal strip: +X,-X,+Y,-Y,+Z,-Z
        Column,     // Vertical strip: +X,-X,+Y,-Y,+Z,-Z
        CrossL,     // Vertical cross with +Y,-Y on the left
        CrossR,     // Vertical cross with +Y,-Y on the right
        CrossH      // Horizontal cross
    }

    public enum Flip
    {
        None,
        Horizontal,
        Vertical,
        Both
    }

    public enum Resample
    {
        Nearest,
        Bilinear,
        Bicubic,
        LBB,        // Reduced halo bicubic
        NoHalo,     // Edge sharpening resampler with halo reduction
        VSQBS       // B-Splines with antialiasing smoothing
    }

    public class CubemapOptions
    {
        public string InputPath { get; set; }
        public string OutputPath { get; set; }
        public int? Size { get; set; }
        public Transform Transform { get; set; } = Transform.None;
        public Layout Layout { get; set; } = Layout.Separate;
        public Flip Flip { get; set; } = Flip.None;
        public Resample Resample { get; set; } = Resample.Bilinear;
        public string[] FaceNames { get; set; } = null; // Custom suffixes for faces
        public int[] Order { get; set; } = null; // Custom face order
        public int[] Rotate { get; set; } = null; // Rotation for each face (0, 90, 180, 270)
        public Dictionary<string, object> CreateOptions { get; set; } = new Dictionary<string, object>();
        public string VipsPath { get; set; } = null; // Path to VIPS bin directory
    }

    public static bool GenerateCubemap(CubemapOptions options)
    {
        if (!PythonSetup.IsInitialized)
        {
            Log.Error("Python environment not initialized. Call PythonSetup.Initialize() first.");
            return false;
        }

        try
        {
            using (Py.GIL())
            {
                // Import the kubi module
                dynamic sys = Py.Import("sys");
                string kubiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Python");
                sys.path.append(kubiPath);

                dynamic kubi = Py.Import("Kubi.kubi");
                    
                // Build arguments list
                var args = BuildArgumentsList(options);
                    
                Log.Information("Generating cubemap with options: {Args}", string.Join(" ", args));

                // Call the main kubi function
                dynamic argNamespace = kubi.parse_args(args.ToArray());
                if (argNamespace == null)
                {
                    Log.Error("Invalid arguments for cubemap generation");
                    return false;
                }

                kubi.kubi(argNamespace);
                Log.Information("✓ Cubemap generated successfully");
                return true;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating cubemap");
            return false;
        }
    }
    
    public static bool GenerateDefaultCubemap(string inputPath, string outputPath, int size = 512)
    {
        var options = new CubemapOptions
        {
            InputPath = inputPath,
            OutputPath = outputPath,
            Size = size,
            Transform = Transform.EAC,
            Layout = Layout.Separate,
            Resample = Resample.Bicubic,
            FaceNames = new[] { "right", "left", "top", "bottom", "front", "back" }
        };

        return GenerateCubemap(options);
    }

    public static bool GenerateCubemap(string inputPath, string outputPath, int? size = null, Transform transform = Transform.None)
    {
        var options = new CubemapOptions
        {
            InputPath = inputPath,
            OutputPath = outputPath,
            Size = size,
            Transform = transform
        };

        return GenerateCubemap(options);
    }

    public static bool GenerateIndexFile(string outputPath, int size, Transform transform = Transform.None, Layout layout = Layout.Separate)
    {
        var options = new CubemapOptions
        {
            OutputPath = outputPath,
            Size = size,
            Transform = transform,
            Layout = layout
        };

        return GenerateIndex(options);
    }

    private static bool GenerateIndex(CubemapOptions options)
    {
        if (!PythonSetup.IsInitialized)
        {
            Log.Error("Python environment not initialized");
            return false;
        }

        try
        {
            using (Py.GIL())
            {
                dynamic sys = Py.Import("sys");
                string kubiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Python");
                sys.path.append(kubiPath);

                dynamic kubi = Py.Import("Kubi.kubi");
                    
                var args = BuildIndexArgumentsList(options);
                    
                Log.Information("Generating index file: {Args}", string.Join(" ", args));

                dynamic argNamespace = kubi.parse_args(args.ToArray());
                if (argNamespace == null)
                {
                    Log.Error("Invalid arguments for index generation");
                    return false;
                }

                kubi.kubi(argNamespace);
                Log.Information("✓ Index file generated successfully");
                return true;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating index file");
            return false;
        }
    }

    private static List<string> BuildArgumentsList(CubemapOptions options)
    {
        var args = new List<string>();

        // Input and output
        if (!string.IsNullOrEmpty(options.InputPath))
            args.Add(options.InputPath);
            
        if (!string.IsNullOrEmpty(options.OutputPath))
            args.Add(options.OutputPath);

        // Size
        if (options.Size.HasValue)
        {
            args.Add("-s");
            args.Add(options.Size.Value.ToString());
        }

        // Transform
        if (options.Transform != Transform.None)
        {
            args.Add("-t");
            args.Add(options.Transform.ToString().ToLower());
        }

        // Layout
        if (options.Layout != Layout.Separate)
        {
            args.Add("-l");
            args.Add(GetLayoutString(options.Layout));
        }

        // Flip/Inverse
        if (options.Flip != Flip.None)
        {
            args.Add("-i");
            args.Add(GetFlipString(options.Flip));
        }

        // Resample
        if (options.Resample != Resample.Bilinear)
        {
            args.Add("-r");
            args.Add(GetResampleString(options.Resample));
        }

        // Face names
        if (options.FaceNames != null && options.FaceNames.Length == 6)
        {
            args.Add("-f");
            args.AddRange(options.FaceNames);
        }

        // Order
        if (options.Order != null && options.Order.Length == 6)
        {
            args.Add("--order");
            args.AddRange(options.Order.Select(o => o.ToString()));
        }

        // Rotate
        if (options.Rotate != null && options.Rotate.Length == 6)
        {
            args.Add("--rotate");
            args.AddRange(options.Rotate.Select(r => r.ToString()));
        }

        // Create options
        foreach (var co in options.CreateOptions)
        {
            args.Add("-co");
            args.Add($"{co.Key}={co.Value}");
        }

        // VIPS path
        if (!string.IsNullOrEmpty(options.VipsPath))
        {
            args.Add("--vips");
            args.Add(options.VipsPath);
        }

        return args;
    }

    private static List<string> BuildIndexArgumentsList(CubemapOptions options)
    {
        var args = new List<string>();

        // For index generation, we don't need input file
        args.Add("--io");
        args.Add(options.OutputPath);

        // Size is required for index generation
        args.Add("-s");
        args.Add(options.Size?.ToString() ?? "512");

        // Transform
        if (options.Transform != Transform.None)
        {
            args.Add("-t");
            args.Add(options.Transform.ToString().ToLower());
        }

        // Layout
        if (options.Layout != Layout.Separate)
        {
            args.Add("-l");
            args.Add(GetLayoutString(options.Layout));
        }

        return args;
    }

    private static string GetLayoutString(Layout layout)
    {
        return layout switch
        {
            Layout.Row => "row",
            Layout.Column => "column",
            Layout.CrossL => "crossL",
            Layout.CrossR => "crossR",
            Layout.CrossH => "crossH",
            _ => "none"
        };
    }

    private static string GetFlipString(Flip flip)
    {
        return flip switch
        {
            Flip.Horizontal => "horizontal",
            Flip.Vertical => "vertical",
            Flip.Both => "both",
            _ => "none"
        };
    }

    private static string GetResampleString(Resample resample)
    {
        return resample switch
        {
            Resample.Nearest => "nearest",
            Resample.Bilinear => "bilinear",
            Resample.Bicubic => "bicubic",
            Resample.LBB => "lbb",
            Resample.NoHalo => "nohalo",
            Resample.VSQBS => "vsqbs",
            _ => "bilinear"
        };
    }

    public static bool IsKubiAvailable()
    {
        if (!PythonSetup.IsInitialized)
        {
            Log.Warning("Python environment not initialized for Kubi check");
            return false;
        }

        try
        {
            // Initialize Python engine if not already done
            if (!PythonEngine.IsInitialized)
            {
                try
                {
                    PythonEngine.Initialize();
                    Log.Debug("Python engine initialized for Kubi check");
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to initialize Python engine: {Error}", ex.Message);
                    return false;
                }
            }

            using (Py.GIL())
            {
                // Check if pyvips is available
                try
                {
                    dynamic pyvips = Py.Import("pyvips");
                    Log.Debug("✓ pyvips module imported successfully");
                }
                catch (Exception ex)
                {
                    Log.Error("✗ pyvips module not available: {Error}", ex.Message);
                    return false;
                }

                // Check if numpy is available
                try
                {
                    dynamic numpy = Py.Import("numpy");
                    Log.Debug("✓ numpy module imported successfully");
                }
                catch (Exception ex)
                {
                    Log.Error("✗ numpy module not available: {Error}", ex.Message);
                    return false;
                }

                // Check if Kubi module is available
                try
                {
                    dynamic sys = Py.Import("sys");
                    string kubiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Python");
                    sys.path.append(kubiPath);

                    dynamic kubi = Py.Import("Kubi.kubi");
                    Log.Debug("✓ Kubi module imported successfully");
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error("✗ Kubi module not available: {Error}", ex.Message);
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error checking Kubi availability: {Error}", ex.Message);
            return false;
        }
    }
}