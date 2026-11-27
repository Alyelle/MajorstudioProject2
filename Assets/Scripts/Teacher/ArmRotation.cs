using UnityEngine;
using System.Collections;

public class ArmRotation : MonoBehaviour
{
    [SerializeField] private float angle = 20f;
    [SerializeField] private float speed = 80f;
    [SerializeField] private float pause = 0.3f;

    private IEnumerator Start()
    {
        Quaternion original = transform.localRotation;
        int direction = 1;

        while (true)
        {
            Quaternion target = original * Quaternion.Euler(0, 0, angle * direction);
            
            while (Quaternion.Angle(transform.localRotation, target) > 0.01f)
            {
                //Debug.Log(target);
                transform.localRotation = Quaternion.RotateTowards(transform.localRotation, target, speed * Time.deltaTime);
                yield return null;
            }

            yield return new WaitForSeconds(pause);
            direction *= -1;
        }
    }
}