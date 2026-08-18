using UnityEngine;

/// <summary>
/// Scene-static About panel actions: open the RuStore developer page and compose email.
/// Wired from Button onClick in the MainMenu scene — does not spawn UI.
/// </summary>
public class AboutDeveloperLinksController : MonoBehaviour
{
    public const string RuStoreUrl = "https://www.rustore.ru/catalog/developer/emahzj";
    public const string DeveloperEmail = "den.kolesov@gmail.com";

    const string KeySubject = "WriteToDeveloperEmailSubject";
    const string KeyChooser = "WriteToDeveloperMailChooser";
    const string FallbackSubject = "Solar System 3D — improvement suggestion";
    const string FallbackChooser = "Write to the developer";

    public void OpenRuStorePage()
    {
        Application.OpenURL(RuStoreUrl);
    }

    public void WriteToDeveloper()
    {
        string subject = ResolveTranslation(KeySubject, FallbackSubject);
        string chooserTitle = ResolveTranslation(KeyChooser, FallbackChooser);
        AndroidEmailHelper.OpenDeveloperEmail(DeveloperEmail, subject, chooserTitle);
    }

    static string ResolveTranslation(string key, string fallback)
    {
        if (LocalizationManager.Instance == null)
            return fallback;

        string value = LocalizationManager.Instance.GetTranslation(key);
        return string.IsNullOrEmpty(value) ? fallback : value;
    }
}
