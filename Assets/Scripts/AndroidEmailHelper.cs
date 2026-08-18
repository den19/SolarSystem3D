using System;
using UnityEngine;

/// <summary>
/// Opens an email composer to a given address. On Android uses ACTION_SENDTO
/// (mailto) so only email apps appear in the chooser, not image share targets.
/// </summary>
public static class AndroidEmailHelper
{
    public static void OpenDeveloperEmail(string email, string subject, string chooserTitle)
    {
        if (string.IsNullOrEmpty(email))
        {
            Debug.LogError("AndroidEmailHelper: email address is empty.");
            return;
        }

        string mailto = BuildMailto(email, subject);

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!TryOpenEmailIntent(email, subject, chooserTitle, mailto))
            Application.OpenURL(mailto);
#else
        Application.OpenURL(mailto);
#endif
    }

    static string BuildMailto(string email, string subject)
    {
        string mailto = "mailto:" + email;
        if (!string.IsNullOrEmpty(subject))
            mailto += "?subject=" + Uri.EscapeDataString(subject);
        return mailto;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    static bool TryOpenEmailIntent(string email, string subject, string chooserTitle, string mailto)
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent"))
            using (AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri"))
            using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent"))
            {
                string actionSendTo = intentClass.GetStatic<string>("ACTION_SENDTO");
                string extraSubject = intentClass.GetStatic<string>("EXTRA_SUBJECT");

                using (AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("parse", mailto))
                {
                    intent.Call<AndroidJavaObject>("setAction", actionSendTo);
                    intent.Call<AndroidJavaObject>("setData", uri);

                    if (!string.IsNullOrEmpty(subject))
                        intent.Call<AndroidJavaObject>("putExtra", extraSubject, subject);

                    string title = string.IsNullOrEmpty(chooserTitle) ? email : chooserTitle;
                    using (AndroidJavaObject chooser = intentClass.CallStatic<AndroidJavaObject>(
                               "createChooser", intent, title))
                    {
                        activity.Call("startActivity", chooser);
                    }
                }
            }

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("AndroidEmailHelper: failed to open email intent. " + exception);
            return false;
        }
    }
#endif
}
