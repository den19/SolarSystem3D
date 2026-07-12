using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Writes Assets/Resources/BuildInfo.json with the local build date before each player build.
/// </summary>
public class BuildInfoGenerator : IPreprocessBuildWithReport
{
    const string RelativePath = "Assets/Resources/BuildInfo.json";

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        string directory = Path.Combine(Application.dataPath, "Resources");
        Directory.CreateDirectory(directory);

        string buildDate = DateTime.Now.ToString("yyyy-MM-dd");
        string json = "{\"buildDate\":\"" + buildDate + "\"}\n";
        string absolutePath = Path.Combine(Application.dataPath, "Resources", "BuildInfo.json");
        File.WriteAllText(absolutePath, json);

        AssetDatabase.ImportAsset(RelativePath);
        Debug.Log("BuildInfoGenerator: wrote " + RelativePath + " buildDate=" + buildDate);
    }
}
