using System;
using UnityEngine;

/// <summary>
/// Opens the Android system share chooser for a PNG file and optional text.
/// </summary>
public static class AndroidShareHelper
{
    public static void ShareImageWithText(string imagePath, string text)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext"))
            using (AndroidJavaClass fileProviderClass = new AndroidJavaClass("androidx.core.content.FileProvider"))
            using (AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent"))
            using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent"))
            {
                string authority = Application.identifier + ".fileprovider";
                using (AndroidJavaObject file = new AndroidJavaObject("java.io.File", imagePath))
                using (AndroidJavaObject uri = fileProviderClass.CallStatic<AndroidJavaObject>(
                           "getUriForFile", context, authority, file))
                {
                    string actionSend = intentClass.GetStatic<string>("ACTION_SEND");
                    intent.Call<AndroidJavaObject>("setAction", actionSend);
                    intent.Call<AndroidJavaObject>("setType", "image/png");
                    intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), uri);
                    intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);

                    int flagGrantRead = intentClass.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION");
                    intent.Call<AndroidJavaObject>("addFlags", flagGrantRead);

                    using (AndroidJavaObject chooser = intentClass.CallStatic<AndroidJavaObject>(
                               "createChooser", intent, "Share"))
                    {
                        activity.Call("startActivity", chooser);
                    }
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogError("AndroidShareHelper: failed to share image. " + exception.Message);
        }
#else
        Debug.Log("AndroidShareHelper: share skipped outside Android build. Image: " + imagePath + ", text: " + text);
#endif
    }
}
