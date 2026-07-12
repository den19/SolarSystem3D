using UnityEngine;

/// <summary>
/// Drives comet nucleus, coma, and dual tails based on heliocentric distance.
/// </summary>
public class CometVisualController : MonoBehaviour
{
    enum ActivityStage
    {
        Dormant,
        Developing,
        Active,
        Outburst
    }

    static readonly Color DefaultComaColor = new Color(0.55f, 0.92f, 0.78f, 1f);

    [Header("Orbital data (set at spawn)")]
    [SerializeField] float semiMajorAxisAu = 2.5f;
    [SerializeField] float eccentricity = 0.5f;

    [Header("Child references")]
    [SerializeField] Transform nucleus;
    [SerializeField] Transform coma;
    [SerializeField] TrailRenderer dustTrail;
    [SerializeField] LineRenderer ionTail;
    [SerializeField] Renderer comaRenderer;

    Transform _sun;
    float _auToUnity = 22f;
    float _perihelionAu;
    Material _comaMaterial;
    float _baseComaScale = 1.35f;
    float _baseDustStartWidth = 0.35f;
    float _baseIonStartWidth = 0.25f;
    float _baseIonLength = 14f;
    CometOrbitController _orbit;
    Material _nucleusBaseMaterial;
    Color _nucleusBaseColor = new Color(0.1f, 0.09f, 0.08f, 1f);
    CometContentData.ContentEntry _profile;
    bool _profileLoaded;

    public void Initialize(CometCatalog.CometDefinition definition, Transform sun, float auToUnity)
    {
        semiMajorAxisAu = definition.semiMajorAxisAu;
        eccentricity = definition.eccentricity;
        _sun = sun;
        _auToUnity = Mathf.Max(0.001f, auToUnity);
        _perihelionAu = semiMajorAxisAu * (1f - eccentricity);
        _orbit = GetComponent<CometOrbitController>();

        LoadProfile(definition.objectName);
        CacheReferences();
        CacheBaseValues();
        EnsureVisualAssets();
    }

    void LoadProfile(string objectName)
    {
        _profile = CometContentData.Get(objectName);
        _profileLoaded = !string.IsNullOrEmpty(_profile.id);

        if (!_profileLoaded)
        {
            _profile = new CometContentData.ContentEntry
            {
                activityStrength = 1f,
                comaColor = DefaultComaColor,
                comaMaxScale = 1.5f,
                dormantThresholdAu = 4f
            };
        }
    }

    void Start()
    {
        if (_orbit == null)
            _orbit = GetComponent<CometOrbitController>();
        EnsureVisualAssets();
    }

    void CacheReferences()
    {
        if (nucleus == null)
            nucleus = transform.Find("Nucleus");
        if (coma == null)
            coma = transform.Find("Coma");
        if (dustTrail == null)
            dustTrail = transform.Find("DustTail")?.GetComponent<TrailRenderer>();
        if (ionTail == null)
            ionTail = transform.Find("IonTail")?.GetComponent<LineRenderer>();
        if (comaRenderer == null && coma != null)
            comaRenderer = coma.GetComponent<Renderer>();

        if (comaRenderer != null)
            _comaMaterial = comaRenderer.material;

        CacheNucleusMaterial();
    }

    void CacheNucleusMaterial()
    {
        if (nucleus == null)
            return;

        var renderer = nucleus.GetComponent<Renderer>();
        if (renderer == null || renderer.sharedMaterial == null)
            return;

        _nucleusBaseMaterial = renderer.sharedMaterial;
        if (_nucleusBaseMaterial.HasProperty("_BaseColor"))
            _nucleusBaseColor = _nucleusBaseMaterial.GetColor("_BaseColor");
    }

