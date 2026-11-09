using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Animator))]
public class MainBehaviour : MonoBehaviour
{
    [SerializeField] Transform anchor;
    [SerializeField] Collider tapZone;
    [SerializeField] Animator animator;
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] FlightTarget flightTarget;
    [SerializeField] LineRenderer flightPathPrefab;
    [SerializeField] float minScale = 0.2f;
    [SerializeField] float shrinkSpeed = 0.8f;
    [SerializeField] float tapLength = 0.22f;
    [SerializeField] float tapShrink = 0.1f;
    [SerializeField] float projectileBaseScale = 0.15f;
    [SerializeField] float launchSpeed = 12f;
    [SerializeField] float smallProjectileSpeed = 1.35f;
    [SerializeField] float bigProjectileSpeed = 0.6f;
    [SerializeField] float pathLength = 40f;

    Vector3 _initialScale;
    Vector3 _logicalScale;
    Vector3 _gestureStartScale;
    Vector2 _aimScreen;
    float _pressTime;
    bool _pressedOnThis;
    bool _awaitingTap;
    bool _sawTapState;
    bool _shrinking;
    bool _popping;
    bool _pendingLaunch;
    bool _launchedThisGesture;

    LineRenderer _path;
    FlightPath _flightPath;

    Transform ScaleTarget => anchor != null ? anchor : transform;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        _initialScale = ScaleTarget.localScale;
        _logicalScale = _initialScale;
        CreatePath();
    }

    void OnDestroy()
    {
        if (_path != null)
            Destroy(_path.gameObject);
    }

    void Update()
    {
        if (_popping)
            return;

        if (!TryReadPointer(out bool pressedThisFrame, out bool releasedThisFrame, out bool isHeld, out Vector2 screen))
        {
            if (_pendingLaunch && TapFinished())
                LaunchFromGesture();
            else if (_pressedOnThis)
                FinishGesture();

            UpdatePath();
            return;
        }

        if (pressedThisFrame && InTapZone(screen))
            BeginGesture(screen);

        if (_pressedOnThis && (isHeld || releasedThisFrame))
            _aimScreen = screen;

        if (_pressedOnThis && isHeld && _awaitingTap && !_shrinking && TapFinished())
            BeginShrink();

        if (_pendingLaunch && TapFinished())
            LaunchFromGesture();

        if (_pressedOnThis && releasedThisFrame)
            FinishGesture();

        if (_shrinking && !_popping)
        {
            ApplyShrink(_initialScale * (shrinkSpeed * Time.deltaTime));
            if (_popping)
                LaunchFromGesture();
        }

        UpdatePath();
    }

    public void PopFinished()
    {
        Destroy(gameObject);
    }

    void BeginGesture(Vector2 screen)
    {
        _pressedOnThis = true;
        _shrinking = false;
        _awaitingTap = true;
        _sawTapState = false;
        _pendingLaunch = false;
        _launchedThisGesture = false;
        _pressTime = Time.time;
        _aimScreen = screen;
        _gestureStartScale = _logicalScale;
        SetHolding(false);
        SetTrigger("Tap");
    }

    void BeginShrink()
    {
        _awaitingTap = false;
        _shrinking = true;
        SetHolding(true);
        ApplyShrink(_initialScale * tapShrink);
        if (_popping)
            LaunchFromGesture();
    }

    void FinishGesture()
    {
        _pressedOnThis = false;
        SetHolding(false);

        if (_awaitingTap)
        {
            _shrinking = false;
            _pendingLaunch = true;
            return;
        }

        LaunchFromGesture();
    }

    void LaunchFromGesture()
    {
        if (_launchedThisGesture)
            return;

        if (_gestureStartScale.x - _logicalScale.x <= 0.0001f)
            ApplyShrink(_initialScale * tapShrink);

        float decreased = Mathf.Max(0f, _gestureStartScale.x - _logicalScale.x);
        float shotScale = decreased + projectileBaseScale;
        Vector3 direction = AimDirection();
        Vector3 origin = LaunchRoot(direction, shotScale);
        Vector3 velocity = ProjectileClone.Velocity(direction, shotScale, launchSpeed, smallProjectileSpeed, bigProjectileSpeed);
        SpawnProjectile(origin, shotScale, velocity);

        _launchedThisGesture = true;
        _pendingLaunch = false;
        _awaitingTap = false;
        _shrinking = false;
        HidePath();
    }

    void SpawnProjectile(Vector3 origin, float shotScale, Vector3 velocity)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("Assign the ProjectileClone prefab on " + name, this);
            return;
        }

        GameObject instance = Instantiate(projectilePrefab, origin, Quaternion.identity);
        ProjectileClone clone = instance.GetComponent<ProjectileClone>();
        if (clone == null)
            clone = instance.GetComponentInChildren<ProjectileClone>();

        if (clone == null)
        {
            Debug.LogWarning("ProjectileClone prefab is missing its ProjectileClone component.", projectilePrefab);
            Destroy(instance);
            return;
        }

        clone.ApplyScale(shotScale);
        clone.Launch(velocity, GetComponent<Collider>());
    }

    void ApplyShrink(Vector3 delta)
    {
        Vector3 minimum = _initialScale * minScale;
        Vector3 next = _logicalScale - delta;
        bool reachedMin = next.x <= minimum.x && next.y <= minimum.y && next.z <= minimum.z;
        _logicalScale = reachedMin ? minimum : next;
        ScaleTarget.localScale = _logicalScale;

        if (reachedMin)
            BeginPop();
    }

    void BeginPop()
    {
        if (_popping)
            return;

        _popping = true;
        _shrinking = false;
        _pressedOnThis = false;
        _pendingLaunch = false;
        HidePath();
        SetTrigger("Pop");
        Invoke(nameof(PopFinished), 0.3f);
    }

    float PreviewShotScale()
    {
        float decreased = Mathf.Max(0f, _gestureStartScale.x - _logicalScale.x);
        if (!_shrinking)
            decreased = Mathf.Max(decreased, _initialScale.x * tapShrink);

        return decreased + projectileBaseScale;
    }

    FlightTarget Target
    {
        get
        {
            if (flightTarget == null)
                flightTarget = FindAnyObjectByType<FlightTarget>();
            return flightTarget;
        }
    }

    Vector3 AimDirection()
    {
        FlightTarget target = Target;
        if (target != null)
        {
            Vector3 toTarget = Vector3.ProjectOnPlane(target.transform.position - ScaleTarget.position, Vector3.up);
            if (toTarget.sqrMagnitude > 0.0001f)
                return toTarget.normalized;
        }

        Camera camera = Camera.main;
        Vector3 origin = ScaleTarget.position;
        if (camera == null)
            return Vector3.forward;

        Ray ray = camera.ScreenPointToRay(_aimScreen);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, origin.y, 0f));
        if (plane.Raycast(ray, out float distance))
        {
            Vector3 flat = Vector3.ProjectOnPlane(ray.GetPoint(distance) - origin, Vector3.up);
            if (flat.sqrMagnitude > 0.0001f)
                return flat.normalized;
        }

        Vector3 fallback = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
        return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.forward;
    }

    float MainRadius()
    {
        Collider body = GetComponent<Collider>();
        if (body != null)
            return body.bounds.extents.x;
        return ScaleTarget.lossyScale.x * 0.5f;
    }

    Vector3 LaunchRoot(Vector3 direction, float shotScale)
    {
        Collider body = GetComponent<Collider>();
        Vector3 center = body != null ? body.bounds.center : transform.position;
        float mainRadius = body != null ? body.bounds.extents.x : 0.5f;
        Vector3 origin = center + direction * (mainRadius + shotScale * 0.5f);
        origin.y = ScaleTarget.position.y;
        return origin;
    }

    Vector3 PathOrigin(Vector3 rootOrigin, float shotScale)
    {
        if (projectilePrefab == null)
            return rootOrigin;

        ProjectileClone clone = projectilePrefab.GetComponent<ProjectileClone>();
        if (clone == null)
            clone = projectilePrefab.GetComponentInChildren<ProjectileClone>();

        if (clone == null || clone.transform.parent == null)
            return rootOrigin;

        return rootOrigin + Vector3.Scale(clone.transform.localPosition, Vector3.one * shotScale);
    }

    void CreatePath()
    {
        if (flightPathPrefab == null)
        {
            Debug.LogWarning("Assign the FlightPath prefab on " + name, this);
            return;
        }

        _path = Instantiate(flightPathPrefab);
        _flightPath = _path.GetComponent<FlightPath>();
        _path.positionCount = 0;
    }

    void UpdatePath()
    {
        if (_path == null)
            return;

        FlightTarget target = Target;
        if (target == null && !_pressedOnThis && !_pendingLaunch)
        {
            HidePath();
            return;
        }

        float shotScale = PreviewShotScale();
        Vector3 direction = AimDirection();
        Vector3 origin = PathOrigin(LaunchRoot(direction, shotScale), shotScale);
        float width = target != null ? MainRadius() * 2f : shotScale;
        Vector3 end = origin + direction * pathLength;
        if (target != null)
        {
            end = target.transform.position;
            end.y = origin.y;
        }

        float yOffset = _flightPath != null ? _flightPath.YOffset : 0f;
        origin.y += yOffset;
        end.y += yOffset;

        _path.startWidth = width;
        _path.endWidth = width;
        _path.positionCount = 2;
        _path.SetPosition(0, origin);
        _path.SetPosition(1, end);
    }

    void HidePath()
    {
        if (_path != null)
            _path.positionCount = 0;
    }

    bool TapFinished()
    {
        if (IsInTapState())
            _sawTapState = true;

        if (!HasController)
            return Time.time - _pressTime >= tapLength;

        if (!_sawTapState)
            return Time.time - _pressTime >= tapLength + 0.15f;

        return !IsTapPlaying();
    }

    bool IsInTapState()
    {
        if (!HasController)
            return false;

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("TapBounce"))
            return true;

        return animator.IsInTransition(0)
            && animator.GetNextAnimatorStateInfo(0).IsName("TapBounce");
    }

    bool IsTapPlaying()
    {
        if (!HasController)
            return Time.time - _pressTime < tapLength;

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        if (current.IsName("TapBounce") && current.normalizedTime < 1f)
            return true;

        return animator.IsInTransition(0)
            && animator.GetNextAnimatorStateInfo(0).IsName("TapBounce");
    }

    bool HasController => animator != null && animator.runtimeAnimatorController != null;

    void SetHolding(bool holding)
    {
        if (HasController)
            animator.SetBool("Holding", holding);
    }

    void SetTrigger(string triggerName)
    {
        if (HasController)
            animator.SetTrigger(triggerName);
    }

    bool InTapZone(Vector2 screen)
    {
        if (tapZone == null)
            return true;

        Camera camera = Camera.main;
        if (camera == null)
            return false;

        Ray ray = camera.ScreenPointToRay(screen);
        return tapZone.Raycast(ray, out _, Mathf.Infinity);
    }

    static bool TryReadPointer(out bool pressedThisFrame, out bool releasedThisFrame, out bool isHeld, out Vector2 screen)
    {
        pressedThisFrame = false;
        releasedThisFrame = false;
        isHeld = false;
        screen = default;

        Mouse mouse = Mouse.current;
        if (mouse != null && (mouse.leftButton.isPressed || mouse.leftButton.wasPressedThisFrame || mouse.leftButton.wasReleasedThisFrame))
        {
            pressedThisFrame = mouse.leftButton.wasPressedThisFrame;
            releasedThisFrame = mouse.leftButton.wasReleasedThisFrame;
            isHeld = mouse.leftButton.isPressed;
            screen = mouse.position.ReadValue();
            return true;
        }

        Touchscreen touch = Touchscreen.current;
        if (touch == null)
            return false;

        var press = touch.primaryTouch.press;
        pressedThisFrame = press.wasPressedThisFrame;
        releasedThisFrame = press.wasReleasedThisFrame;
        isHeld = press.isPressed;
        screen = touch.primaryTouch.position.ReadValue();
        return true;
    }
}
