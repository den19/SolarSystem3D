using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Captures simulation screenshots and opens the Android share sheet.
/// Short share: clean 9:16 camera frame without UI.
/// Long share: screen capture with visible UI, cropped to safe area.
/// </summary>
public class SimulationShareController : MonoBehaviour
{
    public static SimulationShareController Instance { get; private set; }

    const int CaptureWidth = 1080;
    const int CaptureHeight = 1920;

    const string KeyShareFailed = "ShareFailedMessage";
    const string KeyShareCaptureFailed = "ShareCaptureFailedMessage";
    const string KeyShareScreenshotText = "ShareScreenshotText";
    const string ShareButtonName = "ShareButton";

    const string FallbackShareFailed = "Unable to share. Please try again.";
    const string FallbackShareCaptureFailed = "Failed to capture screenshot.";
    const string FallbackShareScreenshotText =
        "Solar System 3D by developer Kolesoff Den (densappstudio)\nVersion: {0}\nBuild date: {1}";

    LookAtTarget _lookAtTarget;
    Camera _minimapCamera;
    bool _isSharing;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        _lookAtTarget = FindFirstObjectByType<LookAtTarget>();

        GameObject minimapGo = GameObject.Find("Minimap Camera");
        if (minimapGo != null)
            _minimapCamera = minimapGo.GetComponent<Camera>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RequestShare()
    {
        if (_isSharing)
            return;

        StartCoroutine(CaptureAndShareRoutine());
    }

    public void RequestShareWithUi()
    {
        if (_isSharing)
            return;

        StartCoroutine(CaptureScreenWithUiAndShareRoutine());
    }

    IEnumerator CaptureAndShareRoutine()
    {
        _isSharing = true;
        Texture2D texture = null;
        RenderTexture renderTexture = null;
        bool captureFailed = false;

        Camera captureCamera = ResolveCaptureCamera();
        if (captureCamera == null)
        {
            Debug.LogError("SimulationShareController: no active camera found for capture.");
            ShowShareCaptureFailed();
            _isSharing = false;
            yield break;
        }

        RenderTexture previousTarget = captureCamera.targetTexture;
        float previousAspect = captureCamera.aspect;
        bool minimapWasEnabled = _minimapCamera != null && _minimapCamera.enabled;
        renderTexture = RenderTexture.GetTemporary(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);

        if (_minimapCamera != null)
            _minimapCamera.enabled = false;

        captureCamera.aspect = 9f / 16f;
        captureCamera.targetTexture = renderTexture;
        captureCamera.Render();

        yield return new WaitForEndOfFrame();

        try
        {
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = renderTexture;

            texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
            texture.Apply();

            RenderTexture.active = previousActive;
        }
        catch (Exception exception)
        {
            captureFailed = true;
            Debug.LogError("SimulationShareController: capture failed. " + exception.Message);
        }

        captureCamera.targetTexture = previousTarget;
        captureCamera.aspect = previousAspect;

        if (_minimapCamera != null)
            _minimapCamera.enabled = minimapWasEnabled;

        if (renderTexture != null)
            RenderTexture.ReleaseTemporary(renderTexture);

        if (captureFailed || texture == null)
        {
            if (texture != null)
                Destroy(texture);

            ShowShareCaptureFailed();
            _isSharing = false;
            yield break;
        }

        yield return FinalizeShare(texture, BuildShareFileName(includeUi: false));
        _isSharing = false;
    }

    IEnumerator CaptureScreenWithUiAndShareRoutine()
    {
        _isSharing = true;
        Texture2D screenTexture = null;
        Texture2D croppedTexture = null;
        Texture2D scaledTexture = null;
        bool captureFailed = false;

        yield return new WaitForEndOfFrame();

        ResetShareButtonVisual();

        try
        {
            screenTexture = ScreenCapture.CaptureScreenshotAsTexture();
            if (screenTexture == null)
                throw new InvalidOperationException("ScreenCapture returned null.");

            Rect safeArea = Screen.safeArea;
            int x = Mathf.Clamp(Mathf.RoundToInt(safeArea.x), 0, Mathf.Max(0, screenTexture.width - 1));
            int y = Mathf.Clamp(Mathf.RoundToInt(safeArea.y), 0, Mathf.Max(0, screenTexture.height - 1));
            int width = Mathf.Clamp(Mathf.RoundToInt(safeArea.width), 1, screenTexture.width - x);
            int height = Mathf.Clamp(Mathf.RoundToInt(safeArea.height), 1, screenTexture.height - y);

            Color[] pixels = screenTexture.GetPixels(x, y, width, height);
            croppedTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
            croppedTexture.SetPixels(pixels);
            croppedTexture.Apply();

            scaledTexture = ScaleToCaptureSize(croppedTexture);
        }
        catch (Exception exception)
        {
            captureFailed = true;
            Debug.LogError("SimulationShareController: UI capture failed. " + exception.Message);
        }
        finally
        {
            if (screenTexture != null)
                Destroy(screenTexture);

            if (croppedTexture != null)
                Destroy(croppedTexture);
        }

        if (captureFailed || scaledTexture == null)
        {
            if (scaledTexture != null)
                Destroy(scaledTexture);

            ShowShareCaptureFailed();
            _isSharing = false;
            yield break;
        }

        yield return FinalizeShare(scaledTexture, BuildShareFileName(includeUi: true));
        _isSharing = false;
    }

