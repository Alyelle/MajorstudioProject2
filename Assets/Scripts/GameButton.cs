using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(BoxCollider2D))]
// [RequireComponent(typeof(Rigidbody2D))]
public class GameButton : MonoBehaviour
{
    
    public UnityEvent OnPress = new();


// Copy + pasted from Textbox;
    private RectTransform rectTransform;
    private BoxCollider2D collider;
    private Rigidbody2D rb;
    void Start()
    {
        // RectTransform
        rectTransform = GetComponent<RectTransform>();

        // Collider
        collider = GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = rectTransform.rect.size;

        // // Rigidbody
        // rb = GetComponent<Rigidbody2D>();
        // rb.bodyType = RigidbodyType2D.Kinematic;

    }

    // // Update is called once per frame
    // void Update()
    // {
        
    // }


    public void Press()
    {
        OnPress.Invoke();
    }

    public void Hold(Vector2 pos)
    {
        
    }

    public void Release()
    {
        
    }
}
