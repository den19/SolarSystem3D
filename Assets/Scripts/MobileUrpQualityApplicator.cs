using SolarSystemApp;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Applies Mobile URP renderScale / MSAA from GraphicsTierSettings on Android and iOS.
/// </summary>
public class MobileUrpQualityApplicator : MonoBehaviour
{
    public const float BalancedRenderScale = 0.8f;
    public const float HighRenderScale = 1f;
    public const int BalancedMsaa = 1;
    public const int HighMsaa = 2;

    static MobileUrpQualityApplicator instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null)
            return;

        var go = new GameObject("MobileUrpQualityApplicator_Persistent");
        instance = go.AddComponent<MobileUrpQualityApplicator>();
        DontDestroyOnLoad(go);
    }

    void OnEnable()
    {
        GraphicsTierSettings.EffectiveTierChanged += OnEffectiveTierChanged;
        GraphicsSettings.UseExtraGraphicsChanged += OnExtraGraphicsChanged;
        Apply();
    }

    void OnDisable()
    {
        GraphicsTierSettings.EffectiveTierChanged -= OnEffectiveTierChanged;
        GraphicsSettings.UseExtraGraphicsChanged -= OnExtraGraphicsChanged;
    }

    void Start()
    {
        Apply();
    }

    void OnEffectiveTierChanged(MobileGraphicsTier _) => Apply();

    void OnExtraGraphicsChanged(bool _)
    {
        GraphicsTierSettings.RefreshEffectiveTier(invokeEvent: true);
        Apply();
    }

    public static void Apply()
    {
        if (!IsMobileRuntime())
            return;

        var urp = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        if (urp == null)
            return;

        bool high = GraphicsTierSettings.IsHighEffective;
        float scale = high ? HighRenderScale : BalancedRenderScale;
        int msaa = high ? HighMsaa : BalancedMsaa;

        if (!Mathf.Approximately(urp.renderScale, scale))
            urp.renderScale = scale;

        if (urp.msaaSampleCount != msaa)
            urp.msaaSampleCount = msaa;
    }

    static bool IsMobileRuntime()
    {
        return Application.platform == RuntimePlatform.Android
            || Application.platform == RuntimePlatform.IPhonePlayer;
    }
}