    IEnumerator FinalizeShare(Texture2D texture, string fileName)
    {
        string shareText = BuildShareText();
        bool shareFailed = false;

        try
        {
            byte[] pngBytes = texture.EncodeToPNG();
            string pngPath = Path.Combine(GetOutputDirectory(), fileName);
            File.WriteAllBytes(pngPath, pngBytes);
            Debug.Log("SimulationShareController: screenshot saved to " + pngPath);

            if (!AndroidShareHelper.TryShareImageWithText(pngPath, shareText))
                shareFailed = true;
        }
        catch (Exception exception)
        {
            shareFailed = true;
            Debug.LogError("SimulationShareController: share failed. " + exception.Message);
        }
        finally
        {
            if (texture != null)
                Destroy(texture);
        }

        if (shareFailed)
            ShowShareFailed();

        yield break;
    }

    static Texture2D ScaleToCaptureSize(Texture2D source)
    {
        RenderTexture renderTexture = RenderTexture.GetTemporary(CaptureWidth, CaptureHeight, 0, RenderTextureFormat.ARGB32);
        RenderTexture previousActive = RenderTexture.active;

        try
        {
            Graphics.Blit(source, renderTexture);
            RenderTexture.active = renderTexture;

            Texture2D scaled = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
            scaled.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
            scaled.Apply();
            return scaled;
        }
        finally
        {
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(renderTexture);
        }
    }

    static void ResetShareButtonVisual()
    {
        GameObject shareButtonGo = GameObject.Find(ShareButtonName);
        if (shareButtonGo == null)
            return;

        if (shareButtonGo.TryGetComponent(out Selectable selectable))
            selectable.OnDeselect(null);

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null && eventSystem.currentSelectedGameObject == shareButtonGo)
            eventSystem.SetSelectedGameObject(null);
    }

    Camera ResolveCaptureCamera()
    {
        if (_lookAtTarget != null)
        {
            GameObject detailCameraGo = _lookAtTarget.GetActiveDetailCamera();
            if (detailCameraGo != null && detailCameraGo.TryGetComponent(out Camera detailCamera) && detailCamera.enabled)
                return detailCamera;
        }

        if (Camera.main != null && Camera.main.enabled)
            return Camera.main;

        Camera[] cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera == null || !camera.enabled || camera == _minimapCamera)
                continue;

            return camera;
        }

        return null;
    }

    static void ShowShareFailed()
    {
        TransientMessageController.ShowLocalized(KeyShareFailed, FallbackShareFailed);
    }

    static void ShowShareCaptureFailed()
    {
        TransientMessageController.ShowLocalized(KeyShareCaptureFailed, FallbackShareCaptureFailed);
    }

    static string BuildShareFileName(bool includeUi)
    {
        string prefix = includeUi ? "SolarSystem3D_UI_" : "SolarSystem3D_";
        return prefix + DateTime.Now.ToString("yyyyMMddHHmmss") + ".png";
    }

    static string BuildShareText()
    {
        string version = Application.version;
        string buildDate = ResolveBuildDate();
        string format = FallbackShareScreenshotText;

        if (LocalizationManager.Instance != null)
        {
            string translation = LocalizationManager.Instance.GetTranslation(KeyShareScreenshotText);
            if (!string.IsNullOrEmpty(translation))
                format = translation;
        }

        return string.Format(format, version, buildDate);
    }

    static string ResolveBuildDate()
    {
        TextAsset buildInfoAsset = Resources.Load<TextAsset>("BuildInfo");
        if (buildInfoAsset != null && !string.IsNullOrEmpty(buildInfoAsset.text))
        {
            try
            {
                BuildInfoData data = JsonUtility.FromJson<BuildInfoData>(buildInfoAsset.text);
                if (data != null && !string.IsNullOrEmpty(data.buildDate))
                    return data.buildDate;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("SimulationShareController: failed to parse BuildInfo. " + exception.Message);
            }
        }

        return DateTime.Now.ToString("yyyy-MM-dd");
    }

    static string GetOutputDirectory()
    {
#if UNITY_EDITOR
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string directory = Path.Combine(projectRoot, "Screenshots");
#else
        string directory = Application.temporaryCachePath;
#endif
        Directory.CreateDirectory(directory);
        return directory;
    }

    [Serializable]
    class BuildInfoData
    {
        public string buildDate;
    }
}
