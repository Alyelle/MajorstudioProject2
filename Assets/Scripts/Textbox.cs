using UnityEngine;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Textbox : MonoBehaviour
{

    #region Variables

    // Components
    private RectTransform rectTransform;
    private BoxCollider2D collider;
    private Rigidbody2D rb;

    // Parent UI Canvas
    private Canvas canvas;

    // TextboxHome
    private TextboxHome textboxHome;

    // States
    private bool isDragging;
    private bool isOverlapping;

#endregion

    void Start()
    {
        // RectTransform
        rectTransform = GetComponent<RectTransform>();

        // Collider
        collider = GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = rectTransform.rect.size;

        // Rigidbody
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Canvas
        canvas = GetComponentInParent<Canvas>();
    }

    void OnTriggerEnter2D(Collider2D collision) 
    {
        if (collision.GetComponent<TextboxHome>() != null)
        {
            isOverlapping = true;
            textboxHome = collision.GetComponent<TextboxHome>();
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<TextboxHome>() != null)
        {
            isOverlapping = false;
            textboxHome = null;
        }
    }

    public void OnMouseClick()
    {
        isDragging = true;
        if (canvas != null)
        {
            // transform.SetParent(canvas.transform, true); //
            transform.SetAsLastSibling(); // brings it to the front of the screen
        }
    }

    public void OnMouseDown(Vector2 mousePos)
    {
        if (isDragging)
        {
            transform.position = mousePos;
        }
    }

    public void OnMouseRelease()
    {
        // If it's overlapping over its home, then snap
        if (isDragging && isOverlapping && textboxHome != null)
        {
            transform.SetParent(textboxHome.transform, false); // false to snap
            transform.localPosition = Vector3.zero;
        }
        isDragging = false;
    }
}
