using Serilog;
using System.IO;
using System.Reflection;

namespace RedLight.Utils;

public struct RLResource
{
    public string ResourceName;
    public string ParentFolder;
}

public static class RLFiles
{
    public static Assembly CheckAndReturnAssembly(string resourceName)
    {
        if (resourceName.Split(".")[0].Contains("RedLight"))
        {
            return Assembly.GetExecutingAssembly();
        }
        else
        {
            return Assembly.GetEntryAssembly();
        }
    }

    public static string GetParentFolder(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        // Handles both file system and resource paths
        var normalized = path.Replace('\\', '/');
        int lastSlash = normalized.LastIndexOf('/');
        if (lastSlash > 0)
            return normalized.Substring(0, lastSlash);

        // If no slash, try dot-separated (for resource names)
        int lastDot = path.LastIndexOf('.');
        if (lastDot > 0)
            return path.Substring(0, lastDot);

        return string.Empty;
    }

    public static Assembly CheckAndReturnAssembly(string resourceName, bool opposite)
    {
        if (resourceName.Split(".")[0].Contains("RedLight"))
        {
            if (opposite)
                return Assembly.GetEntryAssembly();

            return Assembly.GetExecutingAssembly();
        }
        else
        {
            if (opposite)
                return Assembly.GetExecutingAssembly();

            return Assembly.GetEntryAssembly();
        }
    }

    public static RLResource CopyParentFolderToTemp(string resourceName)
    {
        var assembly = CheckAndReturnAssembly(resourceName);

        // Ensure resourceName is fully qualified
        if (!resourceName.Contains('.'))
        {
            string assemblyNamespace = assembly.GetName().Name;
            resourceName = $"{assemblyNamespace}.{resourceName}";
        }

        // Find the parent folder in the resource name
        var resourceParts = resourceName.Split('.');
        if (resourceParts.Length < 2)
            throw new ArgumentException("Resource name must contain at least one folder and a file.");

        // Remove the file name (last part) and extension (second to last part)
        var folderParts = resourceParts.Take(resourceParts.Length - 2).ToArray();
        string folderPrefix = string.Join('.', folderParts);

        // Get all resources in the same folder
        var allResources = assembly.GetManifestResourceNames()
            .Where(r => r.StartsWith(folderPrefix + ".", StringComparison.Ordinal))
            .ToList();

        if (allResources.Count == 0)
        {
            string[] availableResources = assembly.GetManifestResourceNames();

            string[] alsoAvailableResources = CheckAndReturnAssembly(resourceName, true).GetManifestResourceNames();
            string errorMessage = $"Resource '{resourceName}' not found. Available resources:\n";
            errorMessage += string.Join("\n", availableResources);
            errorMessage += string.Join("\n", alsoAvailableResources);

            throw new FileNotFoundException(errorMessage);
        }

        // Create a temp directory
        string tempDir = Path.Combine(Path.GetTempPath(), "RedLight_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);

        // Copy each resource to the temp directory
        foreach (var res in allResources)
        {
            // Convert resource name to real file path (dots to slashes, except for last two parts)
            var resParts = res.Split('.');
            var fileParts = resParts.Skip(folderParts.Length).ToArray();
            string fileName = string.Join('.', fileParts);
            if (fileParts.Length >= 2)
                fileName = string.Join(Path.DirectorySeparatorChar.ToString(), fileParts.Take(fileParts.Length - 2)) +
                           (fileParts.Length > 2 ? Path.DirectorySeparatorChar.ToString() : "") +
                           fileParts[fileParts.Length - 2] + "." + fileParts[fileParts.Length - 1];
            string filePath = Path.Combine(tempDir, fileName);

            // Ensure subdirectories exist
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using Stream? stream = assembly.GetManifestResourceStream(res);
            if (stream == null)
                continue;

            using FileStream fs = File.Create(filePath);
            stream.CopyTo(fs);
        }

        Log.Debug("Copied parent folder of resource [{A}] to temp directory [{B}]", resourceName, tempDir);
        return new RLResource
        {
            ResourceName = resourceName,
            ParentFolder = tempDir
        };
    }

    public static string CopyDirToTempAndGetEmbeddedResource(string resourceName)
    {
        var tempFolder = CopyParentFolderToTemp(resourceName);

        // Extract the real file name from the resource name
        var resourceParts = resourceName.Split('.');
        string fileName = resourceParts[^2] + "." + resourceParts[^1];
        string modelPath = Path.Combine(tempFolder.ParentFolder, fileName);
        return modelPath;
    }

    public static string GetEmbeddedResourceString(string resourceName)
    {
        var assembly = CheckAndReturnAssembly(resourceName);

        if (!resourceName.Contains('.'))
        {
            string assemblyNamespace = assembly.GetName().Name;
            resourceName = $"{assemblyNamespace}.{resourceName}";
        }

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            string[] availableResources = assembly.GetManifestResourceNames();

            string[] alsoAvailableResources = CheckAndReturnAssembly(resourceName, true).GetManifestResourceNames();
            string errorMessage = $"Resource '{resourceName}' not found. Available resources:\n";
            errorMessage += string.Join("\n", availableResources);
            errorMessage += string.Join("\n", alsoAvailableResources);

            throw new FileNotFoundException(errorMessage);
        }

        using StreamReader reader = new(stream);
        Log.Debug("Successfully fetched file information as a string [{A}]", resourceName);
        return reader.ReadToEnd();
    }

