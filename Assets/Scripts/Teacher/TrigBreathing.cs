using UnityEngine;

public class Breathing : MonoBehaviour
{
    [SerializeField] private float amp = 0.05f;
    [SerializeField] private float speed = 2f;

    private Vector3 originalPos;

    private void Start()
    {
        originalPos = transform.localPosition;
    }

    private void Update()
    {
        float offset = Mathf.Sin(Time.time * speed) * amp;

        transform.localPosition = originalPos + new Vector3(0, offset, 0);
    }
}