using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BlinkManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    float intensity = 0;
    float timer;
    bool disabled = false;


    public float maxTime = 5;
    public GameButton button;
    public Image screen;

    public UnityEvent OnEyesBurnt = new();

    void Start()
    {
        timer = maxTime;
        button.OnPress.AddListener(Reset); // Setting Button Behaviour
    }

    void Reset()
    {
        timer = maxTime;
    }

    void SetIntensity(float _intensity)
    {
        if (_intensity > 1) 
        {
            OnEyesBurnt.Invoke();
            disabled = true;
            return;
        }

        intensity = _intensity;
        // There HAS to be a better way to do this... right?
        screen.color = new Color(screen.color.r, screen.color.g, screen.color.b, _intensity); 
    }



    // Update is called once per frame
    void Update()
    {
        if (disabled==true) return;

        timer -= Time.deltaTime;
        SetIntensity((maxTime-timer)/maxTime);
    }
}
