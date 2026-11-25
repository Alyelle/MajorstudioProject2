using UnityEngine;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(BoxCollider2D))]
public class TextboxHome : MonoBehaviour
{
    private RectTransform rectTransform;
    private BoxCollider2D collider;
    
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        collider = GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = rectTransform.rect.size;
    }
}
