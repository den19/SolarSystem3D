using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Captures a clean 9:16 simulation screenshot and opens the Android share sheet.
/// </summary>
public class SimulationShareController : MonoBehaviour
{
    public static SimulationShareController Instance { get; private set; }

    const int CaptureWidth = 1080;
    const int CaptureHeight = 1920;

    const string KeyShareNoInternet = "ShareNoInternetMessage";
    const string KeyShareFailed = "ShareFailedMessage";
    const string KeyShareCaptureFailed = "ShareCaptureFailedMessage";

    const string FallbackShareNoInternet = "No internet connection. Sharing is unavailable.";
    const string FallbackShareFailed = "Unable to share. Please try again.";
    const string FallbackShareCaptureFailed = "Failed to capture screenshot.";

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

    IEnumerator CaptureAndShareRoutine()
    {
        _isSharing = true;
        string pngPath = null;
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
        RenderTexture renderTexture = RenderTexture.GetTemporary(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
        string shareText = BuildShareText();
        Texture2D texture = null;

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

            byte[] pngBytes = texture.EncodeToPNG();
            pngPath = Path.Combine(GetOutputDirectory(), shareText + ".png");
            File.WriteAllBytes(pngPath, pngBytes);
            Debug.Log("SimulationShareController: screenshot saved to " + pngPath);
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

        if (texture != null)
            Destroy(texture);

        if (captureFailed || string.IsNullOrEmpty(pngPath))
        {
            ShowShareCaptureFailed();
            _isSharing = false;
            yield break;
        }

        if (!NetworkReachabilityHelper.HasInternet)
        {
            ShowShareNoInternet();
            _isSharing = false;
            yield break;
        }

        try
        {
            if (!AndroidShareHelper.TryShareImageWithText(pngPath, shareText))
                ShowShareFailed();
        }
        catch (Exception exception)
        {
            Debug.LogError("SimulationShareController: share failed. " + exception.Message);
            ShowShareFailed();
        }

        _isSharing = false;
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

    static void ShowShareNoInternet()
    {
        TransientMessageController.ShowLocalized(KeyShareNoInternet, FallbackShareNoInternet);
    }

    static void ShowShareFailed()
    {
        TransientMessageController.ShowLocalized(KeyShareFailed, FallbackShareFailed);
    }

    static void ShowShareCaptureFailed()
    {
        TransientMessageController.ShowLocalized(KeyShareCaptureFailed, FallbackShareCaptureFailed);
    }

    static string BuildShareText()
    {
        return "SolarSystem3D_" + DateTime.Now.ToString("yyyyMMddHHmmss");
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
}
