using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class FlightPath : MonoBehaviour
{
    [SerializeField] float yOffset;

    public float YOffset => yOffset;
}
