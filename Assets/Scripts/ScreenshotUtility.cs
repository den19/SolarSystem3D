/*
 *           ~~ Screenshot Utility ~~ 
 *  Takes a screenshot of the game window with its
 *  current resolution.
 * 
 *  Notes:
 *    - Editor: images are stored in the project's Screenshots folder.
 *    - Android: images are stored in Application.persistentDataPath.
 * 
 *    - ScaleFactor - If the resolution is 1024x768, and the scale factor
 *      is 2, the screenshot will be saved as 2048x1536.
 * 
 *    - The mouse is not captured in the screenshot.
 * 
 *  Created by Brian Winn
 *  Michigan State University
 *  Games for Entertainment and Learning (GEL) Lab
 */

using UnityEngine;
using System.IO;

/// <summary>
/// Handles taking a screenshot of game window on Android and in the Unity Editor.
/// </summary>
public class ScreenshotUtility : MonoBehaviour
{
    public static ScreenshotUtility screenShotUtility;

    #region Public Variables
    public string m_ScreenshotKey = "s";
    public int m_ScaleFactor = 1;
    #endregion

    #region Private Variables
    private int m_ImageCount;
    #endregion

    #region Constants
    private const string ImageCntKey = "IMAGE_CNT";
    #endregion

    static bool IsSupportedPlatform =>
        Application.isEditor || Application.platform == RuntimePlatform.Android;

    void Awake()
    {
        screenShotUtility = this;
        m_ImageCount = PlayerPrefs.GetInt(ImageCntKey, 0);

        if (!IsSupportedPlatform)
            enabled = false;
    }

    void Update()
    {
        if (!Application.isEditor || !IsSupportedPlatform)
            return;

        if (Input.GetKeyDown(m_ScreenshotKey.ToLower()))
            TakeScreenshot();
    }

    public void TakeScreenshot()
    {
        if (!IsSupportedPlatform)
            return;

        PlayerPrefs.SetInt(ImageCntKey, ++m_ImageCount);

        int width = Screen.width * m_ScaleFactor;
        int height = Screen.height * m_ScaleFactor;
        string fileName = "Screenshot_" + width + "x" + height + "_" + m_ImageCount + ".png";
        string directory = GetScreenshotDirectory();
        Directory.CreateDirectory(directory);

        string fullPath = Path.Combine(directory, fileName);
        ScreenCapture.CaptureScreenshot(fullPath, m_ScaleFactor);
        Debug.Log("Screenshot saved to: " + fullPath);
    }

    static string GetScreenshotDirectory()
    {
#if UNITY_EDITOR
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Screenshots");
#elif UNITY_ANDROID
        return Application.persistentDataPath;
#else
        return Application.persistentDataPath;
#endif
    }
}
