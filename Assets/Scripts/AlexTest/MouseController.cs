using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MouseController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public PenManager penManager; 

    private NewInputTest input;
    void Start()
    {
        
    }

    private void Awake()
    {
        input = new NewInputTest();
    }

    private void OnEnable()
    {
        input.Enable();
    }

    private void OnDisable()
    {
        input.Disable();
    }



    // Update is called once per frame
    void Update()
    {
        // Getting Mouse Position. Source: https://www.youtube.com/watch?v=EWxxFmiWlbc 
        Vector3 mosPos = Mouse.current.position.ReadValue();
        mosPos.z = Camera.main.nearClipPlane;
        Vector3 mosWorldPos = Camera.main.ScreenToWorldPoint(mosPos);
        transform.position = mosWorldPos;
        
        if (Mouse.current.leftButton.isPressed)
        {
            print("mouse clicked");
            penManager.OnClickFrame(Time.deltaTime);
        }

        if (input.Test.Restart.triggered)
        {
            // Application.LoadLevel(Application.loadedLevel);

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    // If it were an API:
    // void UpdatePos()
    // bool WasPressed()
}
