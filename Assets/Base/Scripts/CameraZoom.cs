using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CameraZoom : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] float closeFactor = 0.38f;
    [SerializeField] float holdZoomMax = 12f;
    [SerializeField] float holdZoomTime = 2.2f;
    [SerializeField] float holdShake = 0.05f;
    [SerializeField] float holdShakeSpeed = 18f;

    Transform _rig;
    Camera _camera;
    float _fov;
    Vector3 _local;
    Vector3 _startPosition;
    Quaternion _startRotation;
    Vector3 _endPosition;
    Quaternion _endRotation;
    bool _zooming;
    bool _holding;
    float _hold;
    float _dip;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        _rig = transform.parent != null ? transform.parent : transform;
        _camera = GetComponent<Camera>();
        _local = transform.localPosition;
        if (_camera != null)
            _fov = _camera.fieldOfView;
    }

    public void Zoom(Transform focus)
    {
        if (focus == null)
            return;

        _startPosition = _rig.position;
        _startRotation = _rig.rotation;
        Vector3 lookAt = focus.position;
        _endPosition = lookAt + (_startPosition - lookAt) * closeFactor;
        Vector3 look = lookAt - _endPosition;
        _endRotation = look.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(look, Vector3.up)
            : _startRotation;
        _zooming = true;
        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator != null)
            animator.SetTrigger("Zoom");
    }

    public void SetHolding(bool holding)
    {
        _holding = holding;
    }

    public void Land()
    {
        _dip = 1f;
    }

    void LateUpdate()
    {
        if (_zooming && animator != null)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Zoom"))
            {
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(state.normalizedTime));
                _rig.position = Vector3.Lerp(_startPosition, _endPosition, u);
                _rig.rotation = Quaternion.Slerp(_startRotation, _endRotation, u);
                if (state.normalizedTime >= 1f)
                    _zooming = false;
            }
        }

        if (_holding && !_zooming)
            _hold = Mathf.MoveTowards(_hold, 1f, Time.deltaTime / Mathf.Max(0.01f, holdZoomTime));
        else
            _hold = Mathf.MoveTowards(_hold, 0f, Time.deltaTime / 0.16f);

        if (_camera != null)
            _camera.fieldOfView = _fov - holdZoomMax * _hold;

        _dip = Mathf.MoveTowards(_dip, 0f, Time.deltaTime / 0.18f);
        Vector3 shake = _local;
        shake.y -= _dip * 0.14f;
        if (_hold > 0.001f)
        {
            float t = Time.time * holdShakeSpeed;
            float mag = holdShake * (0.4f + 0.6f * _hold);
            shake += new Vector3(
                (Mathf.PerlinNoise(t, 0.13f) * 2f - 1f) * mag,
                (Mathf.PerlinNoise(0.41f, t) * 2f - 1f) * mag,
                mag * 0.25f);
        }

        transform.localPosition = shake;
    }
}
