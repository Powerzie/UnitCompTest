using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ObstacleZone : MonoBehaviour
{
    [SerializeField] GameObject obstaclePrefab;
    [SerializeField] int obstacleCount = 10;
    [SerializeField] float minScale = 0.5f;
    [SerializeField] float maxScale = 1.2f;
    [SerializeField] float extraChance = 0.33f;

    BoxCollider _zone;
    Transform _root;

    void Awake()
    {
        _zone = GetComponent<BoxCollider>();
        _zone.isTrigger = true;

        GameObject root = new GameObject("Obstacles");
        root.transform.SetParent(transform, false);
        _root = root.transform;
    }

    void Start()
    {
        if (obstaclePrefab == null)
        {
            Debug.LogWarning("Assign the Obsticle prefab on " + name, this);
            return;
        }

        int placed = 0;
        int attempts = 0;
        while (placed < obstacleCount && attempts < obstacleCount * 12)
        {
            attempts++;
            if (Spawn())
                placed++;
        }
    }

    bool Spawn()
    {
        Vector3 size = _zone.size;
        Vector3 local = _zone.center + new Vector3(
            Random.Range(-size.x * 0.5f, size.x * 0.5f),
            -size.y * 0.5f,
            Random.Range(-size.z * 0.5f, size.z * 0.5f));
        return Place(transform.TransformPoint(local), Random.Range(minScale, maxScale));
    }

    bool Place(Vector3 world, float scale)
    {
        if (obstaclePrefab == null)
            return false;

        if (!InsideZone(world) || OverlapsObstacle(world, scale) || InsideTargetClearance(world, scale))
            return false;

        GameObject instance = Instantiate(obstaclePrefab, world, Quaternion.identity);
        instance.transform.SetParent(_root, true);
        Obstacle obstacle = instance.GetComponent<Obstacle>();
        if (obstacle == null)
            obstacle = instance.GetComponentInChildren<Obstacle>();

        if (obstacle == null)
        {
            Debug.LogWarning("Obsticle prefab is missing an Obstacle component.", obstaclePrefab);
            Destroy(instance);
            return false;
        }

        obstacle.ApplyScale(scale);
        Physics.SyncTransforms();
        TrySpawnExtra(world, scale);
        return true;
    }

    void TrySpawnExtra(Vector3 around, float aroundScale)
    {
        if (Random.value >= extraChance)
            return;

        for (int i = 0; i < 8; i++)
        {
            float scale = Random.Range(minScale, maxScale);
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = (aroundScale + scale) * 0.5f;
            Vector3 world = around + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
            world.y = around.y;
            if (Place(world, scale))
                return;
        }
    }

    bool InsideZone(Vector3 world)
    {
        Vector3 local = transform.InverseTransformPoint(world) - _zone.center;
        Vector3 extents = _zone.size * 0.5f;
        return Mathf.Abs(local.x) <= extents.x && Mathf.Abs(local.z) <= extents.z;
    }

    bool OverlapsObstacle(Vector3 world, float scale)
    {
        Collider[] hits = Physics.OverlapSphere(world + Vector3.up * scale * 0.5f, scale * 0.35f, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            if (hit.GetComponentInParent<Obstacle>() != null)
                return true;
        }

        return false;
    }

    static bool InsideTargetClearance(Vector3 world, float scale)
    {
        float obstacleRadius = scale * 0.5f;
        FlightTarget[] targets = FindObjectsByType<FlightTarget>(FindObjectsSortMode.None);
        foreach (FlightTarget target in targets)
        {
            if (target.Blocks(world, obstacleRadius))
                return true;
        }

        return false;
    }

    public void ApplyGrowth(float zoneScale, float countScale, float obstacleScale, float zoneZScale)
    {
        BoxCollider box = _zone != null ? _zone : GetComponent<BoxCollider>();

        if (zoneScale > 1f)
        {
            Vector3 size = box.size;
            float bottom = box.center.y - size.y * 0.5f;
            float extraZ = size.z * (zoneScale - 1f);
            size *= zoneScale;
            Vector3 center = box.center;
            center.y = bottom + size.y * 0.5f;
            box.size = size;
            box.center = center;
            MoveZDown(extraZ);
        }

        if (countScale > 1f)
            obstacleCount = Mathf.Max(1, Mathf.RoundToInt(obstacleCount * countScale));

        if (obstacleScale > 1f)
        {
            minScale *= obstacleScale;
            maxScale *= obstacleScale;
            GrowZFromUpper(box, obstacleScale);
        }

        GrowZFromUpper(box, zoneZScale);
    }

    void GrowZFromUpper(BoxCollider box, float scale)
    {
        if (box == null || scale <= 1f)
            return;

        Vector3 size = box.size;
        float extra = size.z * (scale - 1f);
        size.z += extra;
        box.size = size;
        MoveZDown(extra);
    }

    void MoveZDown(float extra)
    {
        Vector3 position = transform.position;
        position.z -= extra * 0.5f;
        transform.position = position;
    }

    public Vector3 UpperCenter(Camera camera)
    {
        return EdgeCenter(camera, true);
    }

    public Vector3 BottomCenter(Camera camera)
    {
        return EdgeCenter(camera, false);
    }

    Vector3 EdgeCenter(Camera camera, bool upper)
    {
        BoxCollider box = _zone != null ? _zone : GetComponent<BoxCollider>();
        Vector3 size = box.size;
        Vector3 center = box.center;
        float bottom = center.y - size.y * 0.5f;
        Vector3 forward = Vector3.forward;
        if (camera != null)
        {
            forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
            if (forward.sqrMagnitude > 0.0001f)
                forward.Normalize();
            else
                forward = Vector3.forward;
        }

        Vector3 first = transform.position;
        Vector3 second = transform.position;
        float firstScore = upper ? float.NegativeInfinity : float.PositiveInfinity;
        float secondScore = firstScore;
        float[] sides = { -0.5f, 0.5f };
        foreach (float x in sides)
        {
            foreach (float z in sides)
            {
                Vector3 world = transform.TransformPoint(center + new Vector3(size.x * x, bottom - center.y, size.z * z));
                float score = Vector3.Dot(world, forward);
                bool better = upper ? score > firstScore : score < firstScore;
                bool secondBetter = upper ? score > secondScore : score < secondScore;
                if (better)
                {
                    second = first;
                    secondScore = firstScore;
                    first = world;
                    firstScore = score;
                }
                else if (secondBetter)
                {
                    second = world;
                    secondScore = score;
                }
            }
        }

        return (first + second) * 0.5f;
    }

    void OnDrawGizmos()
    {
        BoxCollider box = _zone != null ? _zone : GetComponent<BoxCollider>();
        if (box == null)
            return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1f, 0.45f, 0.1f, 0.18f);
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = new Color(1f, 0.45f, 0.1f, 0.9f);
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
