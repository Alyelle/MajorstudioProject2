using UnityEngine;

public class GameplaySceneManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public MouseMove gameplayCursor;
    public ShakeManager shakeManager;
    void Start()
    {
        gameplayCursor.GetMousePos = shakeManager.GetGameplayCursor;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
