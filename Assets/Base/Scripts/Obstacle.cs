using System.Collections;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [SerializeField] Transform anchor;
    [SerializeField] Color hitColor = new Color(1f, 0.75f, 0.15f, 1f);
    [SerializeField] float hitBounce = 1.16f;
    [SerializeField] float hitDuration = 0.18f;
    [SerializeField] float explodeDelay = 0.28f;
    [SerializeField] float explodeBounce = 1.55f;
    [SerializeField] float explodeDuration = 0.22f;

    bool _hit;

    Transform ScaleTarget => anchor != null ? anchor : transform.root;

    void Awake()
    {
        if (TryGetComponent(out Rigidbody body))
        {
            body.isKinematic = true;
            body.useGravity = false;
        }
    }

    public void ApplyScale(float scale)
    {
        ScaleTarget.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }

    public void Explode()
    {
        if (_hit)
            return;

        _hit = true;
        SetColor(hitColor);
        StartCoroutine(HitSequence());
    }

    IEnumerator HitSequence()
    {
        yield return ScalePunch(hitBounce, hitDuration, false);
        yield return new WaitForSeconds(explodeDelay);

        if (TryGetComponent(out Collider collider))
            collider.enabled = false;

        yield return ScalePunch(explodeBounce, explodeDuration, true);
        Destroy(ScaleTarget.gameObject);
    }

    IEnumerator ScalePunch(float peak, float duration, bool collapse)
    {
        Vector3 start = transform.localScale;
        Vector3 top = start * peak;
        Vector3 end = collapse ? Vector3.zero : start;
        float growTime = duration * 0.4f;
        float shrinkTime = Mathf.Max(0.01f, duration - growTime);
        float time = 0f;

        while (time < growTime)
        {
            time += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, top, Mathf.Clamp01(time / growTime));
            yield return null;
        }

        time = 0f;
        while (time < shrinkTime)
        {
            time += Time.deltaTime;
            transform.localScale = Vector3.Lerp(top, end, Mathf.Clamp01(time / shrinkTime));
            yield return null;
        }

        transform.localScale = end;
    }

    void SetColor(Color color)
    {
        if (!TryGetComponent(out Renderer renderer))
            return;

        Material material = renderer.material;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        material.color = color;
    }
}
