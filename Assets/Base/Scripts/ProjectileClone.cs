using System.Collections;
using UnityEngine;

public class ProjectileClone : MonoBehaviour
{
    [SerializeField] Transform anchor;
    [SerializeField] float lifetime = 10f;
    [SerializeField] float hitRadiusMultiplier = 2f;
    [SerializeField] float popBounce = 1.45f;
    [SerializeField] float popDuration = 0.22f;

    Transform ScaleTarget => anchor != null ? anchor : transform.root;
    bool _popping;

    public static Vector3 Velocity(Vector3 direction, float shotScale, float speed, float smallSpeed, float bigSpeed)
    {
        Vector3 flat = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (flat.sqrMagnitude < 0.0001f)
            flat = Vector3.forward;

        float size = Mathf.InverseLerp(0.15f, 1f, shotScale);
        float speedFactor = Mathf.Lerp(smallSpeed, bigSpeed, size);
        return flat.normalized * (speed * speedFactor);
    }

    public void ApplyScale(float scale)
    {
        ScaleTarget.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }

    public void Launch(Vector3 velocity, Collider ignore)
    {
        if (ignore != null && TryGetComponent(out Collider collider))
            Physics.IgnoreCollision(collider, ignore);

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();

        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        body.WakeUp();
        body.linearVelocity = velocity;
        Destroy(transform.root.gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_popping || collision.collider.GetComponentInParent<Obstacle>() == null)
            return;

        float radius = ScaleTarget.lossyScale.x * hitRadiusMultiplier;
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

        StartCoroutine(PopRoutine());
    }

    IEnumerator PopRoutine()
    {
        Vector3 start = transform.localScale;
        Vector3 top = start * popBounce;
        float growTime = popDuration * 0.35f;
        float shrinkTime = Mathf.Max(0.01f, popDuration - growTime);
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
            transform.localScale = Vector3.Lerp(top, Vector3.zero, Mathf.Clamp01(time / shrinkTime));
            yield return null;
        }

        Destroy(ScaleTarget.gameObject);
    }
}
