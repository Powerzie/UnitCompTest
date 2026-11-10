using System.Collections;
using UnityEngine;

public class ConfettiRain : MonoBehaviour
{
    public void Play(RectTransform canvas)
    {
        StopAllCoroutines();
        StartCoroutine(Rain(canvas));
    }

    IEnumerator Rain(RectTransform canvas)
    {
        for (int i = 0; i < 90; i++)
            GameFx.SpawnConfetti(canvas);

        float elapsed = 0f;
        float spawn = 0f;
        while (elapsed < 5.5f)
        {
            spawn += 95f * Time.deltaTime;
            while (spawn >= 1f)
            {
                spawn -= 1f;
                GameFx.SpawnConfetti(canvas);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
