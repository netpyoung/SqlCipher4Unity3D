using UnityEditor;

public class PackageTool
{
    [MenuItem("Package/Update Package")]
    private static void UpdatePackage()
    {
        const string version = "1.3.2";
        AssetDatabase.ExportPackage(new string[] { "Assets/SqlCipher4Unity3D" }, $"../SqlCipher4Unity3D-{version}.unitypackage", ExportPackageOptions.Recurse);
    }
}
