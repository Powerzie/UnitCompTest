using UnityEngine;
using UnityEngine.UI;

public static class GameFx
{
    static readonly Color[] ConfettiColors =
    {
        new Color(1f, 0.32f, 0.32f),
        new Color(1f, 0.82f, 0.2f),
        new Color(0.35f, 0.85f, 1f),
        new Color(0.55f, 0.95f, 0.4f),
        new Color(1f, 0.45f, 0.85f),
        new Color(1f, 1f, 1f)
    };

    static readonly Color ProjectileColor = new Color(0.95f, 0.55f, 0.2f, 1f);
    static readonly Color BurstColor = new Color32(0xFF, 0x97, 0x25, 0xFF);
    static Material _particleMat;
    static Material _meshMat;
    static Mesh _sphereMesh;
    static bool _hitStop;
    static bool _rim;

    public static void PlayConfetti(RectTransform canvas)
    {
        if (canvas == null)
            return;

        Canvas.ForceUpdateCanvases();
        RectTransform layer = ConfettiLayer(canvas);
        ConfettiRain rain = canvas.GetComponent<ConfettiRain>();
        if (rain == null)
            rain = canvas.gameObject.AddComponent<ConfettiRain>();
        rain.Play(layer);
    }

    public static void ObstacleBurst(Vector3 position, float scale)
    {
        float size = Mathf.Max(0.4f, scale);
        ParticleSystem ps = CreateWorld("ObstacleBurst", position);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.35f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.95f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(7f * size, 13f * size);
        main.startSize = new ParticleSystem.MinMaxCurve(0.09f * size, 0.21f * size);
        main.startColor = BurstColor;
        main.gravityModifier = 2.4f;
        main.maxParticles = 12;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(Mathf.RoundToInt(6f * size), 5, 8)) });
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 38f;
        shape.radius = 0.1f * size;
        ps.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = SphereMesh();
        renderer.sharedMaterial = MeshMat();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        FadeOut(ps);
        ps.Play();
        Dust(position, size);
        Object.Destroy(ps.gameObject, 1.8f);
    }

    public static void Muzzle(Vector3 position, float scale)
    {
        float size = Mathf.Max(0.12f, scale * 0.35f);
        ParticleSystem ps = CreateWorld("Muzzle", position);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.12f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f * size, 2.2f * size);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f * size, 0.1f * size);
        main.startColor = ProjectileColor;
        main.gravityModifier = 0.2f;
        main.maxParticles = 8;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6) });
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.04f * size;
        FadeOut(ps);
        ps.Play();
        Object.Destroy(ps.gameObject, 0.4f);
    }

    public static void Dust(Vector3 position, float scale)
    {
        Vector3 ground = position;
        ground.y = 0.04f;
        ParticleSystem ps = CreateWorld("Dust", ground);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f * scale, 1.2f * scale);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f * scale, 0.28f * scale);
        main.startColor = new Color(0.75f, 0.62f, 0.38f, 0.35f);
        main.gravityModifier = 0.05f;
        main.maxParticles = 10;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.2f * scale;
        ps.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        FadeOut(ps);
        ps.Play();
        Object.Destroy(ps.gameObject, 0.6f);
    }

    public static void HitStop()
    {
        if (_hitStop)
            return;

        _hitStop = true;
        GameObject host = new GameObject("HitStop");
        host.AddComponent<HitStopPulse>().Play();
    }

    public static void EndHitStop()
    {
        _hitStop = false;
        Time.timeScale = 1f;
    }

    public static void EnsureRim()
    {
        if (_rim)
            return;

        _rim = true;
        GameObject go = new GameObject("RimLight");
        Light light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.32f;
        light.color = new Color(0.55f, 0.72f, 1f, 1f);
        light.shadows = LightShadows.None;
        go.transform.rotation = Quaternion.Euler(25f, 210f, 0f);
        Object.DontDestroyOnLoad(go);
    }

    public static void AttachTrail(Transform follow, float scale)
    {
        if (follow == null)
            return;

        GameObject go = new GameObject("ShotTrail");
        go.transform.SetParent(follow, false);
        go.transform.localPosition = Vector3.zero;
        TrailRenderer trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.28f;
        trail.startWidth = Mathf.Max(0.08f, scale * 0.78f);
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.02f;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.alignment = LineAlignment.View;
        trail.numCornerVertices = 4;
        trail.numCapVertices = 4;
        trail.textureMode = LineTextureMode.Stretch;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(ProjectileColor, 0f), new GradientColorKey(ProjectileColor, 1f) },
            new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = gradient;
        trail.material = ParticleMat();
    }

    static RectTransform ConfettiLayer(RectTransform canvas)
    {
        Transform existing = canvas.Find("ConfettiLayer");
        if (existing is RectTransform found)
            return found;

        GameObject go = new GameObject("ConfettiLayer", typeof(RectTransform));
        RectTransform layer = go.GetComponent<RectTransform>();
        layer.SetParent(canvas, false);
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = Vector2.zero;
        layer.offsetMax = Vector2.zero;
        layer.SetSiblingIndex(1);
        return layer;
    }

    public static void SpawnConfetti(RectTransform canvas)
    {
        GameObject go = new GameObject("Confetti");
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(canvas, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        float width = Mathf.Max(200f, canvas.rect.width);
        float height = Mathf.Max(200f, canvas.rect.height);
        rt.anchoredPosition = new Vector2(Random.Range(-width * 0.48f, width * 0.48f), Random.Range(height * 0.5f, height * 0.62f));
        rt.sizeDelta = new Vector2(Random.Range(16f, 32f), Random.Range(7f, 13f));
        rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        Image image = go.AddComponent<Image>();
        image.color = ConfettiColors[Random.Range(0, ConfettiColors.Length)];
        image.raycastTarget = false;
        image.sprite = UnityEngine.Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        go.AddComponent<ConfettiPiece>().Kick();
    }

    static ParticleSystem CreateWorld(string name, Vector3 position)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = ParticleMat();
        return ps;
    }

    static void FadeOut(ParticleSystem ps)
    {
        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
    }

    static Material ParticleMat()
    {
        if (_particleMat != null)
            return _particleMat;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        _particleMat = new Material(shader);
        if (_particleMat.HasProperty("_BaseColor"))
            _particleMat.SetColor("_BaseColor", Color.white);
        if (_particleMat.HasProperty("_Color"))
            _particleMat.SetColor("_Color", Color.white);
        _particleMat.color = Color.white;
        return _particleMat;
    }

    static Material MeshMat()
    {
        if (_meshMat != null)
            return _meshMat;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null)
            return ParticleMat();

        _meshMat = new Material(shader);
        if (_meshMat.HasProperty("_BaseColor"))
            _meshMat.SetColor("_BaseColor", BurstColor);
        if (_meshMat.HasProperty("_Color"))
            _meshMat.SetColor("_Color", BurstColor);
        return _meshMat;
    }

    static Mesh SphereMesh()
    {
        if (_sphereMesh != null)
            return _sphereMesh;

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _sphereMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
        Object.Destroy(sphere);
        return _sphereMesh;
    }
}

public class HitStopPulse : MonoBehaviour
{
    public void Play()
    {
        StartCoroutine(Pulse());
    }

    System.Collections.IEnumerator Pulse()
    {
        Time.timeScale = 0.15f;
        yield return new WaitForSecondsRealtime(0.045f);
        GameFx.EndHitStop();
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        GameFx.EndHitStop();
    }
}
