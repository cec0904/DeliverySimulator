using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("Effects/GTA Mission Marker VFX")]
public class GTAMissionMarkerVFX : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] private Color markerColor = new Color(1f, 0.08f, 0.03f, 1f);
    [SerializeField, Min(0.1f)] private float radius = 1.6f;
    [SerializeField, Min(0.2f)] private float height = 2.8f;
    [SerializeField, Range(0f, 2f)] private float brightness = 1f;

    [Header("Rising Light")]
    [SerializeField, Range(1f, 60f)] private float emissionRate = 18f;
    [SerializeField] private Vector2 lifetimeRange = new Vector2(0.8f, 1.7f);
    [SerializeField] private Vector2 riseSpeedRange = new Vector2(0.9f, 1.8f);
    [SerializeField, Range(0f, 1f)] private float noiseStrength = 0.18f;
    [SerializeField, Range(0f, 3f)] private float noiseFrequency = 0.75f;

    [Header("Motion")]
    [SerializeField, Range(0f, 5f)] private float pulseSpeed = 1.15f;
    [SerializeField, Range(0f, 0.25f)] private float pulseAmount = 0.055f;
    [SerializeField, Range(0f, 1f)] private float flickerAmount = 0.16f;

    private ParticleSystem groundGlow;
    private ParticleSystem coreGlow;
    private ParticleSystem risingLight;
    private ParticleSystem sparks;
    private Material groundMaterial;
    private Material coreMaterial;
    private Material streakMaterial;
    private Material sparkMaterial;
    private Texture2D groundTexture;
    private Texture2D coreTexture;
    private Texture2D streakTexture;
    private Texture2D sparkTexture;
    private bool isBuilding;

    public Color MarkerColor => markerColor;

    private void OnEnable() { Rebuild(); }
    private void OnValidate() { if (isActiveAndEnabled) Rebuild(); }

    private void Update()
    {
        if (groundGlow == null || coreGlow == null) return;

        float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
        float pulse = 1f + Mathf.Sin(time * pulseSpeed * Mathf.PI * 2f) * pulseAmount;
        float irregular = 1f - flickerAmount + Mathf.PerlinNoise(0.37f, time * 1.7f) * flickerAmount;

        groundGlow.transform.localScale = new Vector3(pulse, 1f, pulse);
        coreGlow.transform.localScale = new Vector3(1f + (pulse - 1f) * 0.45f, 1f, 1f);
        ApplyMaterialColor(groundMaterial, WithAlpha(markerColor, 0.34f * brightness * irregular));
        ApplyMaterialColor(coreMaterial, WithAlpha(markerColor, 0.25f * brightness * irregular));
        ApplyMaterialColor(streakMaterial, WithAlpha(markerColor, 0.92f * brightness));
        ApplyMaterialColor(sparkMaterial, WithAlpha(markerColor, 0.82f * brightness));
    }

    private void OnDisable() { ReleaseRuntimeResources(); }

    public void SetColor(Color color)
    {
        markerColor = color;
        Rebuild();
    }

    public void Rebuild()
    {
        if (isBuilding || !isActiveAndEnabled) return;
        isBuilding = true;

        lifetimeRange = SortRange(lifetimeRange, 0.08f);
        riseSpeedRange = SortRange(riseSpeedRange, 0f);

        groundGlow = GetOrCreateParticleSystem("Ground Glow");
        coreGlow = GetOrCreateParticleSystem("Core Glow");
        risingLight = GetOrCreateParticleSystem("Rising Light");
        sparks = GetOrCreateParticleSystem("Floating Sparks");

        ReleaseRuntimeResources();
        groundTexture = CreateGroundTexture(128);
        coreTexture = CreateCoreTexture(128, 256);
        streakTexture = CreateStreakTexture(64, 256);
        sparkTexture = CreateSparkTexture(64);
        groundMaterial = CreateParticleMaterial(groundTexture, "Mission Marker Ground");
        coreMaterial = CreateParticleMaterial(coreTexture, "Mission Marker Core");
        streakMaterial = CreateParticleMaterial(streakTexture, "Mission Marker Streak");
        sparkMaterial = CreateParticleMaterial(sparkTexture, "Mission Marker Spark");

        ConfigureGround();
        ConfigureCore();
        ConfigureRisingLight();
        ConfigureSparks();
        Update();

        isBuilding = false;
    }

    private ParticleSystem GetOrCreateParticleSystem(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            GameObject childObject = new GameObject(childName);
            childObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            childObject.transform.SetParent(transform, false);
            child = childObject.transform;
        }

        ParticleSystem system = child.GetComponent<ParticleSystem>();
        if (system == null) system = child.gameObject.AddComponent<ParticleSystem>();
        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;
        return system;
    }

    private void ConfigureGround()
    {
        groundGlow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = groundGlow.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 5f;
        main.startLifetime = 99999f;
        main.startSpeed = 0f;
        main.startSize3D = false;
        main.startSize = radius * 2f;
        main.startColor = Color.white;
        main.maxParticles = 1;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Local;

        ParticleSystem.EmissionModule emission = groundGlow.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = groundGlow.shape;
        shape.enabled = false;

        ParticleSystemRenderer renderer = groundGlow.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        renderer.material = groundMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingFudge = 2f;

        groundGlow.transform.localPosition = new Vector3(0f, 0.035f, 0f);
        groundGlow.Emit(1);
        groundGlow.Play();
    }

    private void ConfigureCore()
    {
        coreGlow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = coreGlow.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 5f;
        main.startLifetime = 99999f;
        main.startSpeed = 0f;
        main.startSize3D = true;
        main.startSizeX = radius * 2f;
        main.startSizeY = height;
        main.startSizeZ = 0.05f;
        main.startColor = Color.white;
        main.maxParticles = 1;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Local;

        ParticleSystem.EmissionModule emission = coreGlow.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = coreGlow.shape;
        shape.enabled = false;

        ParticleSystemRenderer renderer = coreGlow.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.VerticalBillboard;
        renderer.material = coreMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingFudge = 1f;

        coreGlow.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
        coreGlow.Emit(1);
        coreGlow.Play();
    }

    private void ConfigureRisingLight()
    {
        risingLight.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = risingLight.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeRange.x, lifetimeRange.y);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(radius * 0.035f, radius * 0.11f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = Color.white;
        main.maxParticles = Mathf.Max(32, Mathf.CeilToInt(emissionRate * lifetimeRange.y * 1.5f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Local;

        ParticleSystem.EmissionModule emission = risingLight.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;

        ParticleSystem.ShapeModule shape = risingLight.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(radius * 1.45f, 0.02f, radius * 1.45f);

        ParticleSystem.VelocityOverLifetimeModule velocity = risingLight.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 0f;
        velocity.y = new ParticleSystem.MinMaxCurve(riseSpeedRange.x, riseSpeedRange.y);
        velocity.z = 0f;

        ParticleSystem.NoiseModule noise = risingLight.noise;
        noise.enabled = noiseStrength > 0f;
        noise.separateAxes = true;
        noise.strengthX = noiseStrength;
        noise.strengthY = noiseStrength * 0.12f;
        noise.strengthZ = noiseStrength;
        noise.frequency = noiseFrequency;
        noise.scrollSpeed = 0.22f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.High;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = risingLight.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient colorGradient = new Gradient();
        colorGradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.95f, 0.14f), new GradientAlphaKey(0.55f, 0.72f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = colorGradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = risingLight.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(0.18f, 1f), new Keyframe(0.72f, 0.75f), new Keyframe(1f, 0.12f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystemRenderer renderer = risingLight.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.42f;
        renderer.lengthScale = 2.8f;
        renderer.cameraVelocityScale = 0f;
        renderer.material = streakMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingFudge = 3f;

        risingLight.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        risingLight.Play();
    }

    private void ConfigureSparks()
    {
        sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = sparks.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 1.45f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(radius * 0.018f, radius * 0.055f);
        main.startColor = Color.white;
        main.maxParticles = 64;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Local;

        ParticleSystem.EmissionModule emission = sparks.emission;
        emission.enabled = true;
        emission.rateOverTime = Mathf.Max(3f, emissionRate * 0.28f);

        ParticleSystem.ShapeModule shape = sparks.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(radius * 1.35f, 0.04f, radius * 1.35f);

        ParticleSystem.VelocityOverLifetimeModule velocity = sparks.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
        velocity.y = new ParticleSystem.MinMaxCurve(riseSpeedRange.x * 0.45f, riseSpeedRange.y * 0.8f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = sparks.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient colorGradient = new Gradient();
        colorGradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.2f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = colorGradient;

        ParticleSystemRenderer renderer = sparks.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = sparkMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingFudge = 4f;

        sparks.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        sparks.Play();
    }

    private Material CreateParticleMaterial(Texture2D texture, string materialName)
    {
        Shader shader = GraphicsSettings.currentRenderPipeline == null ? Shader.Find("Particles/Standard Unlit") : Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) return null;

        Material material = new Material(shader) { name = materialName, hideFlags = HideFlags.HideAndDontSave, renderQueue = 3000 };
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 2f);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.One);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        return material;
    }

    private static Texture2D CreateGroundTexture(int size)
    {
        Texture2D texture = NewTexture(size, size, "Mission Marker Ground Texture");
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size * 2f - 1f;
                float ny = (y + 0.5f) / size * 2f - 1f;
                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                float disc = 1f - Mathf.SmoothStep(0.55f, 1f, distance);
                float edge = Mathf.Exp(-Mathf.Pow((distance - 0.83f) * 14f, 2f));
                float alpha = Mathf.Clamp01(disc * 0.52f + edge * 0.85f) * (1f - Mathf.SmoothStep(0.96f, 1f, distance));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private static Texture2D CreateCoreTexture(int width, int heightPixels)
    {
        Texture2D texture = NewTexture(width, heightPixels, "Mission Marker Core Texture");
        Color[] pixels = new Color[width * heightPixels];
        for (int y = 0; y < heightPixels; y++)
        {
            float v = (y + 0.5f) / heightPixels;
            float vertical = Mathf.SmoothStep(0f, 0.12f, v) * (1f - Mathf.SmoothStep(0.58f, 1f, v));
            for (int x = 0; x < width; x++)
            {
                float u = Mathf.Abs((x + 0.5f) / width * 2f - 1f);
                float horizontal = 1f - Mathf.SmoothStep(0.58f, 1f, u);
                float bands = 0.82f + Mathf.Sin(v * 29f + u * 5f) * 0.08f + Mathf.Sin(v * 71f) * 0.04f;
                float alpha = Mathf.Clamp01(horizontal * vertical * bands);
                pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private static Texture2D CreateStreakTexture(int width, int heightPixels)
    {
        Texture2D texture = NewTexture(width, heightPixels, "Mission Marker Streak Texture");
        Color[] pixels = new Color[width * heightPixels];
        for (int y = 0; y < heightPixels; y++)
        {
            float v = (y + 0.5f) / heightPixels;
            float vertical = Mathf.Sin(v * Mathf.PI);
            vertical *= vertical;
            for (int x = 0; x < width; x++)
            {
                float u = Mathf.Abs((x + 0.5f) / width * 2f - 1f);
                float horizontal = Mathf.Pow(Mathf.Clamp01(1f - u), 3.5f);
                float alpha = horizontal * vertical;
                pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private static Texture2D CreateSparkTexture(int size)
    {
        Texture2D texture = NewTexture(size, size, "Mission Marker Spark Texture");
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size * 2f - 1f;
                float ny = (y + 0.5f) / size * 2f - 1f;
                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 3f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private static Texture2D NewTexture(int width, int heightPixels, string textureName)
    {
        Texture2D texture = new Texture2D(width, heightPixels, TextureFormat.RGBA32, false, true)
        {
            name = textureName,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };
        return texture;
    }

    private static void ApplyMaterialColor(Material material, Color color)
    {
        if (material == null) return;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = Mathf.Clamp01(alpha);
        return color;
    }

    private static Vector2 SortRange(Vector2 range, float minimum)
    {
        range.x = Mathf.Max(minimum, range.x);
        range.y = Mathf.Max(minimum, range.y);
        if (range.x > range.y) (range.x, range.y) = (range.y, range.x);
        return range;
    }

    private void ReleaseRuntimeResources()
    {
        SafeDestroy(groundMaterial);
        SafeDestroy(coreMaterial);
        SafeDestroy(streakMaterial);
        SafeDestroy(sparkMaterial);
        SafeDestroy(groundTexture);
        SafeDestroy(coreTexture);
        SafeDestroy(streakTexture);
        SafeDestroy(sparkTexture);
        groundMaterial = null;
        coreMaterial = null;
        streakMaterial = null;
        sparkMaterial = null;
        groundTexture = null;
        coreTexture = null;
        streakTexture = null;
        sparkTexture = null;
    }

    private static void SafeDestroy(Object target)
    {
        if (target == null) return;
        if (Application.isPlaying) Destroy(target); else DestroyImmediate(target);
    }
}
