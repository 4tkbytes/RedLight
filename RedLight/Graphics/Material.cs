using System.Numerics;

namespace RedLight.Graphics
{
    public class Material
    {
        public int Id { get; }
        public string Name { get; set; }
        public Vector4 DiffuseColor { get; set; } = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        public Texture2D? DiffuseTexture { get; set; }

        private static int _nextId = 0;

        public Material(string name = "Default")
        {
            Id = _nextId++;
            Name = name;
        }
    }
}