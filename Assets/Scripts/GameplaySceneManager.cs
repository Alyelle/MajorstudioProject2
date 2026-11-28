using UnityEngine;
using UnityEngine.InputSystem;

public class GameplaySceneManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public MouseMove gameplayCursor;
    public ShakeManager shakeManager;
    public GameTimer gameTimer;
    public BlinkManager blinkManager;
    void Start()
    {
        if (shakeManager != null)
        {
            gameplayCursor.GetMousePos = shakeManager.GetGameplayCursor;
        }
        else gameplayCursor.GetMousePos = Mouse.current.position.ReadValue;
        
        gameTimer.OnGameTimeout.AddListener(GameLost);
        blinkManager.OnEyesBurnt.AddListener(GameLost);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void GameLost()
    {
        print("you lose lol");
    }
}
