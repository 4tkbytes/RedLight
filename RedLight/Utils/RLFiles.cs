using System.Reflection;

namespace RedLight.Utils;

public static class RLFiles
{
    /// <summary>
    /// Reads an embedded resource file and returns its contents as a string.
    /// </summary>
    /// <param name="resourceName">The name of the embedded resource.</param>
    /// <returns>The contents of the resource file as a string.</returns>
    
    // Fuck you very much GetEmbeddedResourceString for being such a pain in the ass to deal with 👊
    public static string GetEmbeddedResourceString(string resourceName)
    {
        // Get the executing assembly
        var assembly = Assembly.GetExecutingAssembly();

        // Ensure resourceName is properly formatted with the assembly namespace
        // if not already fully qualified
        if (!resourceName.Contains('.'))
        {
            string assemblyNamespace = assembly.GetName().Name;
            resourceName = $"{assemblyNamespace}.{resourceName}";
        }

        // Try to get the manifest resource stream
        using Stream? stream = assembly.GetManifestResourceStream(resourceName);

        // Handle the case when the resource isn't found
        if (stream == null)
        {
            // Get available resources to help with debugging
            string[] availableResources = assembly.GetManifestResourceNames();

            // Create helpful error message with available resources
            string errorMessage = $"Resource '{resourceName}' not found. Available resources:\n";
            errorMessage += string.Join("\n", availableResources);

            throw new FileNotFoundException(errorMessage);
        }

        // Read and return the resource content
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
    
    /// <summary>
    /// Extracts an embedded resource to a temporary file and returns the path for use with Texture2D.
    /// </summary>
    /// <param name="resourceName">The name of the embedded resource texture.</param>
    /// <returns>The temporary file path where the texture was extracted.</returns>
    public static string GetEmbeddedResourcePath(string resourceName)
    {
        // Get the executing assembly
        var assembly = Assembly.GetExecutingAssembly();
    
        // Ensure resourceName is properly formatted with the assembly namespace
        // if not already fully qualified
        if (!resourceName.Contains('.'))
        {
            string assemblyNamespace = assembly.GetName().Name;
            resourceName = $"{assemblyNamespace}.{resourceName}";
        }
    
        // Try to get the manifest resource stream
        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
    
        // Handle the case when the resource isn't found
        if (stream == null)
        {
            // Get available resources to help with debugging
            string[] availableResources = assembly.GetManifestResourceNames();
    
            // Create helpful error message with available resources
            string errorMessage = $"Resource '{resourceName}' not found. Available resources:\n";
            errorMessage += string.Join("\n", availableResources);
    
            throw new FileNotFoundException(errorMessage);
        }
    
        // Create a unique temporary file name
        string fileExtension = Path.GetExtension(resourceName);
        if (string.IsNullOrEmpty(fileExtension))
        {
            fileExtension = ".png"; // Default to png if no extension is found
        }
        
        string tempFileName = $"{Path.GetFileNameWithoutExtension(resourceName)}_{Guid.NewGuid()}{fileExtension}";
        string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);
    
        // Extract the resource to the temporary file
        using (FileStream fileStream = File.Create(tempFilePath))
        {
            stream.CopyTo(fileStream);
        }
    
        return tempFilePath;
    }
    
    public static byte[] GetEmbeddedResourceBytes(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();

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
        return ms.ToArray();
    }

    public static string GetAbsolutePath(string relativePath)
    {
        // Normalize slashes for cross-platform compatibility
        string normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar)
                                            .Replace('\\', Path.DirectorySeparatorChar);

        // Get the base directory of the application
        string baseDir = AppContext.BaseDirectory;

        // Combine and return the absolute path
        return Path.GetFullPath(Path.Combine(baseDir, normalizedPath));
    }
}