    public static string GetEmbeddedResourcePath(string resourceName)
    {
        var assembly = CheckAndReturnAssembly(resourceName);

        if (!resourceName.Contains('.'))
        {
            string assemblyNamespace = assembly.GetName().Name;
            resourceName = $"{assemblyNamespace}.{resourceName}";
        }

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            string[] availableResources = assembly.GetManifestResourceNames();

            string[] alsoAvailableResources = CheckAndReturnAssembly(resourceName, true).GetManifestResourceNames();
            string errorMessage = $"Resource '{resourceName}' not found. Available resources:\n";
            errorMessage += string.Join("\n", availableResources);
            errorMessage += string.Join("\n", alsoAvailableResources);

            throw new FileNotFoundException(errorMessage);
        }

        string fileExtension = Path.GetExtension(resourceName);
        string tempFileName = $"{Path.GetFileNameWithoutExtension(resourceName)}_{Guid.NewGuid()}{fileExtension}";
        string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);

        using (FileStream fileStream = File.Create(tempFilePath))
        {
            stream.CopyTo(fileStream);
        }

        Log.Debug("Successfully fetched file path [{A}]", resourceName);
        return tempFilePath;
    }

    public static byte[] GetEmbeddedResourceBytes(string resourceName)
    {
        var assembly = CheckAndReturnAssembly(resourceName);

        if (!resourceName.Contains('.'))
        {
            var assemblyNamespace = assembly.GetName().Name;
            resourceName = $"{assemblyNamespace}.{resourceName}";
        }

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            var available = string.Join("\n", assembly.GetManifestResourceNames());
            throw new FileNotFoundException($"Resource `{resourceName}` not found. Available resources:\n{available}");
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        Log.Debug("Loaded Embedded Resource as bytes [{A}]", resourceName);
        return ms.ToArray();
    }

    public static string GetAbsolutePath(string relativePath)
    {
        string normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar)
                                            .Replace('\\', Path.DirectorySeparatorChar);

        string baseDir = AppContext.BaseDirectory;

        return Path.GetFullPath(Path.Combine(baseDir, normalizedPath));
    }

    public static string ExtractGltfWithDependencies(string gltfResource, string binResource)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "RedLightGltf_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);

        try
        {
            // Use the helper for both resources
            var assembly = CheckAndReturnAssembly(gltfResource);

            string gltfPath = Path.Combine(tempDir, "scene.gltf");
            using (var gltfStream = assembly.GetManifestResourceStream(gltfResource))
            {
                if (gltfStream == null)
                {
                    Log.Error("Could not find embedded resource: {Resource}", gltfResource);
                    throw new FileNotFoundException($"Embedded resource not found: {gltfResource}");
                }

                using (var fileStream = File.Create(gltfPath))
                {
                    gltfStream.CopyTo(fileStream);
                }
            }

            string binPath = Path.Combine(tempDir, "scene.bin");
            using (var binStream = assembly.GetManifestResourceStream(binResource))
            {
                if (binStream == null)
                {
                    Log.Error("Could not find embedded resource: {Resource}", binResource);
                    throw new FileNotFoundException($"Embedded resource not found: {binResource}");
                }

                using (var fileStream = File.Create(binPath))
                {
                    binStream.CopyTo(fileStream);
                }
            }

            Log.Debug("Extracted GLTF to: {GltfPath}", gltfPath);
            Log.Debug("Extracted BIN to: {BinPath}", binPath);
            Log.Debug("Files in temp directory: {Files}", string.Join(", ", Directory.GetFiles(tempDir)));

            return gltfPath;
        }
        catch (Exception ex)
        {
            Log.Error("Failed to extract GLTF files: {Error}", ex.Message);

            try { Directory.Delete(tempDir, true); } catch { }
            throw;
        }
    }
}