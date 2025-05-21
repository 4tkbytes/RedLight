using System.Reflection;
using System.Text;

namespace RedLight.Utils;

public static class Files
{
    public static string EmbeddedResourceAsString(string resourceName)
    {
        var assembly = Assembly.GetCallingAssembly(); // Or Assembly.GetExecutingAssembly() depending on your needs

        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
            }

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }
}