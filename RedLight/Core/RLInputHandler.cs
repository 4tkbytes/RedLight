using Silk.NET.Input;
using System.Collections.Generic;
using System.Numerics;

namespace RedLight.Core
{
    public class RLInputHandler
    {
        protected HashSet<Key> PressedKeys = new();
        
        // Mouse state
        public Vector2 MousePosition { get; private set; }
        public Vector2 MouseDelta { get; private set; }
        public bool IsMouseCaptured { get; private set; }
        
        // Mouse buttons
        protected HashSet<MouseButton> PressedMouseButtons = new();
        
        public virtual void OnKeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            PressedKeys.Add(key);
        }

        public virtual void OnKeyUp(IKeyboard keyboard, Key key, int arg3)
        {
            PressedKeys.Remove(key);
        }
        
        public virtual void OnMouseMove(IMouse mouse, Vector2 position)
        {
            // Calculate mouse delta from the last position
            MouseDelta = position - MousePosition;
            MousePosition = position;
        }
        
        public virtual void OnMouseDown(IMouse mouse, MouseButton button)
        {
            PressedMouseButtons.Add(button);
        }
        
        public virtual void OnMouseUp(IMouse mouse, MouseButton button)
        {
            PressedMouseButtons.Remove(button);
        }
        
        public void SetMouseCapture(bool capture)
        {
            IsMouseCaptured = capture;
        }
        
        public void ResetMouseDelta()
        {
            MouseDelta = Vector2.Zero;
        }

        public bool IsKeyDown(Key key) => PressedKeys.Contains(key);
        
        public bool IsMouseButtonDown(MouseButton button) => PressedMouseButtons.Contains(button);
    }
}