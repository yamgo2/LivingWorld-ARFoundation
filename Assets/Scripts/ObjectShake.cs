using UnityEngine;
using System.Collections;

public class ObjectShake : MonoBehaviour
{
    private Vector3 originScale;
    public float shake_decay;
    public float shake_intensity;
    public bool isShaking = false;

    private float temp_shake_intensity = 0;

    // Remember original scale
    private void Start()
    {
        originScale = transform.localScale;
    }

    // Called when image plane or background is clicked
    public void Shake(float duration)
    {
        // Shake if not already shaking
        if (!isShaking)
        {
            isShaking = true;
            temp_shake_intensity = shake_intensity;
            StartCoroutine(DoShake(duration));
        } else
            isShaking = false;
    }

    // While loop that shrinks gameObject then returns it back to its original size over duration
    private IEnumerator DoShake(float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            transform.localScale = new Vector3(originScale.x - temp_shake_intensity, originScale.y, originScale.z - temp_shake_intensity);
            temp_shake_intensity -= shake_decay;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = originScale;
        isShaking = false;
    }
}