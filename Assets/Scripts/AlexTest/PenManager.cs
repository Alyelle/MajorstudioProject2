using UnityEditor;
using UnityEngine;

public class PenManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject spread;
    public float spreadDelay;

    float timer = 0;
    // void Start()
    // {
        
    // }

    // // Update is called once per frame
    // void Update()
    // {
        
    // }

    public void OnClickFrame(float delta)
    {
        timer += delta;
        if (timer >= spreadDelay)
        {
            timer = 0;
            GameObject newSpread = GameObjectUtility.DuplicateGameObject(spread);
            newSpread.transform.position = transform.position;
        }
        
        
    }
}
