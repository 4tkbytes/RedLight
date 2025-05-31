using Silk.NET.Input;

namespace RedLight.Input;

public interface RLKeyboard
{
    void OnKeyDown(IKeyboard keyboard, Key key, int keyCode);
}