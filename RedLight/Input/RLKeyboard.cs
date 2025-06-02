using Silk.NET.Input;

namespace RedLight.Input;

public interface RLKeyboard
{
    public HashSet<Key> PressedKeys { get; }

    void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        PressedKeys.Add(key);
    }

    void OnKeyUp(IKeyboard keyboard, Key key, int keyCode)
    {
        PressedKeys.Remove(key);
    }
}