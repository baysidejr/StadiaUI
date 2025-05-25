using StadiaUI.Models;

namespace StadiaUI.Services;

public class IntroService
{
    private bool _introCompleted = false;
    public event Action? OnIntroCompleted;

    public bool IsIntroCompleted => _introCompleted;

    public void CompleteIntro()
    {
        _introCompleted = true;
        OnIntroCompleted?.Invoke();
    }
}