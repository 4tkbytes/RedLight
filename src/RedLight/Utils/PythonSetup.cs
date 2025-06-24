using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Python.Runtime;
using Serilog;

namespace RedLight.Utils;

public static class PythonSetup
{
    private static bool _isInitialized = false;
    private static string _pythonHome = null;
    private static string _venvPath = null;
    private static readonly object _lock = new object();

    public static bool Initialize()
    {
        lock (_lock)
        {
            if (_isInitialized)
                return true;

            try
            {
                Log.Information("Initializing Python environment...");

                // Find suitable Python installation
                string pythonExecutable = FindPythonExecutable();
                if (string.IsNullOrEmpty(pythonExecutable))
                {
                    Log.Error("No suitable Python installation found (minimum Python 3.10 required)");
                    return false;
                }

                Log.Information("Using Python: {PythonPath}", pythonExecutable);

                // Set up virtual environment
                if (!SetupVirtualEnvironment(pythonExecutable))
                {
                    Log.Error("Failed to set up virtual environment");
                    return false;
                }

                // Configure Python.NET
                if (!ConfigurePythonNet())
                {
                    Log.Error("Failed to configure Python.NET");
                    return false;
                }

                // Install required packages
                if (!InstallRequiredPackages())
                {
                    Log.Warning("Some packages failed to install, but continuing...");
                }

                _isInitialized = true;
                Log.Information("✓ Python environment initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize Python environment");
                return false;
            }
        }
    }

    private static string FindPythonExecutable()
    {
        List<string> candidates = new List<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            candidates = GetWindowsPythonCandidates();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            candidates = GetLinuxPythonCandidates();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            candidates = GetMacOSPythonCandidates();
        }
        else
        {
            Log.Warning("Unsupported operating system");
            return null;
        }

        Log.Debug("Searching for Python in {Count} locations...", candidates.Count);

        foreach (string candidate in candidates)
        {
            if (IsValidPythonInstallation(candidate))
            {
                Log.Information("✓ Found valid Python installation: {Path}", candidate);
                return candidate;
            }
        }

