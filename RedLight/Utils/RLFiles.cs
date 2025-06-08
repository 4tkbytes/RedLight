using System.Reflection;

namespace RedLight.Utils;

public static class RLFiles
{
    public static string GetResourcePath(string resourceName)
    {
        // If resourceName is already a full path (starts with drive letter or UNC path)
        if (Path.IsPathRooted(resourceName))
        {
            // Just validate the file exists and return it
            if (!File.Exists(resourceName))
            {
                throw new FileNotFoundException($"Resource not found at path '{resourceName}'.");
            }
            return resourceName;
        }

        // Get the directory of the main executable
        string executableDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)
                                     ?? throw new InvalidOperationException("Unable to determine executable directory.");

        // Handle resource names that include project prefix
        string processedName = resourceName;
        if (resourceName.Contains(".Resources."))
        {
            int index = resourceName.IndexOf(".Resources.") + ".Resources.".Length;
            processedName = resourceName.Substring(index);
        }

        // Convert the processed name to a file path
        string relativePath = processedName.Replace('.', Path.DirectorySeparatorChar);

        // Format the last segment as a filename if needed
        int lastSeparatorIndex = relativePath.LastIndexOf(Path.DirectorySeparatorChar);
        if (lastSeparatorIndex != -1)
        {
            relativePath = relativePath.Substring(0, lastSeparatorIndex) + "." +
                           relativePath.Substring(lastSeparatorIndex + 1);
        }

        // Path directly from executable to Resources folder
        string resourcePath = Path.Combine(executableDirectory, "Resources", relativePath);

        // Check if the resource exists
        if (!File.Exists(resourcePath))
        {
            throw new FileNotFoundException($"Resource '{resourceName}' not found at path '{resourcePath}'.");
        }

        return resourcePath;
    }
    
    public static string GetResourceAsString(string resourceName)
    {
        // Get the path to the resource file
        string resourcePath = GetResourcePath(resourceName);
    
        // Read the entire content of the file as a string
        return File.ReadAllText(resourcePath);
    }
}