using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Animator))]
public class MainBehaviour : MonoBehaviour
{
    const float HopDuration = 0.32f;

    [SerializeField] Transform anchor;
    [SerializeField] Collider tapZone;
    [SerializeField] Animator animator;
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] FlightTarget flightTarget;
    [SerializeField] LineRenderer flightPathPrefab;
    [SerializeField] NextLevelPopup nextLevelPopup;
    [SerializeField] TryAgainPopup tryAgainPopup;
    [SerializeField] float minScale = 0.2f;
    [SerializeField] float shrinkSpeed = 0.8f;
    [SerializeField] float tapLength = 0.22f;
    [SerializeField] float tapShrink = 0.1f;
    [SerializeField] float projectileBaseScale = 0.15f;
    [SerializeField] float launchSpeed = 12f;
    [SerializeField] float smallProjectileSpeed = 1.35f;
    [SerializeField] float bigProjectileSpeed = 0.6f;
    [SerializeField] float smallHitRadius = 1f;
    [SerializeField] float hitRadiusHold = 0.1f;
    [SerializeField] float hitRadiusStep = 1.5f;
    [SerializeField] float hitRadiusOnHold = 2f;
    [SerializeField] float hitRadiusMax = 8f;
    [SerializeField] float pathLength = 40f;
    [SerializeField] float spawnZOffset;

    Vector3 _initialScale;
    Vector3 _logicalScale;
    Vector3 _gestureStartScale;
    Vector2 _aimScreen;
    float _pressTime;
    float _holdStart;
    float _approachDistance;
    bool _pressedOnThis;
    bool _awaitingTap;
    bool _sawTapState;
    bool _shrinking;
    bool _popping;
    bool _pendingLaunch;
    bool _launchedThisGesture;
    bool _advancing;
    bool _arrivalPlayed;
    bool _doorOpened;
    LineRenderer _path;
    FlightPath _flightPath;
    Collider _body;
    Renderer _ball;
    Color _emitRest;
    Color _emitHold;
    float _charge;

    Transform ScaleTarget => anchor != null ? anchor : transform;
    public float SpawnZOffset => spawnZOffset;

    public void ApplyGrowth(float scale, float projectileScale)
    {
        if (scale > 1f)
            ScaleTarget.localScale *= scale;
        if (projectileScale > 1f)
            projectileBaseScale *= projectileScale;
    }

    public void PlaceOnGround(Vector3 worldPosition)
    {
        Vector3 position = ScaleTarget.position;
        position.x = worldPosition.x;
        position.z = worldPosition.z;
        ScaleTarget.position = position;
    }

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        _body = GetComponent<Collider>();
        _initialScale = ScaleTarget.localScale;
        _logicalScale = _initialScale;
        _ball = GetComponent<Renderer>();
        if (_ball != null)
        {
            Material material = _ball.material;
            material.EnableKeyword("_EMISSION");
            _emitRest = material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;
            _emitHold = _emitRest + new Color(0.55f, 0.22f, 0.04f, 0f);
        }

        GameFx.EnsureRim();
        CreatePath();
    }

    void OnDestroy()
    {
        if (_path != null)
            Destroy(_path.gameObject);
    }

    void Update()
    {
        if (_popping || _advancing)
            return;

        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame && Target != null)
        {
            BeginAdvance();
            return;
        }

        if (Target != null && PathIsClear())
        {
            BeginAdvance();
            return;
        }

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
            ApplyShrink(_initialScale * (shrinkSpeed * Time.deltaTime));

        UpdatePath();
    }

    void LateUpdate()
    {
        _charge = Mathf.MoveTowards(_charge, _shrinking ? 1f : 0f, Time.deltaTime / 0.12f);
        if (_ball != null && _ball.material.HasProperty("_EmissionColor"))
            _ball.material.SetColor("_EmissionColor", Color.Lerp(_emitRest, _emitHold, _charge));
    }

    bool PathIsClear()
    {
        Vector3 from = ScaleTarget.position;
        Vector3 to = Target.transform.position;
        float pathRadius = MainRadius();
        Obstacle[] obstacles = FindObjectsByType<Obstacle>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Obstacle obstacle in obstacles)
        {
            if (obstacle.OverlapsPath(from, to, pathRadius))
                return false;
        }

        return true;
    }

    void BeginAdvance()
    {
        _advancing = true;
        _pressedOnThis = false;
        _shrinking = false;
        _awaitingTap = false;
        _pendingLaunch = false;
        SetHolding(false);
        HidePath();
        GameAudio.PlayPathCleared();
        if (HasController)
            animator.Play("Idle", 0, 0f);

        _approachDistance = Target != null
            ? HorizontalDistance(ScaleTarget.position, Target.transform.position)
            : 0f;
        StartCoroutine(AdvanceToTarget());
    }

    IEnumerator AdvanceToTarget()
    {
        Transform body = ScaleTarget;
        float floorY = body.position.y;
        Vector3 destination = Target.transform.position;
        destination.y = floorY;
        Vector3 toTarget = destination - body.position;
        toTarget.y = 0f;
        float extra = Mathf.Max(2.4f, MainRadius() * 3.2f);
        if (toTarget.sqrMagnitude > 0.0001f)
            destination += toTarget.normalized * extra;
        float hopTime = HopTime();
        float celebrateTime = hopTime * 3.1f;
        if (HasController)
            animator.speed = HopDuration / celebrateTime;

        SetTrigger("Hop");
        GameAudio.PlayJump();
        float lift = Mathf.Max(3f, MainRadius() * 4.2f);
        float celebrate = 0f;
        while (celebrate < celebrateTime)
        {
            celebrate += Time.deltaTime;
            float u = Mathf.Clamp01(celebrate / celebrateTime);
            Vector3 position = body.position;
            position.y = floorY + Mathf.Sin(u * Mathf.PI) * lift;
            body.position = position;
            yield return null;
        }

        Vector3 landed = body.position;
        landed.y = floorY;
        body.position = landed;
        CameraZoom land = Camera.main != null ? Camera.main.GetComponent<CameraZoom>() : null;
        if (land != null)
            land.Land();

        float step = HopStep();
        float pathLength = HorizontalDistance(body.position, destination);
        if (HasController)
            animator.speed = HopDuration / hopTime;

        while (HorizontalDistance(body.position, destination) > 0.05f)
        {
            TryPlayArrival();
            Vector3 start = body.position;
            Vector3 flat = destination - start;
            flat.y = 0f;
            float remaining = flat.magnitude;
            float thisHop = hopTime;
            if (pathLength > 0.05f && remaining <= pathLength * 0.25f)
                thisHop *= 1.35f;
            if (HasController)
                animator.speed = HopDuration / thisHop;
            Vector3 next = start + flat.normalized * Mathf.Min(step, remaining);
            next.y = floorY;
            SetTrigger("Hop");
            GameAudio.PlayJump();
            float time = 0f;
            while (time < thisHop)
            {
                time += Time.deltaTime;
                float u = Mathf.Clamp01(time / thisHop);
                Vector3 position = Vector3.Lerp(start, next, u);
                position.y = floorY;
                body.position = position;
                TryPlayArrival();
                yield return null;
            }

            body.position = next;
            if (land != null)
                land.Land();
        }

        if (HasController)
            animator.speed = 1f;
        ShowNextScreen();
    }

    void TryPlayArrival()
    {
        if (Target == null)
            return;

        float step = HopStep();
        float hopTime = HopTime();
        float lead = step / hopTime * 2f;
        float near = Mathf.Max(MainRadius() * 4f, _approachDistance * 0.3f) + lead;
        float dist = HorizontalDistance(ScaleTarget.position, Target.transform.position);

        if (!_doorOpened && dist <= near + step / hopTime * 0.5f)
        {
            _doorOpened = true;
            Door door = FindAnyObjectByType<Door>();
            if (door != null)
                door.Open();
        }

        if (_arrivalPlayed || dist > near)
            return;

        _arrivalPlayed = true;
        CameraZoom zoom = FindAnyObjectByType<CameraZoom>();
        if (zoom != null)
            zoom.Zoom(Target.transform);
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void ShowNextScreen()
    {
        if (nextLevelPopup == null)
        {
            Debug.LogWarning("Assign the NextLevelPopup prefab on " + name, this);
            return;
        }

        GameAudio.PlayWin();
        NextLevelPopup popup = Instantiate(nextLevelPopup);
        popup.Show(LevelProgress.IsFinalWin);
    }

    public void PopFinished()
    {
        if (tryAgainPopup != null)
            Instantiate(tryAgainPopup);
        else
            Debug.LogWarning("Assign the TryAgainPopup prefab on " + name, this);

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
        _holdStart = 0f;
        _aimScreen = screen;
        _gestureStartScale = _logicalScale;
        SetHolding(false);
        GameAudio.PlayHold();
        SetTrigger("Tap");
    }

    void BeginShrink()
    {
        _awaitingTap = false;
        _shrinking = true;
        _holdStart = Time.time;
        SetHolding(true);
        ApplyShrink(_initialScale * tapShrink);
    }

    void FinishGesture()
    {
        _pressedOnThis = false;
        SetHolding(false);
        if (_popping)
            return;

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
        if (_launchedThisGesture || _popping)
            return;

        if (_gestureStartScale.x - _logicalScale.x <= 0.0001f)
            ApplyShrink(_initialScale * tapShrink);

        if (_popping)
            return;

        float shotScale = Mathf.Max(0f, _gestureStartScale.x - _logicalScale.x) + projectileBaseScale;
        Vector3 direction = AimDirection();
        SpawnProjectile(LaunchRoot(direction, shotScale), shotScale, ProjectileClone.Velocity(direction, shotScale, launchSpeed, smallProjectileSpeed, bigProjectileSpeed));
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

        GameAudio.PlayShoot();
        GameFx.Muzzle(origin, shotScale);
        clone.ApplyScale(shotScale);
        bool holding = _holdStart > 0f;
        float hold = holding ? Time.time - _holdStart : 0f;
        float onHold = holding ? hitRadiusOnHold : 1f;
        clone.Launch(velocity, _body, ProjectileClone.HoldRadius(hold, smallHitRadius, hitRadiusHold, hitRadiusStep, hitRadiusMax, onHold));
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
        SetHolding(false);
        GameAudio.PlayLost();
        SetTrigger("Pop");
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

    float HopTime()
    {
        float u = Mathf.InverseLerp(_initialScale.x * minScale, _initialScale.x, _logicalScale.x);
        return HopDuration * Mathf.Lerp(0.9f, 1.25f, u);
    }

    float HopStep()
    {
        float small = Mathf.InverseLerp(_initialScale.x, _initialScale.x * minScale, _logicalScale.x);
        return Mathf.Max(1f, MainRadius() * 5f) * Mathf.Lerp(1f, 2.6f, small);
    }

    float MainRadius()
    {
        return _body != null ? _body.bounds.extents.x : ScaleTarget.lossyScale.x * 0.5f;
    }

    Vector3 LaunchRoot(Vector3 direction, float shotScale)
    {
        Vector3 center = _body != null ? _body.bounds.center : transform.position;
        float mainRadius = _body != null ? _body.bounds.extents.x : 0.5f;
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
        Vector3 end = origin + direction * pathLength;
        if (target != null)
        {
            end = target.transform.position;
            end.y = origin.y;
        }

        float yOffset = _flightPath != null ? _flightPath.YOffset : 0f;
        origin.y += yOffset;
        end.y += yOffset;
        float width = target != null ? MainRadius() * 2f : shotScale;
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
        return animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName("TapBounce");
    }

    bool IsTapPlaying()
    {
        if (!HasController)
            return Time.time - _pressTime < tapLength;

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        if (current.IsName("TapBounce") && current.normalizedTime < 1f)
            return true;
        return animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName("TapBounce");
    }

    bool HasController => animator != null && animator.runtimeAnimatorController != null;

    void SetHolding(bool holding)
    {
        if (HasController)
            animator.SetBool("Holding", holding);
        if (holding)
            GameAudio.PlayHold();
        else
            GameAudio.StopHold();

        CameraZoom zoom = Camera.main != null ? Camera.main.GetComponent<CameraZoom>() : null;
        if (zoom != null)
            zoom.SetHolding(holding);
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
        return tapZone.Raycast(camera.ScreenPointToRay(screen), out _, Mathf.Infinity);
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
