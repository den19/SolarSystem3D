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

        Camera captureCamera = ResolveCaptureCamera();
        if (captureCamera == null)
        {
            Debug.LogError("SimulationShareController: no active camera found for capture.");
            _isSharing = false;
            yield break;
        }

        RenderTexture previousTarget = captureCamera.targetTexture;
        float previousAspect = captureCamera.aspect;
        bool minimapWasEnabled = _minimapCamera != null && _minimapCamera.enabled;
        RenderTexture renderTexture = RenderTexture.GetTemporary(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
        string pngPath = null;
        string shareText = BuildShareText();

        if (_minimapCamera != null)
            _minimapCamera.enabled = false;

        captureCamera.aspect = 9f / 16f;
        captureCamera.targetTexture = renderTexture;
        captureCamera.Render();

        yield return new WaitForEndOfFrame();

        Texture2D texture = null;
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

        if (!string.IsNullOrEmpty(pngPath))
            AndroidShareHelper.ShareImageWithText(pngPath, shareText);

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
