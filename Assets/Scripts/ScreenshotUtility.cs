/*
 *           ~~ Screenshot Utility ~~ 
 *  Takes a screenshot of the game window with its
 *  current resolution.
 * 
 *  Notes:
 *    - Images are stored in a Screenshots folder within the Unity project directory.
 * 
 *    - Images will copied over if player prefs are reset!
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
using System.Collections;
using System.IO; // included for access to File IO such as Directory class

/// <summary>
/// Handles taking a screenshot of game window.
/// </summary>
public class ScreenshotUtility : MonoBehaviour
{
    // static reference to ScreenshotUtility so can be called from other scripts directly (not just through gameobject component)
    public static ScreenshotUtility screenShotUtility;

    #region Public Variables
    // Should this run on a build of the game
    public bool runOnBuild = false;
    // The key used to take a screenshot
    public string m_ScreenshotKey = "s";
    // The amount to scale the screenshot
    public int m_ScaleFactor = 1;
    #endregion

    #region Private Variables
    // The number of screenshots taken
    private int m_ImageCount = 0;
    #endregion

    #region Constants
    // The key used to get/set the number of images
    private const string ImageCntKey = "IMAGE_CNT";
    #endregion

    /// <summary>
    /// Lets the screenshot utility persist through scenes.
    /// </summary>
    void Awake()
    {
        // Скрипт полностью отключен по запросу пользователя для стабильности на Android
        this.enabled = false;
        return;
    }

    /// <summary>
    /// Called once per frame. Handles the input.
    /// </summary>
    void Update()
    {

        // Checks for input
        if (Input.GetKeyDown(m_ScreenshotKey.ToLower()))
        {
            // Saves the current image count
            PlayerPrefs.SetInt(ImageCntKey, ++m_ImageCount);

            // Adjusts the height and width for the file name
            int width = Screen.width * m_ScaleFactor;
            int height = Screen.height * m_ScaleFactor;

            // Takes the screenshot with filename "Screenshot_WIDTHxHEIGHT_IMAGECOUNT.png"
            // and save it in the Screenshots folder
            ScreenCapture.CaptureScreenshot("Screenshots/Screenshot_" +
                                          +width + "x" + height
                                          + "_"
                                          + m_ImageCount
                                          + ".png",
                                          m_ScaleFactor);
        }
    }
}
