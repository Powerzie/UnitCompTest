using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ObstacleZone : MonoBehaviour
{
    [SerializeField] GameObject obstaclePrefab;
    [SerializeField] int obstacleCount = 10;
    [SerializeField] float minScale = 0.5f;
    [SerializeField] float maxScale = 1.2f;

    BoxCollider _zone;
    Transform _root;
    int _alive;

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
        if (obstaclePrefab == null)
            return false;

        Vector3 size = _zone.size;
        Vector3 local = _zone.center + new Vector3(
            Random.Range(-size.x * 0.5f, size.x * 0.5f),
            -size.y * 0.5f,
            Random.Range(-size.z * 0.5f, size.z * 0.5f));
        Vector3 world = transform.TransformPoint(local);
        float scale = Random.Range(minScale, maxScale);

        if (OverlapsObstacle(world, scale) || InsideTargetClearance(world, scale))
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
        _alive++;
        ObstacleLifetime tracker = instance.AddComponent<ObstacleLifetime>();
        tracker.Bind(this);
        return true;
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

    public void NotifyDestroyed()
    {
        _alive = Mathf.Max(0, _alive - 1);
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

public class ObstacleLifetime : MonoBehaviour
{
    ObstacleZone _zone;

    public void Bind(ObstacleZone zone)
    {
        _zone = zone;
    }

    void OnDestroy()
    {
        if (_zone != null)
            _zone.NotifyDestroyed();
    }
}
