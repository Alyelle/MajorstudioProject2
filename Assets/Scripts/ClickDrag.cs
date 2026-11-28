using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class ClickDrag : MonoBehaviour
{

    #region Variables

    // Components
    private RectTransform rectTransform;
    private BoxCollider2D collider;
    private Rigidbody2D rb;

    // Textbox
    private Textbox textbox;

    // States
    private bool isMousePressed;

    // Button (Alex)
    private GameButton button;

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
    }

    void Update()
    {
        // Get Mouse Position
        // Vector2 mousePos = Mouse.current.position.ReadValue();

        // Click to Drag
        if (Mouse.current.leftButton.wasPressedThisFrame && !isMousePressed && textbox != null)
        {
            isMousePressed = true;
            textbox.OnMouseClick();
        }

        // When the Mouse is Held Down, and There's a Textbox
        if (isMousePressed && textbox != null)
        {
            textbox.OnMouseDown(transform.position);
        }
        
        // Check if Mouse Clicks on this Object
        if (Mouse.current.leftButton.wasReleasedThisFrame && isMousePressed)
        {
            if (textbox != null)
            {
                textbox.OnMouseRelease();
            }
            isMousePressed = false;
        }


        // Copy_Paste code for the button. We really should find a way to reuse some code here:
        {
            if (Mouse.current.leftButton.wasPressedThisFrame && !isMousePressed && button != null)
            {
                isMousePressed = true;
                button.Press();
            }

            // When the Mouse is Held Down, and There's a Textbox
            if (isMousePressed && button != null)
            {
                button.Hold(transform.position);
            }
            
            // Check if Mouse Clicks on this Object
            if (Mouse.current.leftButton.wasReleasedThisFrame && isMousePressed)
            {
                if (button != null)
                {
                    button.Release();
                }
                isMousePressed = false;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isMousePressed && collision.GetComponent<Textbox>() != null)
        {
            textbox = collision.GetComponent<Textbox>();
        }

        // Same for button. It might be interesting to see if we can reuse code in some way.
        if (collision.TryGetComponent<GameButton>(out var aux)) // IntelliSense suggested me this formatting
        {
            button = aux;
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!isMousePressed && collision.GetComponent<Textbox>() == textbox)
        {
            textbox = null;
        }

        // Same for button. It might be interesting to see if we can reuse code in some way.
        if (collision.GetComponent<GameButton>() == button)
        {
            button = null;
        }
    }
}
