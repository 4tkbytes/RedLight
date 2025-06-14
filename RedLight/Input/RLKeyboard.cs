using Silk.NET.Input;

namespace RedLight.Input;

public interface RLKeyboard
{
    public HashSet<Key> PressedKeys { get; set; }

    void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
    }

    void OnKeyUp(IKeyboard keyboard, Key key, int keyCode)
    {
    }
}