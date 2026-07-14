using System.IO;
using UnityEditor.Android;

public sealed class MicPermissionNamespacePostprocessor : IPostGenerateGradleAndroidProject
{
    private const string Namespace = "com.fixskill.fixskill.micpermission";

    public int callbackOrder => 0;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string micPermissionPath = Path.Combine(path, "MicPermission.androidlib");
        PatchBuildGradle(Path.Combine(micPermissionPath, "build.gradle"));
        PatchManifest(Path.Combine(micPermissionPath, "AndroidManifest.xml"));
    }

    private static void PatchBuildGradle(string buildGradlePath)
    {
        if (!File.Exists(buildGradlePath))
        {
            return;
        }

        string content = File.ReadAllText(buildGradlePath);
        if (content.Contains("namespace "))
        {
            return;
        }

        content = content.Replace("android {", $"android {{\n    namespace '{Namespace}'");
        File.WriteAllText(buildGradlePath, content);
    }

    private static void PatchManifest(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return;
        }

        string content = File.ReadAllText(manifestPath);
        if (content.Contains(" package="))
        {
            return;
        }

        content = content.Replace("<manifest ", $"<manifest package=\"{Namespace}\" ");
        File.WriteAllText(manifestPath, content);
    }
}
