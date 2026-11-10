using UnityEngine;
using UnityEngine.Rendering.Universal;

[DefaultExecutionOrder(-100)]
public class LevelSetup : MonoBehaviour
{
    void Awake()
    {
        ApplyMobileQuality();
        float scale = LevelProgress.Scale;
        float areaScale = LevelProgress.AreaScale;

        MainBehaviour ball = FindAnyObjectByType<MainBehaviour>();
        if (ball != null)
            ball.ApplyGrowth(LevelProgress.SphereScale, LevelProgress.ProjectileScale);

        ObstacleZone zone = FindAnyObjectByType<ObstacleZone>();
        if (zone != null)
            zone.ApplyGrowth(areaScale, scale, LevelProgress.ObstacleScale, LevelProgress.AreaZScale);

        Camera camera = Camera.main;
        if (camera != null)
            GrowCamera(camera.transform, LevelProgress.CameraScale, LevelProgress.CameraSideScale);

        FlightTarget target = FindAnyObjectByType<FlightTarget>();
        if (target != null)
            target.ApplyGrowth(scale);

        if (target != null && zone != null)
        {
            Vector3 corner = zone.UpperCenter(camera);
            Vector3 position = target.transform.position;
            position.x = corner.x;
            position.z = corner.z;
            position += target.SpawnOffset;
            target.transform.position = position;
        }

        if (ball != null && zone != null)
        {
            Vector3 spawn = zone.BottomCenter(camera);
            spawn.z += ball.SpawnZOffset;
            ball.PlaceOnGround(spawn);
            if (camera != null && LevelProgress.Level > 0)
                SetCameraZ(camera.transform, spawn.z + LevelProgress.CameraZOffset);
        }
    }

    static Transform CameraRig(Transform camera)
    {
        Transform rig = camera;
        while (rig.parent != null && rig.localPosition.sqrMagnitude < 0.0001f)
            rig = rig.parent;
        return rig;
    }

    static void GrowCamera(Transform camera, float heightScale, float sideScale)
    {
        if (heightScale <= 1f && sideScale <= 1f)
            return;

        Transform rig = CameraRig(camera);
        Vector3 position = rig.localPosition;
        position.x *= sideScale;
        position.y *= heightScale;
        rig.localPosition = position;
    }

    static void SetCameraZ(Transform camera, float z)
    {
        Transform rig = CameraRig(camera);
        Vector3 position = rig.localPosition;
        position.z = z;
        rig.localPosition = position;
    }

    static void ApplyMobileQuality()
    {
        if (!Application.isMobilePlatform)
            return;

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Camera camera = Camera.main;
        if (camera == null)
            return;

        camera.allowHDR = false;
        if (camera.TryGetComponent(out UniversalAdditionalCameraData data))
        {
            data.antialiasing = AntialiasingMode.None;
            data.antialiasingQuality = AntialiasingQuality.Low;
        }
    }
}

public static class LevelProgress
{
    public const float Growth = 1.2f;
    public const float SphereGrowth = 1.3f;
    public const float AreaGrowth = 1.15f;
    public const float AreaZGrowth = 1.2f;
    public const float ObstacleGrowth = 1.15f;
    public const float CameraGrowth =  1.15f;
    public const float CameraZOffset = -8.5f;
    public const float CameraSideGrowth =  1.15f;
    public const float ProjectileGrowth = 1.2f;
    public const int WinsToFinish = 3;

    public static int Level { get; private set; }

    public static float Scale => Mathf.Pow(Growth, Level);
    public static float SphereScale => Mathf.Pow(SphereGrowth, Level);
    public static float AreaScale => Mathf.Pow(AreaGrowth, Level);
    public static float AreaZScale => Mathf.Pow(AreaZGrowth, Level);
    public static float ObstacleScale => Mathf.Pow(ObstacleGrowth, Level);
    public static float CameraScale => Mathf.Pow(CameraGrowth, Level);
    public static float CameraSideScale => Mathf.Pow(CameraSideGrowth, Level);
    public static float ProjectileScale => Mathf.Pow(ProjectileGrowth, Level);
    public static bool IsFinalWin => Level + 1 >= WinsToFinish;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        Level = 0;
    }

    public static void Advance()
    {
        Level++;
    }
}