    public void EnsureVisualAssets()
    {
        if (comaRenderer == null && coma != null)
            comaRenderer = coma.GetComponent<Renderer>();

        if (comaRenderer != null && comaRenderer.sharedMaterial == null)
        {
            var comaMat = CometGraphicsLibrary.LoadComaMaterial();
            if (comaMat != null)
            {
                comaRenderer.sharedMaterial = comaMat;
                _comaMaterial = comaRenderer.material;
            }
        }
        else if (comaRenderer != null && _comaMaterial == null)
        {
            _comaMaterial = comaRenderer.material;
        }

        if (nucleus != null)
        {
            var nucleusRenderer = nucleus.GetComponent<Renderer>();
            if (nucleusRenderer != null && nucleusRenderer.sharedMaterial == null)
            {
                string variant = _profileLoaded ? _profile.nucleusMaterialVariant : "dusty";
                var nucleusMat = CometGraphicsLibrary.LoadNucleusMaterial(variant);
                if (nucleusMat != null)
                    nucleusRenderer.sharedMaterial = nucleusMat;
            }

            CacheNucleusMaterial();
        }

        if (dustTrail != null && dustTrail.sharedMaterial == null)
        {
            var trailMat = CometGraphicsLibrary.LoadDustTrailMaterial();
            if (trailMat != null)
                dustTrail.sharedMaterial = trailMat;
        }

        if (ionTail != null && ionTail.sharedMaterial == null)
        {
            var ionMat = CometGraphicsLibrary.LoadIonMaterial();
            if (ionMat != null)
                ionTail.sharedMaterial = ionMat;
        }
    }

    void CacheBaseValues()
    {
        if (coma != null)
            _baseComaScale = coma.localScale.x;
        if (dustTrail != null)
            _baseDustStartWidth = dustTrail.startWidth;
        if (ionTail != null)
        {
            _baseIonStartWidth = ionTail.startWidth;
            if (ionTail.positionCount >= 2)
                _baseIonLength = Vector3.Distance(ionTail.GetPosition(0), ionTail.GetPosition(1));
        }
    }

    public void SetReferences(Transform nucleusTransform, Transform comaTransform, TrailRenderer dust,
        LineRenderer ion, Renderer comaRender)
    {
        nucleus = nucleusTransform;
        coma = comaTransform;
        dustTrail = dust;
        ionTail = ion;
        comaRenderer = comaRender;
        CacheReferences();
        CacheBaseValues();
    }

    void LateUpdate()
    {
        if (_orbit == null)
            _orbit = GetComponent<CometOrbitController>();

        if (_sun == null)
        {
            var sunGo = GameObject.Find("Sun");
            if (sunGo != null)
                _sun = sunGo.transform;
            if (_sun == null)
                return;
        }

        var scaleController = FindFirstObjectByType<SolarSystemScaleController>();
        if (scaleController != null)
            _auToUnity = scaleController.AuToUnity;

        if (_orbit != null)
            _perihelionAu = _orbit.PerihelionAu;

        float distanceAu = GetHeliocentricDistanceAu();
        float activity = EvaluateActivity(distanceAu);
        ActivityStage stage = ClassifyStage(distanceAu, activity);

        ApplyNucleus(stage);
        ApplyComa(activity, stage);
        ApplyDustTrail(activity, stage);
        ApplyIonTail(activity, stage, distanceAu);
    }

    float GetHeliocentricDistanceAu()
    {
        if (_orbit != null)
            return _orbit.GetHeliocentricDistanceAu();

        if (_sun == null)
            return semiMajorAxisAu;

        return Vector3.Distance(transform.position, _sun.position) / _auToUnity;
    }

    float EvaluateActivity(float distanceAu)
    {
        float dormantThreshold = _profile.dormantThresholdAu;
        if (distanceAu >= dormantThreshold)
            return 0f;

        float q = _perihelionAu;
        float flux = Mathf.Pow(q / Mathf.Max(distanceAu, q), 2f);
        float activity = _profile.activityStrength * flux;

        float fade = 1f - Mathf.InverseLerp(dormantThreshold * 0.85f, dormantThreshold, distanceAu);
        return Mathf.Clamp01(activity * fade);
    }

    ActivityStage ClassifyStage(float distanceAu, float activity)
    {
        if (activity <= 0.03f)
            return ActivityStage.Dormant;

        float dormantThreshold = _profile.dormantThresholdAu;
        float midDistance = Mathf.Lerp(_perihelionAu, dormantThreshold, 0.45f);

        if (distanceAu <= _perihelionAu * 1.15f || activity > 0.85f)
            return ActivityStage.Outburst;
        if (activity > 0.45f || distanceAu < midDistance)
            return ActivityStage.Active;
        if (distanceAu < dormantThreshold)
            return ActivityStage.Developing;

        return ActivityStage.Dormant;
    }

