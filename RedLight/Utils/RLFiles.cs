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

    public static void ExportScene(string templatePath, string exportDir, string className, List<Transformable<RLModel>> objectModels)
    {
        Directory.CreateDirectory(exportDir);

        // Read template and remove the first two comments
        var lines = File.ReadAllLines(templatePath).ToList();
        int commentsRemoved = 0;
        for (int i = 0; i < lines.Count && commentsRemoved < 2;)
        {
            if (lines[i].TrimStart().StartsWith("//"))
            {
                lines.RemoveAt(i);
                commentsRemoved++;
            }
            else
            {
                i++;
            }
        }
        string templateContent = string.Join("\n", lines);

        // Replace class name and namespace
        templateContent = Regex.Replace(templateContent, @"\bSceneTemplate\b", className);
        templateContent = templateContent.Replace("namespace RedLight.Resources.Templates;", "namespace RedLight.Resources.Exported;");

        string exportPath = Path.Combine(exportDir, $"{className}.cs");

        // Generate model creation and transform code
        var sb = new StringBuilder();
        for (int i = 0; i < objectModels.Count; i++)
        {
            var model = objectModels[i];
            Matrix4X4.Decompose(model.Model, out var scale, out var rotation, out var translation);
            var euler = QuaternionToEuler(rotation);
            var resourcePath = model.Target.ResourcePath ?? "";
            var modelName = model.Target.Name ?? $"model{i}";
            string creationCode;
            switch (modelName.ToLowerInvariant())
            {
                case "plane":
                    creationCode = $"        var model{i} = new Plane(Graphics, TextureManager, ShaderManager).Default().Model;";
                    break;
                case "cube":
                    creationCode = $"        var model{i} = new Cube(Graphics, TextureManager, ShaderManager).Model;";
                    break;
                case "sphere":
                    creationCode = $"        var model{i} = new Sphere(Graphics, TextureManager, ShaderManager).Model;";
                    break;
                case "cat":
                    creationCode = $"        var model{i} = new Cat(Graphics, TextureManager, ShaderManager).Model;";
                    break;
                default:
                    creationCode = $"        var model{i} = Graphics.CreateModel(@\"{resourcePath}\", TextureManager, ShaderManager, \"{modelName}\");";
                    break;
            }
            sb.AppendLine($"        // Model {i}");
            sb.AppendLine(creationCode);
            sb.AppendLine($"        model{i}.Translate(new Silk.NET.Maths.Vector3D<float>({translation.X}f, {translation.Y}f, {translation.Z}f));");
            sb.AppendLine($"        model{i}.Rotate({euler.X}f, new Silk.NET.Maths.Vector3D<float>(1,0,0));");
            sb.AppendLine($"        model{i}.Rotate({euler.Y}f, new Silk.NET.Maths.Vector3D<float>(0,1,0));");
            sb.AppendLine($"        model{i}.Rotate({euler.Z}f, new Silk.NET.Maths.Vector3D<float>(0,0,1));");
            sb.AppendLine($"        model{i}.Scale(new Silk.NET.Maths.Vector3D<float>({scale.X}f, {scale.Y}f, {scale.Z}f));");
            sb.AppendLine($"        Graphics.AddModels(ObjectModels, controller, model{i});");
        }
        string generatedModelCode = sb.ToString();

        // Insert after the marker line
        string marker = "// Generated contents starts from this line below";
        int markerIndex = templateContent.IndexOf(marker);
        if (markerIndex != -1)
        {
            int insertIndex = markerIndex + marker.Length;
            templateContent = templateContent.Insert(insertIndex, "\n\n" + generatedModelCode);
        }

        File.WriteAllText(exportPath, templateContent);
    }

    private static System.Numerics.Vector3 QuaternionToEuler(Quaternion<float> q)
    {
        // i aint know how to do ts
        float sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
        float cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
        float roll = MathF.Atan2(sinr_cosp, cosr_cosp);

        float sinp = 2 * (q.W * q.Y - q.Z * q.X);
        float pitch;
        if (MathF.Abs(sinp) >= 1)
            pitch = MathF.CopySign(MathF.PI / 2, sinp);
        else
            pitch = MathF.Asin(sinp);

        float siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
        float cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
        float yaw = MathF.Atan2(siny_cosp, cosy_cosp);

        return new System.Numerics.Vector3(roll, pitch, yaw);
    }
}