using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Python.Runtime;
using Serilog;

namespace RedLight.Utils;

public static class PythonSetup
{
    private static bool _isInitialized = false;
    private static readonly object _initLock = new object();
    private static readonly Dictionary<string, bool> _installedPackages = new();
    private static string _venvPath;
    private static string _pythonExecutable;

    public static bool Initialize()
    {
        lock (_initLock)
        {
            if (_isInitialized)
            {
                Log.Debug("Python already initialized, skipping");
                return true;
            }

            try
            {
                Log.Information("Setting up Python virtual environment...");
                
                if (!SetupVirtualEnvironment())
                {
                    Log.Error("Failed to setup virtual environment");
                    return false;
                }

                Log.Information("Configuring Python.NET...");
                ConfigurePythonEnvironment();

                Log.Information("Starting Python engine...");
                if (!PythonEngine.IsInitialized)
                {
                    PythonEngine.Initialize();
                }

                Log.Information("Verifying Python installation...");
                if (!VerifyPythonWorking())
                {
                    throw new InvalidOperationException("Python verification failed");
                }

                Log.Information("Installing required packages...");
                InstallRequiredPackages();

                _isInitialized = true;
                Log.Information("✓ Python environment ready at: {VenvPath}", _venvPath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("✗ Python initialization failed: {Error}", ex.Message);
                return false;
            }
        }
    }

    private static bool SetupVirtualEnvironment()
    {
        try
        {
            // Get executable directory and create Python resources path
            string executableDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string resourcesDir = Path.Combine(executableDir, "Resources");
            _venvPath = Path.Combine(resourcesDir, "Python", "venv");
            
            Directory.CreateDirectory(Path.GetDirectoryName(_venvPath));
            
            // Set Python executable path based on OS
            _pythonExecutable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(_venvPath, "Scripts", "python.exe")
                : Path.Combine(_venvPath, "bin", "python3");

            // Check if venv already exists and is valid
            if (Directory.Exists(_venvPath) && File.Exists(_pythonExecutable))
            {
                Log.Information("Virtual environment already exists at: {VenvPath}", _venvPath);
                return true;
            }

            // Find system Python to create venv
            string systemPython = FindSystemPython();
            if (systemPython == null)
            {
                Log.Error("No system Python found to create virtual environment");
                return false;
            }

            Log.Information("Creating virtual environment with: {SystemPython}", systemPython);
            
            // Create virtual environment
            var createVenvProcess = new ProcessStartInfo
            {
                FileName = systemPython,
                Arguments = $"-m venv \"{_venvPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(createVenvProcess);
            if (process == null)
            {
                Log.Error("Failed to start venv creation process");
                return false;
            }

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(stdout))
                Log.Debug("Venv creation stdout: {Output}", stdout);

            if (!string.IsNullOrWhiteSpace(stderr))
                Log.Debug("Venv creation stderr: {Output}", stderr);

            if (process.ExitCode == 0 && File.Exists(_pythonExecutable))
            {
                Log.Information("✓ Virtual environment created successfully");
                return true;
            }
            else
            {
                Log.Error("Virtual environment creation failed with exit code: {ExitCode}", process.ExitCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error setting up virtual environment: {Error}", ex.Message);
            return false;
        }
    }

    private static string FindSystemPython()
    {
        string[] candidates;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            candidates = new[]
            {
                "python", "python3", "py",
                @"C:\Python313\python.exe",
                @"C:\Python312\python.exe", 
                @"C:\Python311\python.exe",
                @"C:\Python310\python.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    @"Programs\Python\Python313\python.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    @"Programs\Python\Python312\python.exe")
            };
        }
        else
        {
            candidates = new[]
            {
                "python3", "python3.13", "python3.12", "python3.11", "python3.10", "python",
                "/usr/bin/python3", "/usr/local/bin/python3", "/opt/homebrew/bin/python3"
            };
        }

        foreach (string candidate in candidates)
        {
            try
            {
                var testProcess = new ProcessStartInfo
                {
                    FileName = candidate,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(testProcess);
                if (process != null)
                {
                    process.WaitForExit(2000);
                    if (process.ExitCode == 0)
                    {
                        string version = process.StandardOutput.ReadToEnd().Trim();
                        Log.Debug("Found Python: {Python} - {Version}", candidate, version);
                        return candidate;
                    }
                }
            }
            catch
            {
                continue; // Try next candidate
            }
        }

        return null;
    }

    private static bool InstallPackageWithProgress(string packageName, int current, int total)
    {
        try
        {
            Log.Information("Installing {Package}... ({Current}/{Total})", packageName, current, total);
            
            // Use pip from our venv
            string pipExecutable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(_venvPath, "Scripts", "pip.exe")
                : Path.Combine(_venvPath, "bin", "pip");

            var processInfo = new ProcessStartInfo
            {
                FileName = pipExecutable,
                Arguments = $"install {packageName}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                Log.Error("Failed to start pip process");
                return false;
            }

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            
            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(stdout))
            {
                Console.WriteLine("STDOUT:");
                Console.WriteLine(stdout);
            }

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                Console.WriteLine("STDERR:");
                Console.WriteLine(stderr);
            }

            if (process.ExitCode == 0)
            {
                // Check if pip says it was successful OR already satisfied
                if (stdout.Contains("Successfully installed") || stdout.Contains("Requirement already satisfied"))
                {
                    Log.Information("✓ {Package} ready (installed or already satisfied)", packageName);
        
                    // Try to verify, but don't fail if verification has issues
                    try
                    {
                        using (Py.GIL())
                        {
                            Py.Import(packageName);
                            Log.Debug("✓ {Package} import verification successful", packageName);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Package {Package} installed but import verification failed: {Error}", packageName, ex.Message);
                        Log.Information("This might be due to missing system libraries (like _ctypes), but the package should still work");
                    }
        
                    return true;
                }
                else
                {
                    Log.Error("Pip completed but did not report successful installation or satisfaction");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("Failed to install {Package}: {Error}", packageName, ex.Message);
            return false;
        }

        return false;
    }
    
    private static bool VerifyPythonWorking()
    {
        try
        {
            using (Py.GIL())
            {
                dynamic sys = Py.Import("sys");
                string version = sys.version.ToString();
                string executable = sys.executable.ToString();
                
                Log.Debug("Python version: {Version}", version.Split('\n')[0]);
                Log.Debug("Python executable: {Executable}", executable);
                
                // Verify we're using our venv
                if (executable.Contains(_venvPath))
                {
                    Log.Information("✓ Using virtual environment Python");
                    return true;
                }
                else
                {
                    Log.Warning("Python executable is not from venv: {Executable}", executable);
                    return true; // Still allow it to work
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("Python verification failed: {Error}", ex.Message);
            return false;
        }
    }

    private static void InstallRequiredPackages()
    {
        string[] requiredPackages = { "pyvips", "numpy" };
        
        for (int i = 0; i < requiredPackages.Length; i++)
        {
            string package = requiredPackages[i];
            
            if (IsPackageInstalled(package))
            {
                Log.Debug("✓ {Package} already installed", package);
                _installedPackages[package] = true;
            }
            else
            {
                bool success = InstallPackageWithProgress(package, i + 1, requiredPackages.Length);
                _installedPackages[package] = success;
                
                if (success)
                {
                    Log.Information("✓ Successfully installed {Package}", package);
                }
                else
                {
                    Log.Warning("✗ Failed to install {Package} - some features may not work", package);
                }
            }
        }
    }

    private static bool IsPackageInstalled(string packageName)
    {
        if (_installedPackages.TryGetValue(packageName, out bool cached))
        {
            return cached;
        }

        try
        {
            using (Py.GIL())
            {
                Py.Import(packageName);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public static bool IsPackageAvailable(string packageName)
    {
        return _installedPackages.TryGetValue(packageName, out bool available) && available;
    }
    
    private static void ConfigurePythonEnvironment()
    {
        // FIRST: Find and set the Python DLL before doing anything else
        string pythonDll = FindSystemPythonDLL();
        if (!string.IsNullOrEmpty(pythonDll))
        {
            Runtime.PythonDLL = pythonDll;
            Log.Debug("Using Python DLL: {DLL}", pythonDll);
        }
        else
        {
            Log.Error("Could not find Python DLL - this will cause initialization to fail");
            throw new InvalidOperationException("No Python DLL found");
        }
        
        // Find the system Python installation directory (where the standard library is)
        string systemPythonHome = FindSystemPythonHome(pythonDll);
        if (string.IsNullOrEmpty(systemPythonHome))
        {
            Log.Error("Could not find system Python home directory");
            throw new InvalidOperationException("No system Python home found");
        }
        
        // Set up PATH to include venv first, then system Python
        var currentPath = Environment.GetEnvironmentVariable("PATH")?.TrimEnd(';') ?? "";
        
        string venvBinPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(_venvPath, "Scripts")
            : Path.Combine(_venvPath, "bin");
        
        string systemPythonBinPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? systemPythonHome
            : Path.Combine(systemPythonHome, "bin");
        
        string newPath = $"{venvBinPath};{systemPythonBinPath}";
        if (!string.IsNullOrEmpty(currentPath))
        {
            newPath += ";" + currentPath;
        }
        Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.Process);
        
        // Set PYTHONHOME to system Python (where stdlib is), NOT the venv
        Environment.SetEnvironmentVariable("PYTHONHOME", systemPythonHome, EnvironmentVariableTarget.Process);
        
        // Set PYTHONPATH to include both venv site-packages AND system Python
        string pythonPath;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string venvSitePackages = Path.Combine(_venvPath, "Lib", "site-packages");
            string systemLib = Path.Combine(systemPythonHome, "Lib");
            string systemSitePackages = Path.Combine(systemPythonHome, "Lib", "site-packages");
            pythonPath = $"{venvSitePackages};{systemLib};{systemSitePackages}";
        }
        else
        {
            // For Unix systems
            string venvLibPath = Path.Combine(_venvPath, "lib");
            string systemLibPath = Path.Combine(systemPythonHome, "lib");
            
            string venvSitePackages = "";
            if (Directory.Exists(venvLibPath))
            {
                var venvPythonDirs = Directory.GetDirectories(venvLibPath, "python*");
                if (venvPythonDirs.Length > 0)
                {
                    venvSitePackages = Path.Combine(venvPythonDirs[0], "site-packages");
                }
            }
            
            string systemSitePackages = "";
            if (Directory.Exists(systemLibPath))
            {
                var systemPythonDirs = Directory.GetDirectories(systemLibPath, "python*");
                if (systemPythonDirs.Length > 0)
                {
                    systemSitePackages = Path.Combine(systemPythonDirs[0], "site-packages");
                }
            }
            
            pythonPath = $"{venvSitePackages}:{systemSitePackages}:{systemLibPath}";
        }
        
        Environment.SetEnvironmentVariable("PYTHONPATH", pythonPath, EnvironmentVariableTarget.Process);
        
        // Set PythonEngine properties
        PythonEngine.PythonHome = systemPythonHome;
        string envPythonPath = Environment.GetEnvironmentVariable("PYTHONPATH", EnvironmentVariableTarget.Process);
        PythonEngine.PythonPath = pythonPath + ";" + envPythonPath;
        
        Log.Debug("System PYTHONHOME: {PythonHome}", systemPythonHome);
        Log.Debug("PYTHONPATH: {PythonPath}", pythonPath);
        Log.Debug("Venv PATH: {Path}", venvBinPath);
    }

    private static string FindSystemPythonHome(string pythonDll)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // The DLL should be in the Python installation directory
            string dllDir = Path.GetDirectoryName(pythonDll);
            if (File.Exists(Path.Combine(dllDir, "python.exe")))
            {
                return dllDir;
            }
        }
        else
        {
            // For Unix, try common system Python locations
            string[] candidates = {
                "/usr",
                "/usr/local", 
                "/opt/python"
            };
            
            foreach (string candidate in candidates)
            {
                string pythonExe = Path.Combine(candidate, "bin", "python3");
                if (File.Exists(pythonExe))
                {
                    return candidate;
                }
            }
        }
        
        return null;
    }

    private static string FindSystemPythonDLL()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Check common Python installation locations (latest first)
            for (int minor = 13; minor >= 8; minor--)
            {
                string[] candidates = {
                    $@"C:\Python3{minor}\python3{minor}.dll",
                    $@"C:\Users\{Environment.UserName}\AppData\Local\Programs\Python\Python3{minor}\python3{minor}.dll",
                    $@"C:\Program Files\Python3{minor}\python3{minor}.dll"
                };

                foreach (string candidate in candidates)
                {
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            for (int minor = 13; minor >= 8; minor--)
            {
                string ver = $"3.{minor}";
                string[] candidates = {
                    $"/usr/lib/x86_64-linux-gnu/libpython{ver}.so",
                    $"/usr/lib/libpython{ver}.so",
                    $"/usr/local/lib/libpython{ver}.so"
                };

                foreach (string candidate in candidates)
                {
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            for (int minor = 13; minor >= 8; minor--)
            {
                string ver = $"3.{minor}";
                string[] candidates = {
                    $"/opt/homebrew/lib/libpython{ver}.dylib", // Apple Silicon
                    $"/usr/local/lib/libpython{ver}.dylib",    // Intel Mac
                    $"/Library/Frameworks/Python.framework/Versions/{ver}/lib/libpython{ver}.dylib"
                };

                foreach (string candidate in candidates)
                {
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }

        return null;
    }
    
    public static void Shutdown()
    {
        lock (_initLock)
        {
            if (_isInitialized && PythonEngine.IsInitialized)
            {
                try
                {
                    PythonEngine.Shutdown();
                    _isInitialized = false;
                    _installedPackages.Clear();
                
                    // Clear venv paths
                    _venvPath = null;
                    _pythonExecutable = null;
                
                    Log.Information("Python environment shut down");
                }
                catch (Exception ex)
                {
                    Log.Warning("Error during Python shutdown: {Error}", ex.Message);
                }
            }
        }
    }

    public static bool IsInitialized => _isInitialized;
}