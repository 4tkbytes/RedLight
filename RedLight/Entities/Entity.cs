using System.Numerics;
using RedLight.Graphics;

namespace RedLight.Entities
{
    public class Entity
    {
        public int Id { get; }
        public int ModelId { get; }
        public Transform Transform { get; } = new Transform();
        public int MaterialId { get; set; }

        private static int _nextId = 0;

        public Entity(int modelId, int materialId = 0)
        {
            Id = _nextId++;
            ModelId = modelId;
            MaterialId = materialId;
        }
    }
}