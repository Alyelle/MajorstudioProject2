using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameTimer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Image image;
    private float time;
    public float totalTime;
    public UnityEvent OnGameTimeout = new();
    private bool disabled = false;
    void SetVisuals() // Maybe replace by a SetTime(time) function that handles everything
    {
        image.fillAmount = time / totalTime;

    }
    void Start()
    {
        time = totalTime;
    }

    // Update is called once per frame
    void Update()
    {
        if (disabled) return;

        time -= Time.deltaTime;
        if (time <= 0)
        {
            OnGameTimeout.Invoke();
            disabled = true;
        }
        else SetVisuals();
    }
}