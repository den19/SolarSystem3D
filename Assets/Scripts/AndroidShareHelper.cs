using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Opens the Android system share chooser for a PNG file and optional text.
/// </summary>
public static class AndroidShareHelper
{
    public static bool TryShareImageWithText(string imagePath, string text)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
        {
            Debug.LogError("AndroidShareHelper: image file not found. path=" + imagePath);
            return false;
        }

        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaClass fileProviderClass = new AndroidJavaClass("androidx.core.content.FileProvider"))
            using (AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent"))
            using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent"))
            {
                string authority = Application.identifier + ".fileprovider";
                long fileSize = new FileInfo(imagePath).Length;
                Debug.Log("AndroidShareHelper: sharing path=" + imagePath + ", authority=" + authority + ", size=" + fileSize);

                using (AndroidJavaObject file = new AndroidJavaObject("java.io.File", imagePath))
                using (AndroidJavaObject uri = fileProviderClass.CallStatic<AndroidJavaObject>(
                           "getUriForFile", activity, authority, file))
                {
                    string actionSend = intentClass.GetStatic<string>("ACTION_SEND");
                    intent.Call<AndroidJavaObject>("setAction", actionSend);
                    intent.Call<AndroidJavaObject>("setType", "image/png");
                    intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), uri);
                    intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);

                    using (AndroidJavaClass clipDataClass = new AndroidJavaClass("android.content.ClipData"))
                    using (AndroidJavaObject clip = clipDataClass.CallStatic<AndroidJavaObject>("newRawUri", "", uri))
                    {
                        intent.Call("setClipData", clip);
                    }

                    int flagGrantRead = intentClass.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION");
                    int flagGrantWrite = intentClass.GetStatic<int>("FLAG_GRANT_WRITE_URI_PERMISSION");
                    int grantFlags = flagGrantRead | flagGrantWrite;
                    intent.Call<AndroidJavaObject>("addFlags", grantFlags);

                    try
                    {
                        GrantUriPermissionsToTargets(activity, intent, uri, grantFlags);
                    }
                    catch (Exception grantException)
                    {
                        Debug.LogWarning("AndroidShareHelper: grantUriPermission skipped. " + grantException.Message);
                    }

                    using (AndroidJavaObject chooser = intentClass.CallStatic<AndroidJavaObject>(
                               "createChooser", intent, "Share"))
                    {
                        chooser.Call<AndroidJavaObject>("addFlags", flagGrantRead);
                        activity.Call("startActivity", chooser);
                    }
                }
            }

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("AndroidShareHelper: failed to share image. " + exception);
            return false;
        }
#else
        Debug.Log("AndroidShareHelper: share skipped outside Android build. Image: " + imagePath + ", text: " + text);
        return true;
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    static void GrantUriPermissionsToTargets(
        AndroidJavaObject activity,
        AndroidJavaObject intent,
        AndroidJavaObject uri,
        int grantFlags)
    {
        using (AndroidJavaObject packageManager = activity.Call<AndroidJavaObject>("getPackageManager"))
        using (AndroidJavaClass packageManagerClass = new AndroidJavaClass("android.content.pm.PackageManager"))
        {
            int matchDefaultOnly = packageManagerClass.GetStatic<int>("MATCH_DEFAULT_ONLY");
            using (AndroidJavaObject resolveList = packageManager.Call<AndroidJavaObject>(
                       "queryIntentActivities", intent, matchDefaultOnly))
            {
                int count = resolveList.Call<int>("size");
                for (int i = 0; i < count; i++)
                {
                    using (AndroidJavaObject resolveInfo = resolveList.Call<AndroidJavaObject>("get", i))
                    using (AndroidJavaObject activityInfo = resolveInfo.Get<AndroidJavaObject>("activityInfo"))
                    {
                        string packageName = activityInfo.Get<string>("packageName");
                        activity.Call("grantUriPermission", packageName, uri, grantFlags);
                    }
                }
            }
        }
    }
#endif

    public static void ShareImageWithText(string imagePath, string text)
    {
        TryShareImageWithText(imagePath, text);
    }
}
