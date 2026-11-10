using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Obstacle : MonoBehaviour
{
    [SerializeField] Transform anchor;
    [SerializeField] Animator animator;
    [SerializeField] Color hitColor = new Color(1f, 0.75f, 0.15f, 1f);

    bool _hit;
    float _baseY;
    float _phase;
    Transform ScaleTarget => anchor != null ? anchor : transform.root;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (TryGetComponent(out Rigidbody body))
        {
            body.isKinematic = true;
            body.useGravity = false;
        }

        _baseY = transform.localPosition.y;
        _phase = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        if (_hit)
            return;

        Vector3 position = transform.localPosition;
        position.y = _baseY + Mathf.Sin(Time.time * 1.4f + _phase) * 0.035f;
        transform.localPosition = position;
    }

    public void ApplyScale(float scale)
    {
        ScaleTarget.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }

    public Vector3 Center
    {
        get
        {
            if (TryGetComponent(out Collider collider) && collider.enabled)
                return collider.bounds.center;
            return transform.position;
        }
    }

    public float Radius
    {
        get
        {
            if (TryGetComponent(out Collider collider) && collider.enabled)
                return Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.z);
            return Mathf.Max(0.01f, transform.lossyScale.x * 0.5f);
        }
    }

    public bool OverlapsPath(Vector3 from, Vector3 to, float pathRadius)
    {
        return DistanceToSegment(Center, from, to) <= pathRadius + Radius;
    }

    static float DistanceToSegment(Vector3 point, Vector3 from, Vector3 to)
    {
        Vector2 p = new Vector2(point.x, point.z);
        Vector2 a = new Vector2(from.x, from.z);
        Vector2 b = new Vector2(to.x, to.z);
        Vector2 line = b - a;
        float length = line.sqrMagnitude;
        float t = length < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, line) / length);
        return Vector2.Distance(p, a + line * t);
    }

    public void Explode()
    {
        if (_hit)
            return;

        _hit = true;
        SetColor(hitColor);
        GameFx.HitStop();
        GameFx.ObstacleBurst(transform.position, ScaleTarget.lossyScale.x);
        float delay = Mathf.Max(0f, Random.Range(-0.3f, 0.3f));
        if (delay > 0f)
            Invoke(nameof(Pop), delay);
        else
            Pop();
    }

    void Pop()
    {
        animator.SetTrigger("Die");
        Invoke(nameof(PlayPop), 0.16f);
    }

    public void PlayPop()
    {
        GameAudio.PlayPop();
    }

    public void DisableCollider()
    {
        if (TryGetComponent(out Collider body))
            body.enabled = false;
    }

    public void DieFinished()
    {
        Destroy(ScaleTarget.gameObject);
    }

    void SetColor(Color color)
    {
        if (!TryGetComponent(out Renderer renderer))
            return;

        Material material = renderer.material;
        if (material.HasProperty("_BottomColor"))
            material.SetColor("_BottomColor", color);
        if (material.HasProperty("_TopColor"))
            material.SetColor("_TopColor", color);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        material.color = color;
    }
}
