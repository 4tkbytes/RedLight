using RedLight.Graphics;
using Silk.NET.Maths;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace RedLight.Utils;

public static class RLFiles
{
    /// <summary>
    /// This function gets the relative path from the resource name. It takes an input
    /// of resourceName, formatted like "RedLight.Resources.Models.Maxwell.maxwell_the_cat.glb". The function will
    /// then search through the Resources folder to get to {AssemblyExecutionLocation}/Resources/Models/maxwell_the_cat.glb.
    /// 
    /// It will then return the path of that resource, which can be used for other functions to load textures or models or more. 
    /// </summary>
    /// <param name="resourceName"><see cref="string"/> The name of the resource</param>
    /// <returns><see cref="string"/> The location of the resource</returns>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
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
                                     ?? GetExecutableDirectoryFallback();

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

    private static string GetExecutableDirectoryFallback()
    {
        return AppContext.BaseDirectory ??
               throw new InvalidOperationException("Unable to determine executable directory.");
    }

    /// <summary>
    /// This scene fetches the resource as per its resource name. This function is typically used for shaders
    /// to compile them, however they can be used however they like. 
    /// 
    /// The resource name parameter is expected to be of the type "RedLight.Resources.Shaders.basic.vert", where the 
    /// function will travel to {AssemblyExecutionLocation}/Resources/Shaders/basic.vert. 
    /// </summary>
    /// <param name="resourceName"><see cref="string"/></param>
    /// <returns><see cref="string"/> The file as a string</returns>
    public static string GetResourceAsString(string resourceName)
    {
        // Get the path to the resource file
        string resourcePath = GetResourcePath(resourceName);
    
        // Read the entire content of the file as a string
        return File.ReadAllText(resourcePath);
    }
}