        return null;
    }

    private static List<string> GetWindowsPythonCandidates()
    {
        var candidates = new List<string>();
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        // Microsoft Store Python installations (User scope) - Most common on modern Windows
        for (int minor = 13; minor >= 10; minor--)
        {
            candidates.Add(Path.Combine(localAppData, $@"Programs\Python\Python3{minor}\python.exe"));
            candidates.Add(Path.Combine(localAppData, $@"Programs\Python\Python31{minor - 10}\python.exe")); // 310, 311, 312, 313
        }

        // System-wide Python.org installations
        for (int minor = 13; minor >= 10; minor--)
        {
            candidates.Add($@"C:\Python3{minor}\python.exe");
            candidates.Add($@"C:\Python31{minor - 10}\python.exe");
            candidates.Add(Path.Combine(programFiles, $@"Python3{minor}\python.exe"));
            candidates.Add(Path.Combine(programFilesX86, $@"Python3{minor}\python.exe"));
        }

        // Anaconda/Miniconda installations
        candidates.AddRange(new[]
        {
            Path.Combine(userProfile, @"miniconda3\python.exe"),
            Path.Combine(userProfile, @"anaconda3\python.exe"),
            Path.Combine(userProfile, @"mambaforge\python.exe"),
            Path.Combine(userProfile, @"miniforge3\python.exe"),
            @"C:\ProgramData\miniconda3\python.exe",
            @"C:\ProgramData\anaconda3\python.exe",
            @"C:\ProgramData\mambaforge\python.exe",
            @"C:\ProgramData\miniforge3\python.exe",
            Path.Combine(programFiles, @"Anaconda3\python.exe"),
            Path.Combine(programFiles, @"Miniconda3\python.exe")
        });

        // PyEnv-win installations
        string pyenvRoot = Environment.GetEnvironmentVariable("PYENV_ROOT") ?? Path.Combine(userProfile, ".pyenv");
        if (Directory.Exists(Path.Combine(pyenvRoot, "versions")))
        {
            var versions = Directory.GetDirectories(Path.Combine(pyenvRoot, "versions"));
            foreach (var version in versions.OrderByDescending(v => v))
            {
                candidates.Add(Path.Combine(version, "python.exe"));
            }
        }

        // Windows Subsystem for Linux Python (if accessible)
        candidates.AddRange(new[]
        {
            @"C:\Windows\System32\wsl.exe python3",
            @"C:\Windows\System32\wsl.exe python3.13",
            @"C:\Windows\System32\wsl.exe python3.12",
            @"C:\Windows\System32\wsl.exe python3.11",
            @"C:\Windows\System32\wsl.exe python3.10"
        });

        // PATH-based executables (last resort)
        candidates.AddRange(new[] { "python", "python3", "py" });

        return candidates;
    }

    private static List<string> GetLinuxPythonCandidates()
    {
        var candidates = new List<string>();
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // System Python installations
        for (int minor = 13; minor >= 10; minor--)
        {
            candidates.AddRange(new[]
            {
                $"python3.{minor}",
                $"/usr/bin/python3.{minor}",
                $"/usr/local/bin/python3.{minor}",
                $"/opt/python3.{minor}/bin/python3",
                $"/opt/python/3.{minor}/bin/python3"
            });
        }

        // Conda installations
        candidates.AddRange(new[]
        {
            Path.Combine(home, "miniconda3/bin/python"),
            Path.Combine(home, "anaconda3/bin/python"),
            Path.Combine(home, "mambaforge/bin/python"),
            Path.Combine(home, "miniforge3/bin/python"),
            "/opt/miniconda3/bin/python",
            "/opt/anaconda3/bin/python",
            "/opt/mambaforge/bin/python",
            "/opt/miniforge3/bin/python"
        });

        // PyEnv installations
        string pyenvRoot = Environment.GetEnvironmentVariable("PYENV_ROOT") ?? Path.Combine(home, ".pyenv");
        if (Directory.Exists(Path.Combine(pyenvRoot, "versions")))
        {
            var versions = Directory.GetDirectories(Path.Combine(pyenvRoot, "versions"));
            foreach (var version in versions.OrderByDescending(v => v))
            {
                candidates.Add(Path.Combine(version, "bin", "python3"));
                candidates.Add(Path.Combine(version, "bin", "python"));
            }
        }

        // Snap packages
        for (int minor = 13; minor >= 10; minor--)
        {
            candidates.Add($"/snap/bin/python3.{minor}");
        }

        // Flatpak
        candidates.Add("flatpak run org.python.Python");

        // Generic fallbacks
        candidates.AddRange(new[] { "python3", "python", "/usr/bin/python3", "/usr/local/bin/python3" });

        return candidates;
    }

    private static List<string> GetMacOSPythonCandidates()
    {
        var candidates = new List<string>();
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Homebrew installations (most common on macOS)
        for (int minor = 13; minor >= 10; minor--)
        {
            candidates.AddRange(new[]
            {
                $"/opt/homebrew/bin/python3.{minor}",  // Apple Silicon
                $"/usr/local/bin/python3.{minor}",     // Intel Mac
                $"/opt/homebrew/opt/python@3.{minor}/bin/python3",
                $"/usr/local/opt/python@3.{minor}/bin/python3"
            });
        }

        // Python.org framework installations
        for (int minor = 13; minor >= 10; minor--)
        {
            candidates.AddRange(new[]
            {
                $"/Library/Frameworks/Python.framework/Versions/3.{minor}/bin/python3",
                $"/usr/local/Frameworks/Python.framework/Versions/3.{minor}/bin/python3"
            });
        }

        // Conda installations
        candidates.AddRange(new[]
        {
            Path.Combine(home, "miniconda3/bin/python"),
            Path.Combine(home, "anaconda3/bin/python"),
            Path.Combine(home, "mambaforge/bin/python"),
            Path.Combine(home, "miniforge3/bin/python"),
            "/opt/miniconda3/bin/python",
            "/opt/anaconda3/bin/python"
        });

        // PyEnv installations
        string pyenvRoot = Environment.GetEnvironmentVariable("PYENV_ROOT") ?? Path.Combine(home, ".pyenv");
        if (Directory.Exists(Path.Combine(pyenvRoot, "versions")))
        {
            var versions = Directory.GetDirectories(Path.Combine(pyenvRoot, "versions"));
            foreach (var version in versions.OrderByDescending(v => v))
            {
                candidates.Add(Path.Combine(version, "bin", "python3"));
                candidates.Add(Path.Combine(version, "bin", "python"));
            }
        }

        // MacPorts
        for (int minor = 13; minor >= 10; minor--)
        {
            candidates.Add($"/opt/local/bin/python3.{minor}");
        }

        // Xcode Command Line Tools Python (usually older, but check anyway)
        candidates.Add("/usr/bin/python3");

        // Generic fallbacks
        candidates.AddRange(new[] { "python3", "python", "/usr/local/bin/python3", "/opt/homebrew/bin/python3" });

        return candidates;
    }

    private static bool IsValidPythonInstallation(string pythonPath)
    {
        try
        {
            if (Path.IsPathRooted(pythonPath) && !File.Exists(pythonPath))
                return false;

            var psi = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = "-c \"import sys; import ctypes; print(f'{sys.version_info.major}.{sys.version_info.minor}')\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                process.WaitForExit(5000);
                if (process.ExitCode == 0)
                {
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    if (Version.TryParse(output, out Version version) && version >= new Version(3, 10))
                    {
                        Log.Debug("✓ Valid Python {Version} found at: {Path}", output, pythonPath);
                        return true;
                    }
                    else if (version != null)
                    {
                        Log.Debug("✗ Python {Version} is below minimum requirement (3.10) at: {Path}", output, pythonPath);
                    }
                }
                else
                {
                    string error = process.StandardError.ReadToEnd();
                    Log.Debug("Python validation failed for {Path}: {Error}", pythonPath, error.Trim());
                }
            }
        }
        catch (Exception ex)
        {
            Log.Debug("Error testing Python at {Path}: {Error}", pythonPath, ex.Message);
        }

        return false;
    }

    private static string GetPythonDLL(string pythonExe)
    {
        try
        {
            Log.Debug("Detecting Python DLL for: {PythonExe}", pythonExe);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsPythonDLL(pythonExe);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxPythonLibrary(pythonExe);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetMacOSPythonLibrary(pythonExe);
            }

            Log.Warning("Unsupported platform for Python DLL detection");
            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error detecting Python DLL/library");
            return null;
        }
    }

    private static string GetWindowsPythonDLL(string pythonExe)
    {
        // Try to get DLL path from Python itself
        var psi = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = "-c \"import sys; import os; print(os.path.join(sys.exec_prefix, f'python{sys.version_info.major}{sys.version_info.minor}.dll'))\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process != null)
        {
            process.WaitForExit(5000);
            if (process.ExitCode == 0)
            {
                string dllPath = process.StandardOutput.ReadToEnd().Trim();
                if (File.Exists(dllPath))
                {
                    Log.Debug("Found Python DLL via sys.exec_prefix: {DllPath}", dllPath);
                    return dllPath;
                }
            }
        }

        // Fallback: search common locations
        var candidates = new List<string>();
        for (int minor = 13; minor >= 10; minor--)
        {
            candidates.Add($@"C:\Users\{Environment.UserName}\AppData\Local\Programs\Python\Python3{minor}\python3{minor}.dll");
            candidates.Add($@"C:\Python3{minor}\python3{minor}.dll");
            candidates.Add($@"C:\Program Files\Python3{minor}\python3{minor}.dll");
            candidates.Add($@"C:\Program Files (x86)\Python3{minor}\python3{minor}.dll");
                
            // Also check for the standard naming convention
            candidates.Add($@"C:\Users\{Environment.UserName}\AppData\Local\Programs\Python\Python3{minor}\python{3}{minor}.dll");
            candidates.Add($@"C:\Python3{minor}\python{3}{minor}.dll");
            candidates.Add($@"C:\Program Files\Python3{minor}\python{3}{minor}.dll");
            candidates.Add($@"C:\Program Files (x86)\Python3{minor}\python{3}{minor}.dll");
        }

        // Check each candidate
        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                Log.Debug("Found Python DLL: {DllPath}", candidate);
                return candidate;
            }
        }

        // Additional fallback: search near the python executable
        var pythonDir = Path.GetDirectoryName(pythonExe);
        var searchDirs = new[]
        {
            pythonDir,
            Path.GetDirectoryName(pythonDir), // Parent directory
            Environment.SystemDirectory, // System32
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysWOW64")
        };

        foreach (var dir in searchDirs.Where(Directory.Exists))
        {
            var dllFiles = Directory.GetFiles(dir, "python*.dll", SearchOption.TopDirectoryOnly);
                
            // Prefer python3X.dll over pythonXY.dll
            var preferredDll = dllFiles
                .Where(dll => Path.GetFileName(dll).StartsWith("python3") && !Path.GetFileName(dll).Contains("_"))
                .OrderByDescending(Path.GetFileName)
                .FirstOrDefault();

            if (preferredDll != null)
            {
                Log.Debug("Found Python DLL in {Dir}: {DllPath}", dir, preferredDll);
                return preferredDll;
            }

            // Fallback to any python dll
            var anyDll = dllFiles.FirstOrDefault();
            if (anyDll != null)
            {
                Log.Debug("Found fallback Python DLL in {Dir}: {DllPath}", dir, anyDll);
                return anyDll;
            }
        }

        Log.Warning("Could not locate Python DLL for {PythonExe}", pythonExe);
        return null;
    }

    private static string GetLinuxPythonLibrary(string pythonExe)
    {
        // Get Python version and library path
        var psi = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = "-c \"import sys, sysconfig; print(sysconfig.get_config_var('LIBDIR')); print(f'{sys.version_info.major}.{sys.version_info.minor}')\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process != null)
        {
            process.WaitForExit(5000);
            if (process.ExitCode == 0)
            {
                var lines = process.StandardOutput.ReadToEnd().Trim().Split('\n');
                if (lines.Length >= 2)
                {
                    string libDir = lines[0].Trim();
                    string version = lines[1].Trim();

                    var possibleLibs = new[]
                    {
                        Path.Combine(libDir, $"libpython{version}.so"),
                        Path.Combine(libDir, $"libpython{version}m.so"),
                        $"/usr/lib/x86_64-linux-gnu/libpython{version}.so",
                        $"/usr/lib/libpython{version}.so",
                        $"/usr/local/lib/libpython{version}.so"
                    };

                    foreach (var lib in possibleLibs)
                    {
                        if (File.Exists(lib))
                        {
                            Log.Debug("Found Python library: {LibPath}", lib);
                            return lib;
                        }
                    }
                }
            }
        }

        Log.Warning("Could not locate Python shared library for {PythonExe}", pythonExe);
        return null;
    }

    private static string GetMacOSPythonLibrary(string pythonExe)
    {
        // Get Python library path
        var psi = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = "-c \"import sys, sysconfig; print(sysconfig.get_config_var('LIBDIR')); print(f'{sys.version_info.major}.{sys.version_info.minor}')\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process != null)
        {
            process.WaitForExit(5000);
            if (process.ExitCode == 0)
            {
                var lines = process.StandardOutput.ReadToEnd().Trim().Split('\n');
                if (lines.Length >= 2)
                {
                    string libDir = lines[0].Trim();
                    string version = lines[1].Trim();

                    var possibleLibs = new[]
                    {
                        Path.Combine(libDir, $"libpython{version}.dylib"),
                        Path.Combine(libDir, $"libpython{version}m.dylib"),
                        $"/usr/local/lib/libpython{version}.dylib",
                        $"/opt/homebrew/lib/libpython{version}.dylib",
                        $"/Library/Frameworks/Python.framework/Versions/{version}/lib/libpython{version}.dylib"
                    };

                    foreach (var lib in possibleLibs)
                    {
                        if (File.Exists(lib))
                        {
                            Log.Debug("Found Python library: {LibPath}", lib);
                            return lib;
                        }
                    }
                }
            }
        }

        Log.Warning("Could not locate Python shared library for {PythonExe}", pythonExe);
        return null;
    }

    // ... rest of the existing methods remain the same (SetupVirtualEnvironment, ConfigurePythonNet, etc.)
        
    private static bool SetupVirtualEnvironment(string pythonExecutable)
    {
        try
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RedLight", "python_env");

            _venvPath = appDataPath;
            Directory.CreateDirectory(Path.GetDirectoryName(appDataPath));

            if (IsValidVirtualEnvironment(appDataPath))
            {
                Log.Information("✓ Using existing virtual environment: {VenvPath}", appDataPath);
                return true;
            }

            if (Directory.Exists(appDataPath))
            {
                Log.Information("Removing existing virtual environment...");
                Directory.Delete(appDataPath, true);
            }

            Log.Information("Creating virtual environment at: {VenvPath}", appDataPath);

            var psi = new ProcessStartInfo
            {
                FileName = pythonExecutable,
                Arguments = $"-m venv \"{appDataPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                process.WaitForExit(30000);
                if (process.ExitCode == 0)
                {
                    Log.Information("✓ Virtual environment created successfully");
                    return true;
                }
                else
                {
                    string error = process.StandardError.ReadToEnd();
                    Log.Error("Failed to create virtual environment: {Error}", error);
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error setting up virtual environment");
            return false;
        }
    }

    private static bool IsValidVirtualEnvironment(string venvPath)
    {
        try
        {
            string pythonExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(venvPath, "Scripts", "python.exe")
                : Path.Combine(venvPath, "bin", "python3");

            return File.Exists(pythonExe) && IsValidPythonInstallation(pythonExe);
        }
        catch
        {
            return false;
        }
    }

    private static bool ConfigurePythonNet()
    {
        try
        {
            if (string.IsNullOrEmpty(_venvPath))
            {
                Log.Error("Virtual environment path not set");
                return false;
            }

            string pythonExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(_venvPath, "Scripts", "python.exe")
                : Path.Combine(_venvPath, "bin", "python3");

            string pythonDll = GetPythonDLL(pythonExe);
            if (!string.IsNullOrEmpty(pythonDll))
            {
                Runtime.PythonDLL = pythonDll;
                PythonEngine.Initialize();
            }

            PythonEngine.PythonHome = _venvPath;
            PythonEngine.PythonPath = GetPythonPath(_venvPath);

            Log.Information("Python.NET configured:");
            Log.Information("  Python DLL: {PythonDLL}", Runtime.PythonDLL ?? "Auto-detect");
            Log.Information("  Python Home: {PythonHome}", PythonEngine.PythonHome);

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to configure Python.NET");
            return false;
        }
    }

    private static string GetPythonPath(string pythonHome)
    {
        var paths = new List<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            paths.Add(Path.Combine(pythonHome, "Lib"));
            paths.Add(Path.Combine(pythonHome, "Lib", "site-packages"));
            paths.Add(Path.Combine(pythonHome, "Scripts"));
        }
        else
        {
            // Try to detect the actual Python version directory
            var libDir = Path.Combine(pythonHome, "lib");
            if (Directory.Exists(libDir))
            {
                var pythonDirs = Directory.GetDirectories(libDir, "python3.*");
                if (pythonDirs.Length > 0)
                {
                    var pythonLibDir = pythonDirs.OrderByDescending(d => d).First();
                    paths.Add(pythonLibDir);
                    paths.Add(Path.Combine(pythonLibDir, "site-packages"));
                }
            }
            paths.Add(Path.Combine(pythonHome, "bin"));
        }

        return string.Join(Path.PathSeparator.ToString(), paths.Where(Directory.Exists));
    }

    private static bool InstallRequiredPackages()
    {
        string[] packages = { "pyvips", "numpy", "pillow" };
        bool allSucceeded = true;

        foreach (string package in packages)
        {
            Log.Information("Installing {Package}...", package);
                
            if (InstallPackage(package))
            {
                Log.Information("✓ {Package} installed successfully", package);
            }
            else
            {
                Log.Warning("✗ Failed to install {Package}", package);
                allSucceeded = false;
            }
        }

        return allSucceeded;
    }

    private static bool InstallPackage(string packageName)
    {
        try
        {
            string pipExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(_venvPath, "Scripts", "pip.exe")
                : Path.Combine(_venvPath, "bin", "pip3");

            var psi = new ProcessStartInfo
            {
                FileName = pipExe,
                Arguments = $"install {packageName} --upgrade",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                process.WaitForExit(60000);
                return process.ExitCode == 0;
            }

            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error installing package {Package}", packageName);
            return false;
        }
    }

    public static bool IsInitialized => _isInitialized;

    public static void Shutdown()
    {
        lock (_lock)
        {
            if (_isInitialized && PythonEngine.IsInitialized)
            {
                try
                {
                    AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);
                    PythonEngine.Shutdown();
                    AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", false);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error during Python engine shutdown");
                }
            }

            _isInitialized = false;
        }
    }
}