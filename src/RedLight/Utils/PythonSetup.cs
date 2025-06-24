using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Python.Runtime;
using Serilog;

namespace RedLight.Utils;

public static class PythonSetup
{
    private static bool _isInitialized = false;

    public static void Initialize()
    {
        if (_isInitialized)
            return;

        try
        {
            ConfigureEnvironment();

            if (!PythonEngine.IsInitialized)
                PythonEngine.Initialize();

            InstallRequiredPackages();
            _isInitialized = true;
            Log.Information("Python environment initialized successfully");
        }
        catch (Exception ex)
        {
            Log.Error("Failed to initialize Python: {Error}", ex);
            throw;
        }
    }

    public static void Shutdown()
    {
        if (_isInitialized && PythonEngine.IsInitialized)
        {
            PythonEngine.Shutdown();
            _isInitialized = false;
            Log.Information("Python environment shut down");
        }
    }

    private static void ConfigureEnvironment()
    {
        Runtime.PythonDLL = ProbeForPythonDLL();

        // if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PYTHONHOME")))
        // {
        //     string home = FindPythonHome();
        //     if (!string.IsNullOrEmpty(home))
        //     {
        //         Environment.SetEnvironmentVariable("PYTHONHOME", home);
        //         Environment.SetEnvironmentVariable("PYTHONPATH", home);
        //     }
        // }
    }

    private static string ProbeForPythonDLL()
    {
        var candidates = new List<string>();
        for (int minor = 13; minor >= 8; minor--)
        {
            string ver = $"3.{minor}";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                candidates.Add($@"C:\Users\{Environment.UserName}\AppData\Local\Programs\Python\Python3{minor}\python3{minor}.dll");
                candidates.Add($@"C:\Python3{minor}\python3{minor}.dll");
                candidates.Add($@"C:\Program Files\Python3{minor}\python3{minor}.dll");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                candidates.Add($@"/usr/lib/python{ver}/config-{ver}/libpython{ver}.so");
                candidates.Add($@"/usr/lib64/python{ver}/config-{ver}/libpython{ver}.so");
                candidates.Add($@"/usr/local/lib/python{ver}/config-{ver}/libpython{ver}.so");
                candidates.Add($@"/usr/lib/x86_64-linux-gnu/libpython{ver}.so");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                candidates.Add($@"/Library/Frameworks/Python.framework/Versions/{ver}/lib/libpython{ver}.dylib");
                candidates.Add($@"/usr/local/opt/python@{ver}/Frameworks/Python.framework/Versions/{ver}/lib/libpython{ver}.dylib");
            }
        }

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                Log.Debug("Found Python DLL/shared library at: {Path}", path);
                return path;
            }
        }

        Log.Warning("Could not locate Python shared library via known paths — using fallback.");
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "python312.dll" :
               RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "libpython3.dylib" : "libpython3.so";
    }

    private static string FindPythonHome()
    {
        var paths = GetCommonInstallRoots();

        foreach (var root in paths)
        {
            if (Directory.Exists(root) && ContainsPythonExecutable(root))
            {
                Log.Debug("Detected Python home at: {Path}", root);
                return root;
            }
        }

        Log.Warning("Could not auto-detect Python installation path.");
        PromptToInstallPython();
        return null;
    }

    private static List<string> GetCommonInstallRoots()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var roots = new List<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            roots.AddRange(new[]
            {
                @"C:\Python312", @"C:\Python311", @"C:\Python310", @"C:\Python39",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Python\Python312"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Python\Python311"),
                @"C:\ProgramData\Anaconda3", @"C:\tools\miniconda3"
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            roots.AddRange(new[]
            {
                "/usr", "/usr/local", "/opt/python",
                Path.Combine(home, ".local"), Path.Combine(home, "anaconda3"), Path.Combine(home, "miniconda3")
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            roots.AddRange(new[]
            {
                "/usr/local", "/opt/homebrew", "/usr",
                "/System/Library/Frameworks/Python.framework/Versions/Current",
                Path.Combine(home, "miniconda3"), Path.Combine(home, "anaconda3")
            });
        }

        return roots;
    }

    private static bool ContainsPythonExecutable(string basePath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return File.Exists(Path.Combine(basePath, "python.exe"));

        return File.Exists(Path.Combine(basePath, "bin", "python3")) ||
               File.Exists(Path.Combine(basePath, "python3"));
    }

    private static void PromptToInstallPython()
    {
        Console.WriteLine();
        Console.WriteLine("------------------------------------------------------------------------------------");
        Console.WriteLine("Hello! It looks like Python isn't set up on your system.");
        Console.WriteLine("RedLight Engine would like to install it on your behalf.");
        Console.WriteLine("This process is transparent and requires your confirmation.");
        Console.WriteLine("------------------------------------------------------------------------------------");
        Console.Write("Allow us to install Python? (y/n): ");

        string input;
        while (true)
        {
            input = Console.ReadLine()?.Trim().ToLower();
            if (input == "y") break;
            if (input == "n")
            {
                Console.WriteLine("Python is required to continue. Please install it manually.");
                Environment.Exit(1);
            }
            Console.Write("Invalid input. Please enter 'y' or 'n': ");
        }
    }

    private static void InstallRequiredPackages()
    {
        try
        {
            using (Py.GIL())
            {
                // EnsurePackage("pyvips");
                // EnsurePackage("numpy");
            }
        }
        catch (Exception ex)
        {
            Log.Warning("Failed to verify or install packages: {Error}", ex.Message);
        }
    }

    private static void EnsurePackage(string name)
    {
        try
        {
            Py.Import(name);
        }
        catch
        {
            Log.Information("Installing package: {Package}", name);
            dynamic subprocess = Py.Import("subprocess");
            dynamic sys = Py.Import("sys");
            subprocess.check_call(new[] { sys.executable, "-m", "pip", "install", name });
        }
    }
}
