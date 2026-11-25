using UnityEngine;
using UnityEngine.InputSystem;

public class MouseMove : MonoBehaviour
{
    void LateUpdate()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        transform.position = mousePos;
    }
}
