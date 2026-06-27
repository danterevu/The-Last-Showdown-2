using UnityEngine;

public class PresenterEventBridge : MonoBehaviour
{
    [SerializeField] private FinalScreenManager finalScreenManager;

    // Animador intro del presentador
    public void OnIntroFinished() => finalScreenManager.OnIntroFinished();

    // Animador cinematica del presentador
    public void OnLoserWorried() => finalScreenManager.OnLoserWorried();
    public void OnLoserGrabbed() => finalScreenManager.OnLoserGrabbed();
    public void OnReturnToMenu() => finalScreenManager.OnReturnToMenu();
}