using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace RedLight.Input;

public interface RLMouse
{
    void OnMouseMove(IMouse mouse, Vector2 mousePosition);
}