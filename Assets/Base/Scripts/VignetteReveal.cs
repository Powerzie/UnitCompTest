using UnityEngine;

[RequireComponent(typeof(Animator))]
public class VignetteReveal : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] Material material;
    [SerializeField] float smoothness = 0.45f;

    Transform _quad;
    Material _material;
    bool _done;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (material == null)
            return;

        _material = Instantiate(material);
        Apply(0f);
        BuildQuad();
    }

    void OnDestroy()
    {
        if (_material != null)
            Destroy(_material);
    }

    void LateUpdate()
    {
        if (_done || _material == null)
            return;

        Place(Camera.main);
        float u = Progress();
        Apply(Mathf.Lerp(0f, 2f, Mathf.SmoothStep(0f, 1f, u)));
        if (u < 1f)
            return;

        _done = true;
        if (_quad != null)
            Destroy(_quad.gameObject);
        gameObject.SetActive(false);
    }

    void BuildQuad()
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "VignetteQuad";
        Destroy(quad.GetComponent<Collider>());
        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = _material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        _quad = quad.transform;
        Place(Camera.main);
    }

    void Place(Camera camera)
    {
        if (camera == null || _quad == null)
            return;

        float z = camera.nearClipPlane + 0.02f;
        float height = 2f * z * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        _quad.SetParent(camera.transform, false);
        _quad.localRotation = Quaternion.identity;
        _quad.localPosition = new Vector3(0f, 0f, z);
        _quad.localScale = new Vector3(height * camera.aspect, height, 1f);
    }

    float Progress()
    {
        if (animator == null)
            return Mathf.Clamp01(Time.timeSinceLevelLoad / 1.25f);

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("Open") && state.length > 0.001f)
            return Mathf.Clamp01(state.normalizedTime);

        return Mathf.Clamp01(Time.timeSinceLevelLoad / 1.25f);
    }

    void Apply(float open)
    {
        _material.SetFloat("_Open", open);
        _material.SetFloat("_Smooth", smoothness);
    }
}
