using UnityEngine;
using UnityEngine.InputSystem;

public class ShakeManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform target;

    public Vector2 GetGameplayCursor() // For the GameManager
    {
        return target.position;
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Mouse.current.position.ReadValue();
    }
}
