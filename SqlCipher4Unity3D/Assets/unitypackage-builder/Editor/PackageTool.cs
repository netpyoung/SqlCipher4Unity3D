using UnityEditor;

public class PackageTool
{
    [MenuItem("Package/Update Package")]
    private static void UpdatePackage()
    {
        const string version = "v1.0.1";
        AssetDatabase.ExportPackage(new string[] { "Assets/SqlCipher4Unity3D" }, $"../SqlCipher4Unity3D-{version}.unitypackage", ExportPackageOptions.Recurse);
    }
}
