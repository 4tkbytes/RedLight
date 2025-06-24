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

                string pythonExecutable = FindPythonExecutable();
                if (string.IsNullOrEmpty(pythonExecutable))
                {
                    Log.Error("No suitable Python installation found (minimum Python 3.10 required)");
                    return false;
                }

                Log.Information("Using Python: {PythonPath}", pythonExecutable);

                // create env
                if (!SetupVirtualEnvironment(pythonExecutable))
                {
                    Log.Error("Failed to set up virtual environment");
                    return false;
                }

                if (!ConfigurePythonNet())
                {
                    Log.Error("Failed to configure Python.NET");
                    return false;
                }

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
        
        // ms store
        for (int minor = 13; minor >= 10; minor--)
        {
            candidates.Add(Path.Combine(localAppData, $@"Programs\Python\Python3{minor}\python.exe"));
            candidates.Add(Path.Combine(localAppData, $@"Programs\Python\Python31{minor - 10}\python.exe")); // 310, 311, 312, 313
        }

        // python website downloads
        for (int minor = 13; minor >= 10; minor--)
        {
            candidates.Add($@"C:\Python3{minor}\python.exe");
            candidates.Add($@"C:\Python31{minor - 10}\python.exe");
            candidates.Add(Path.Combine(programFiles, $@"Python3{minor}\python.exe"));
            candidates.Add(Path.Combine(programFilesX86, $@"Python3{minor}\python.exe"));
        }

        // anaconda
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

        string pyenvRoot = Environment.GetEnvironmentVariable("PYENV_ROOT") ?? Path.Combine(userProfile, ".pyenv");
        if (Directory.Exists(Path.Combine(pyenvRoot, "versions")))
        {
            var versions = Directory.GetDirectories(Path.Combine(pyenvRoot, "versions"));
            foreach (var version in versions.OrderByDescending(v => v))
            {
                candidates.Add(Path.Combine(version, "python.exe"));
            }
        }

        // wsl
        candidates.AddRange(new[]
        {
            @"C:\Windows\System32\wsl.exe python3",
            @"C:\Windows\System32\wsl.exe python3.13",
            @"C:\Windows\System32\wsl.exe python3.12",
            @"C:\Windows\System32\wsl.exe python3.11",
            @"C:\Windows\System32\wsl.exe python3.10"
        });

        // path
        candidates.AddRange(new[] { "python", "python3", "py" });

        return candidates;
    }

    private static List<string> GetLinuxPythonCandidates()
    {
        var candidates = new List<string>();
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // linux downloaded
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
        
        // conda
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
        
        // yucky snap
        for (int minor = 13; minor >= 10; minor--)
        {
            candidates.Add($"/snap/bin/python3.{minor}");
        }

        // chad flatpak (but bad coz of restrictions)
        candidates.Add("flatpak run org.python.Python");

        // any other locations possible
        candidates.AddRange(new[] { "python3", "python", "/usr/bin/python3", "/usr/local/bin/python3" });

        return candidates;
    }

    private static List<string> GetMacOSPythonCandidates()
    {
        var candidates = new List<string>();
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        
        // homebrew
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
        
        // python website
        for (int minor = 13; minor >= 10; minor--)
        {
            candidates.AddRange(new[]
            {
                $"/Library/Frameworks/Python.framework/Versions/3.{minor}/bin/python3",
                $"/usr/local/Frameworks/Python.framework/Versions/3.{minor}/bin/python3"
            });
        }

        // anaconda
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
        // For virtual environments, we need to use the DLL from the virtual environment's base Python
        try
        {
            // First, get the base executable path from the virtual environment
            var psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = "-c \"import sys; print(sys.base_exec_prefix)\"",
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
                    string basePrefix = process.StandardOutput.ReadToEnd().Trim();
                    
                    // Try to find the DLL in the base Python installation
                    var possibleDlls = new[]
                    {
                        Path.Combine(basePrefix, "python313.dll"),
                        Path.Combine(basePrefix, "python312.dll"),
                        Path.Combine(basePrefix, "python311.dll"),
                        Path.Combine(basePrefix, "python310.dll")
                    };

                    foreach (var dll in possibleDlls)
                    {
                        if (File.Exists(dll))
                        {
                            Log.Debug("Found Python DLL via base_exec_prefix: {DllPath}", dll);
                            return dll;
                        }
                    }
                }
            }

            // Fallback: try to get DLL path directly
            var fallbackPsi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = "-c \"import sys; import os; print(os.path.join(sys.base_exec_prefix, f'python{sys.version_info.major}{sys.version_info.minor}.dll'))\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var fallbackProcess = Process.Start(fallbackPsi);
            if (fallbackProcess != null)
            {
                fallbackProcess.WaitForExit(5000);
                if (fallbackProcess.ExitCode == 0)
                {
                    string dllPath = fallbackProcess.StandardOutput.ReadToEnd().Trim();
                    if (File.Exists(dllPath))
                    {
                        Log.Debug("Found Python DLL via fallback method: {DllPath}", dllPath);
                        return dllPath;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error getting Python DLL path from virtual environment");
        }

        // Original fallback code for non-venv scenarios
        var pythonDir = Path.GetDirectoryName(pythonExe);
        var searchDirs = new[]
        {
            pythonDir,
            Path.GetDirectoryName(pythonDir),
            Environment.SystemDirectory,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysWOW64")
        };

        foreach (var dir in searchDirs.Where(Directory.Exists))
        {
            var dllFiles = Directory.GetFiles(dir, "python*.dll", SearchOption.TopDirectoryOnly);
            
            var preferredDll = dllFiles
                .Where(dll => Path.GetFileName(dll).StartsWith("python3") && !Path.GetFileName(dll).Contains("_"))
                .OrderByDescending(Path.GetFileName)
                .FirstOrDefault();

            if (preferredDll != null)
            {
                Log.Debug("Found Python DLL in {Dir}: {DllPath}", dir, preferredDll);
                return preferredDll;
            }
        }

        Log.Warning("Could not locate Python DLL for {PythonExe}", pythonExe);
        return null;
    }

    private static void ActivateVirtualEnvironment()
    {
        try
        {
            // Set VIRTUAL_ENV environment variable
            Environment.SetEnvironmentVariable("VIRTUAL_ENV", _venvPath);
            
            // Update PATH to include the virtual environment's Scripts/bin directory
            string venvBinPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(_venvPath, "Scripts")
                : Path.Combine(_venvPath, "bin");
                
            string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
            string newPath = $"{venvBinPath}{Path.PathSeparator}{currentPath}";
            Environment.SetEnvironmentVariable("PATH", newPath);
            
            // Unset PYTHONHOME if it's set (this can interfere with virtual environments)
            Environment.SetEnvironmentVariable("PYTHONHOME", null);
            
            // Set PYTHONPATH to include the virtual environment's site-packages
            string sitePackages = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(_venvPath, "Lib", "site-packages")
                : Path.Combine(_venvPath, "lib", "python3.13", "site-packages"); // Adjust version as needed
                
            Environment.SetEnvironmentVariable("PYTHONPATH", sitePackages);
            
            Log.Information("Virtual environment activated:");
            Log.Information("  VIRTUAL_ENV: {VirtualEnv}", Environment.GetEnvironmentVariable("VIRTUAL_ENV"));
            Log.Information("  PYTHONPATH: {PythonPath}", Environment.GetEnvironmentVariable("PYTHONPATH"));
            Log.Information("  Updated PATH with: {VenvBin}", venvBinPath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error activating virtual environment");
        }
    }

    private static string GetVenvPythonPath()
    {
        var paths = new List<string>();
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Virtual environment paths (highest priority)
            paths.Add(Path.Combine(_venvPath, "Lib", "site-packages"));
            paths.Add(Path.Combine(_venvPath, "Lib"));
            paths.Add(Path.Combine(_venvPath, "Scripts"));
            
            // Get the Python version to find the correct standard library
            try
            {
                string pythonExe = Path.Combine(_venvPath, "Scripts", "python.exe");
                var psi = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = "-c \"import sys; print(f'{sys.version_info.major}.{sys.version_info.minor}')\"",
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
                        string version = process.StandardOutput.ReadToEnd().Trim();
                        
                        // Add base Python standard library (but not its site-packages)
                        var baseLibPsi = new ProcessStartInfo
                        {
                            FileName = pythonExe,
                            Arguments = "-c \"import sys; print(sys.base_exec_prefix)\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };

                        using var baseProcess = Process.Start(baseLibPsi);
                        if (baseProcess != null)
                        {
                            baseProcess.WaitForExit(5000);
                            if (baseProcess.ExitCode == 0)
                            {
                                string basePrefix = baseProcess.StandardOutput.ReadToEnd().Trim();
                                var baseLib = Path.Combine(basePrefix, "Lib");
                                if (Directory.Exists(baseLib))
                                {
                                    paths.Add(baseLib);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Could not detect Python version for path setup: {Error}", ex.Message);
            }
        }
        else
        {
            // Linux/Mac virtual environment paths
            var libDir = Path.Combine(_venvPath, "lib");
            if (Directory.Exists(libDir))
            {
                var pythonDirs = Directory.GetDirectories(libDir, "python3.*");
                if (pythonDirs.Length > 0)
                {
                    var pythonLibDir = pythonDirs.OrderByDescending(d => d).First();
                    paths.Add(Path.Combine(pythonLibDir, "site-packages"));
                    paths.Add(pythonLibDir);
                }
            }
            paths.Add(Path.Combine(_venvPath, "bin"));
        }

        var validPaths = paths.Where(Directory.Exists).ToList();
        return string.Join(Path.PathSeparator.ToString(), validPaths);
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

            // Activate the virtual environment by setting environment variables
            ActivateVirtualEnvironment();

            string pythonExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(_venvPath, "Scripts", "python.exe")
                : Path.Combine(_venvPath, "bin", "python3");

            // Set Python.NET to use the virtual environment's Python executable
            Runtime.PythonDLL = GetPythonDLL(pythonExe);
            PythonEngine.Initialize();
            PythonEngine.PythonHome = _venvPath;
            PythonEngine.PythonPath = GetVenvPythonPath();

            Log.Information("Python.NET configured for virtual environment:");
            Log.Information("  Python DLL: {PythonDLL}", Runtime.PythonDLL ?? "Auto-detect");
            Log.Information("  Python Home: {PythonHome}", PythonEngine.PythonHome);
            Log.Information("  Python Path: {PythonPath}", PythonEngine.PythonPath);

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
        try
        {
            // Create requirements.txt content
            string requirementsContent = RLFiles.GetResourceAsString("RedLight.Resources.Python.requirements.txt");

            string requirementsPath = Path.Combine(_venvPath, "requirements.txt");
            File.WriteAllText(requirementsPath, requirementsContent);

            Log.Information("Checking package requirements...");

            // First, do a dry-run to see what needs to be installed
            var missingPackages = CheckMissingPackages(requirementsPath);
        
            if (missingPackages.Count == 0)
            {
                Log.Information("✓ All required packages are already installed");
                return true;
            }

            Log.Information("Installing {Count} missing packages: {Packages}", 
                missingPackages.Count, string.Join(", ", missingPackages));

            // Install all missing packages in one command
            return InstallFromRequirements(requirementsPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during package installation");
            return false;
        }
    }
    
    private static List<string> CheckMissingPackages(string requirementsPath)
    {
        var missingPackages = new List<string>();
        
        try
        {
            string pipExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(_venvPath, "Scripts", "pip.exe")
                : Path.Combine(_venvPath, "bin", "pip3");

            // Use pip install --dry-run to check what would be installed
            var psi = new ProcessStartInfo
            {
                FileName = pipExe,
                Arguments = $"install --dry-run --quiet --requirement \"{requirementsPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                process.WaitForExit(30000);
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                if (process.ExitCode == 0)
                {
                    // Parse dry-run output to find packages that would be installed
                    var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (line.Trim().StartsWith("Would install"))
                        {
                            // Extract package names from "Would install package1 package2 ..."
                            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 2; i < parts.Length; i++) // Skip "Would" and "install"
                            {
                                var packageInfo = parts[i].Trim();
                                if (!string.IsNullOrEmpty(packageInfo))
                                {
                                    // Extract just the package name (before version info)
                                    var packageName = packageInfo.Split('-', '=', '>', '<')[0];
                                    if (!missingPackages.Contains(packageName))
                                    {
                                        missingPackages.Add(packageName);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Log.Debug("Dry-run check failed, falling back to individual package checks: {Error}", error.Trim());
                    // Fallback: check each package individually
                    return CheckPackagesIndividually();
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to check packages with dry-run, falling back to individual checks");
            return CheckPackagesIndividually();
        }

        return missingPackages;
    }

    private static List<string> CheckPackagesIndividually()
    {
        string[] requiredPackages = { "pyvips", "numpy", "pillow" };
        var missingPackages = new List<string>();

        string pipExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(_venvPath, "Scripts", "pip.exe")
            : Path.Combine(_venvPath, "bin", "pip3");

        foreach (string package in requiredPackages)
        {
            if (!IsPackageInstalled(pipExe, package))
            {
                missingPackages.Add(package);
            }
        }

        return missingPackages;
    }

    private static bool IsPackageInstalled(string pipExe, string packageName)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = pipExe,
                Arguments = $"show {packageName}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                process.WaitForExit(10000);
                return process.ExitCode == 0;
            }
        }
        catch (Exception ex)
        {
            Log.Debug("Error checking package {Package}: {Error}", packageName, ex.Message);
        }

        return false;
    }

    private static bool InstallFromRequirements(string requirementsPath)
    {
        try
        {
            string pipExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(_venvPath, "Scripts", "pip.exe")
                : Path.Combine(_venvPath, "bin", "pip3");

            Log.Information("Installing packages from requirements.txt...");

            var psi = new ProcessStartInfo
            {
                FileName = pipExe,
                Arguments = $"install --requirement \"{requirementsPath}\" --upgrade",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                process.WaitForExit(120000); // 2 minute timeout for installation

                if (process.ExitCode == 0)
                {
                    Log.Information("✓ All packages installed successfully");
                    return true;
                }
                else
                {
                    string error = process.StandardError.ReadToEnd();
                    string output = process.StandardOutput.ReadToEnd();
                    Log.Error("Package installation failed: {Error}", error);
                    Log.Debug("Installation output: {Output}", output);
                    return false;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error installing packages from requirements");
            return false;
        }
    }

    private static bool VerifyPackageInstallation()
    {
        try
        {
            string pythonExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(_venvPath, "Scripts", "python.exe")
                : Path.Combine(_venvPath, "bin", "python3");

            string testScript = @"
    try:
        import pyvips
        import numpy
        import PIL
        print('SUCCESS: All packages imported successfully')
    except ImportError as e:
        print(f'ERROR: {e}')
        exit(1)
    ";

            var psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"-c \"{testScript}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                process.WaitForExit(10000);
                string output = process.StandardOutput.ReadToEnd();
                
                if (process.ExitCode == 0 && output.Contains("SUCCESS"))
                {
                    Log.Information("✓ Package verification successful");
                    return true;
                }
                else
                {
                    string error = process.StandardError.ReadToEnd();
                    Log.Warning("Package verification failed: {Error}", error);
                    return false;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error during package verification");
            return false;
        }
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
    
    public static bool StartPythonEngine()
    {
        lock (_lock)
        {
            if (!_isInitialized)
            {
                Log.Warning("Python setup not initialized");
                return false;
            }

            if (PythonEngine.IsInitialized)
                return true;

            try
            {
                PythonEngine.Initialize();
                Log.Information("✓ Python engine started successfully");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to start Python engine");
                return false;
            }
        }
    }
    
    public static void DiagnosePythonEnvironment()
    {
        if (!_isInitialized)
        {
            Log.Warning("Python environment not initialized");
            return;
        }

        try
        {
            string pythonExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(_venvPath, "Scripts", "python.exe")
                : Path.Combine(_venvPath, "bin", "python3");

            // List installed packages
            string pipExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(_venvPath, "Scripts", "pip.exe")
                : Path.Combine(_venvPath, "bin", "pip3");

            var psi = new ProcessStartInfo
            {
                FileName = pipExe,
                Arguments = "list",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                process.WaitForExit(10000);
                if (process.ExitCode == 0)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    Log.Information("Installed Python packages:\n{Packages}", output);
                }
                else
                {
                    string error = process.StandardError.ReadToEnd();
                    Log.Error("Failed to list packages: {Error}", error);
                }
            }

            // Test specific imports
            var testScript = RLFiles.GetResourceAsString("RedLight.Resources.Python.envtest.py");

            var testPsi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"-c \"{testScript}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var testProcess = Process.Start(testPsi);
            if (testProcess != null)
            {
                testProcess.WaitForExit(10000);
                string output = testProcess.StandardOutput.ReadToEnd();
                string error = testProcess.StandardError.ReadToEnd();
                
                Log.Information("Python import test:\n{Output}", output);
                if (!string.IsNullOrEmpty(error))
                {
                    Log.Warning("Python test errors:\n{Error}", error);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error diagnosing Python environment");
        }
    }

    public static bool ReinstallPackages()
    {
        if (!_isInitialized)
        {
            Log.Error("Python environment not initialized");
            return false;
        }

        Log.Information("Attempting to reinstall Python packages...");
        return InstallRequiredPackages();
    }
}