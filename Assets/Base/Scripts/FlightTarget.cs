using UnityEngine;

public class FlightTarget : MonoBehaviour
{
    [SerializeField] float clearRadius = 2f;
    [SerializeField] Vector3 spawnOffset;

    public float ClearRadius => Mathf.Max(0f, clearRadius);
    public Vector3 SpawnOffset => spawnOffset;

    public void ApplyGrowth(float scale)
    {
        if (scale <= 1f)
            return;

        transform.localScale *= scale;
        clearRadius *= scale;
    }

    public bool Blocks(Vector3 world, float obstacleRadius)
    {
        Vector3 delta = world - transform.position;
        delta.y = 0f;
        return delta.magnitude < ClearRadius + Mathf.Max(0f, obstacleRadius);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, ClearRadius);
    }
}
