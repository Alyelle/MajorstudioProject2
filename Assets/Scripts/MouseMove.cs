using UnityEngine;
using UnityEngine.InputSystem;

public class MouseMove : MonoBehaviour
{
    
    public delegate Vector2 Vector2Getter();  // This looks so awful lol
    public Vector2Getter GetMousePos; // IMPORTANT: Should be set externally in the GameManager

    void LateUpdate()
    {
        Vector2 mousePos = GetMousePos();
        transform.position = mousePos;
    }
}
