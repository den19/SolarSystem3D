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

    [Header("Orbital data (set at spawn)")]
    [SerializeField] float semiMajorAxisAu = 2.5f;
    [SerializeField] float eccentricity = 0.5f;

    [Header("Distance thresholds (AU)")]
    [SerializeField] float dormantThresholdAu = 5f;
    [SerializeField] float activeThresholdAu = 2f;
    [SerializeField] float outburstPerihelionFactor = 1.3f;
    [SerializeField] float minimumVisualActivity = 0.4f;

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

    public void Initialize(CometCatalog.CometDefinition definition, Transform sun, float auToUnity)
    {
        semiMajorAxisAu = definition.semiMajorAxisAu;
        eccentricity = definition.eccentricity;
        _sun = sun;
        _auToUnity = Mathf.Max(0.001f, auToUnity);
        _perihelionAu = semiMajorAxisAu * (1f - eccentricity);
        _orbit = GetComponent<CometOrbitController>();

        CacheReferences();
        CacheBaseValues();
        EnsureVisualAssets();
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

        float distanceAu = Vector3.Distance(transform.position, _sun.position) / _auToUnity;
        float activity = EvaluateActivity(distanceAu);
        activity = Mathf.Max(minimumVisualActivity, activity);
        ActivityStage stage = ClassifyStage(distanceAu, activity);

        ApplyNucleus(stage);
        ApplyComa(activity, stage);
        ApplyDustTrail(activity, stage);
        ApplyIonTail(activity, stage, distanceAu);
    }

    float EvaluateActivity(float distanceAu)
    {
        float distanceFactor = 0f;
        if (distanceAu < dormantThresholdAu)
        {
            if (distanceAu <= _perihelionAu * outburstPerihelionFactor)
                distanceFactor = 1f;
            else if (distanceAu <= activeThresholdAu)
            {
                float t = Mathf.InverseLerp(activeThresholdAu, _perihelionAu * outburstPerihelionFactor, distanceAu);
                distanceFactor = Mathf.Lerp(0.75f, 1f, 1f - t);
            }
            else
            {
                float developingT = Mathf.InverseLerp(dormantThresholdAu, activeThresholdAu, distanceAu);
                distanceFactor = Mathf.SmoothStep(0f, 0.7f, 1f - developingT);
            }
        }

        float angleFactor = EvaluateOrbitAngleActivity();
        return Mathf.Clamp01(Mathf.Max(distanceFactor, angleFactor));
    }

    float EvaluateOrbitAngleActivity()
    {
        if (_sun == null)
            return 0f;

        Vector3 offset = transform.position - _sun.position;
        if (offset.sqrMagnitude < 0.0001f)
            return 0f;

        float angle = Mathf.Atan2(offset.z, offset.x);
        if (_orbit != null)
            angle = _orbit.OrbitAngle;

        // Near the Sun side of the orbit the comet is most active.
        float sunSide = (Mathf.Cos(angle) + 1f) * 0.5f;
        return Mathf.SmoothStep(0.15f, 1f, sunSide);
    }

    ActivityStage ClassifyStage(float distanceAu, float activity)
    {
        if (activity <= 0.05f && distanceAu > dormantThresholdAu)
            return ActivityStage.Dormant;
        if (distanceAu < _perihelionAu * outburstPerihelionFactor || activity > 0.85f)
            return ActivityStage.Outburst;
        if (activity > 0.55f || distanceAu < activeThresholdAu)
            return ActivityStage.Active;
        return ActivityStage.Developing;
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

        bool visible = activity > 0.08f;
        if (comaRenderer != null)
            comaRenderer.enabled = visible;

        if (!visible)
            return;

        float scale = _baseComaScale * Mathf.Lerp(1f, 1.8f, activity);
        coma.localScale = Vector3.one * scale;

        if (_comaMaterial != null)
        {
            Color baseColor = new Color(0.55f, 0.92f, 0.78f, Mathf.Lerp(0.18f, 0.55f, activity));
            if (stage == ActivityStage.Outburst)
                baseColor.a = Mathf.Min(0.65f, baseColor.a * 1.25f);

            _comaMaterial.SetColor("_AtmosphereColor", baseColor);
            _comaMaterial.SetFloat("_SunInfluence", Mathf.Lerp(0.4f, 0.95f, activity));
        }
    }

    void ApplyDustTrail(float activity, ActivityStage stage)
    {
        if (dustTrail == null)
            return;

        bool visible = activity > 0.12f;
        dustTrail.emitting = visible;
        if (!visible)
            return;

        float widthMul = stage == ActivityStage.Outburst ? 1.25f : 1f;
        dustTrail.startWidth = _baseDustStartWidth * Mathf.Lerp(0.15f, widthMul, activity);
        dustTrail.endWidth = dustTrail.startWidth * 0.05f;
        dustTrail.time = Mathf.Lerp(0.8f, 2.5f, activity);

        Color start = new Color(0.92f, 0.95f, 0.15f, Mathf.Lerp(0.2f, 0.85f, activity));
        Color end = new Color(0.98f, 0.97f, 0.28f, 0f);
        dustTrail.startColor = start;
        dustTrail.endColor = end;
    }

    void ApplyIonTail(float activity, ActivityStage stage, float distanceAu)
    {
        if (ionTail == null || _sun == null)
            return;

        bool visible = activity > 0.2f;
        ionTail.enabled = visible;
        if (!visible)
            return;

        Vector3 awayFromSun = (transform.position - _sun.position).normalized;
        if (awayFromSun.sqrMagnitude < 0.0001f)
            awayFromSun = -transform.forward;

        float sunProximity = Mathf.InverseLerp(dormantThresholdAu, _perihelionAu, distanceAu);
        float lengthMul = stage == ActivityStage.Outburst ? 1.35f : 1f;
        float length = _baseIonLength * Mathf.Lerp(0.45f, lengthMul, activity);
        float headWidth = _baseIonStartWidth * Mathf.Lerp(0.35f, 1.2f, activity);
        headWidth *= Mathf.Lerp(0.85f, 1.45f, sunProximity);

        ionTail.positionCount = 2;
        ionTail.useWorldSpace = true;
        ionTail.SetPosition(0, transform.position);
        ionTail.SetPosition(1, transform.position + awayFromSun * length);
        ionTail.startWidth = headWidth;
        ionTail.endWidth = headWidth * 0.04f;

        float alpha = Mathf.Lerp(0.55f, 0.98f, activity) * Mathf.Lerp(0.7f, 1f, sunProximity);
        ionTail.startColor = new Color(0.35f, 0.72f, 1f, alpha);
        ionTail.endColor = new Color(0.5f, 0.85f, 1f, 0f);
    }
}