    void ApplyNucleus(ActivityStage stage)
    {
        if (nucleus == null)
            return;

        var renderer = nucleus.GetComponent<Renderer>();
        if (renderer == null)
            return;

        if (_nucleusBaseMaterial != null)
            renderer.sharedMaterial = _nucleusBaseMaterial;

        float brighten = stage == ActivityStage.Dormant ? 1f : 1.15f;
        Color c = _nucleusBaseColor * brighten;
        if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_BaseColor"))
            renderer.material.SetColor("_BaseColor", c);
    }

    void ApplyComa(float activity, ActivityStage stage)
    {
        if (coma == null)
            return;

        if (comaRenderer != null)
            comaRenderer.enabled = true;

        bool active = activity > 0.03f;
        float maxScale = _baseComaScale * _profile.comaMaxScale * 2.5f;
        float scale = active
            ? Mathf.Lerp(_baseComaScale * 0.15f, maxScale, activity)
            : _baseComaScale * 0.12f;
        coma.localScale = Vector3.one * scale;

        if (_comaMaterial != null)
        {
            Color baseColor = _profile.comaColor;
            baseColor.a = active
                ? Mathf.Lerp(0f, 0.7f, activity)
                : 0.12f;
            if (stage == ActivityStage.Outburst)
                baseColor.a = Mathf.Min(0.85f, baseColor.a * 1.2f);

            _comaMaterial.SetColor("_AtmosphereColor", baseColor);
            _comaMaterial.SetFloat("_SunInfluence", active
                ? Mathf.Lerp(0.15f, 0.95f, activity)
                : 0.2f);
        }
    }

    void ApplyDustTrail(float activity, ActivityStage stage)
    {
        if (dustTrail == null)
            return;

        bool visible = activity > 0.08f;
        dustTrail.emitting = visible;
        if (!visible)
            return;

        float widthMul = stage == ActivityStage.Outburst ? 1.35f : 1f;
        dustTrail.startWidth = _baseDustStartWidth * Mathf.Lerp(0.05f, widthMul, activity);
        dustTrail.endWidth = dustTrail.startWidth * 0.05f;
        dustTrail.time = Mathf.Lerp(0.5f, 2.8f, activity);

        Color start = new Color(0.92f, 0.95f, 0.15f, Mathf.Lerp(0f, 0.9f, activity));
        Color end = new Color(0.98f, 0.97f, 0.28f, 0f);
        dustTrail.startColor = start;
        dustTrail.endColor = end;
    }

    void ApplyIonTail(float activity, ActivityStage stage, float distanceAu)
    {
        if (ionTail == null || _sun == null)
            return;

        bool visible = activity > 0.12f;
        ionTail.enabled = visible;
        if (!visible)
            return;

        Vector3 awayFromSun = (transform.position - _sun.position).normalized;
        if (awayFromSun.sqrMagnitude < 0.0001f)
            awayFromSun = -transform.forward;

        float dormantThreshold = _profile.dormantThresholdAu;
        float sunProximity = 1f - Mathf.InverseLerp(_perihelionAu, dormantThreshold, distanceAu);
        float lengthMul = stage == ActivityStage.Outburst ? 1.4f : 1f;
        float length = _baseIonLength * Mathf.Lerp(0.2f, lengthMul, activity);
        float headWidth = _baseIonStartWidth * Mathf.Lerp(0.1f, 1.3f, activity);
        headWidth *= Mathf.Lerp(0.75f, 1.5f, sunProximity);

        ionTail.positionCount = 2;
        ionTail.useWorldSpace = true;
        ionTail.SetPosition(0, transform.position);
        ionTail.SetPosition(1, transform.position + awayFromSun * length);
        ionTail.startWidth = headWidth;
        ionTail.endWidth = headWidth * 0.04f;

        float alpha = Mathf.Lerp(0f, 0.98f, activity) * Mathf.Lerp(0.6f, 1f, sunProximity);
        ionTail.startColor = new Color(0.35f, 0.72f, 1f, alpha);
        ionTail.endColor = new Color(0.5f, 0.85f, 1f, 0f);
    }
}
