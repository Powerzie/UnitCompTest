using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ProjectileClone : MonoBehaviour
{
    [SerializeField] Transform anchor;
    [SerializeField] Animator animator;
    [SerializeField] float lifetime = 10f;

    Transform ScaleTarget => anchor != null ? anchor : transform.root;
    float _hitRadius = 1f;
    bool _popping;

    public static float Blend(float value, float small, float big, float from, float to)
    {
        return Mathf.Lerp(small, big, Mathf.InverseLerp(from, to, value));
    }

    public static float HoldRadius(float hold, float radius, float interval, float step, float max, float onHold)
    {
        return radius * Mathf.Min(max, onHold * Mathf.Pow(step, hold / Mathf.Max(0.0001f, interval)));
    }

    public static Vector3 Velocity(Vector3 direction, float shotScale, float speed, float smallSpeed, float bigSpeed)
    {
        Vector3 flat = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (flat.sqrMagnitude < 0.0001f)
            flat = Vector3.forward;

        return flat.normalized * (speed * Blend(shotScale, smallSpeed, bigSpeed, 0.15f, 1f));
    }

    public void ApplyScale(float scale)
    {
        ScaleTarget.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }

    public void Launch(Vector3 velocity, Collider ignore, float hitRadius)
    {
        _hitRadius = hitRadius;
        if (animator == null)
            animator = GetComponent<Animator>();

        if (ignore != null && TryGetComponent(out Collider collider))
            Physics.IgnoreCollision(collider, ignore);

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();

        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        body.WakeUp();
        body.linearVelocity = velocity;
        GameFx.AttachTrail(transform, ScaleTarget.lossyScale.x);
        Destroy(transform.root.gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_popping)
            return;

        if (collision.collider.GetComponentInParent<FlightTarget>() != null)
        {
            Pop();
            return;
        }

        if (collision.collider.GetComponentInParent<Obstacle>() == null)
            return;

        float radius = ScaleTarget.lossyScale.x * 0.5f * _hitRadius;
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            Obstacle obstacle = hit.GetComponentInParent<Obstacle>();
            if (obstacle != null)
                obstacle.Explode();
        }

        collision.collider.GetComponentInParent<Obstacle>().Explode();
        Pop();
    }

    void Pop()
    {
        if (_popping)
            return;

        _popping = true;
        if (TryGetComponent(out Rigidbody body))
        {
            body.linearVelocity = Vector3.zero;
            body.isKinematic = true;
        }

        if (TryGetComponent(out Collider collider))
            collider.enabled = false;

        CancelInvoke();
        TrailRenderer trail = GetComponentInChildren<TrailRenderer>();
        if (trail != null)
            trail.time = 0.1f;
        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.speed = 1f;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.Play("Pop", 0, 0f);
        }
        else
            PopFinished();
    }

    public void PopFinished()
    {
        Destroy(ScaleTarget.gameObject);
    }
}
