public class CS_InputManager
{
    private static CS_InputManager instance;
    public static CS_InputManager readInstance
    {
        get
        {
            if (instance == null)
            {
                instance = new CS_InputManager();
            }
            return instance;
        }
    }

    public CS_CustomInputSystem customInputSystem { private set; get; }

    CS_InputManager()
    {
        customInputSystem = new CS_CustomInputSystem();
        customInputSystem.Enable();
    }

    ~CS_InputManager()
    {
        if (customInputSystem != null)
        {
            customInputSystem.Disable();
        }
    }
}
