namespace RedLight.UI;

public class UIManager
{
    private static readonly Lazy<UIManager> _lazyInstance = 
        new Lazy<UIManager>(() => new UIManager());
    private UIManager()
    { }
    public static UIManager Instance => _lazyInstance.Value;
    
    }