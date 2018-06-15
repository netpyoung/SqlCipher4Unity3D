using UnityEditor;

public class PackageTool
{
	[MenuItem("Package/Update Package")]
	static void UpdatePackage()
	{
		const string version = "v1.0.0";
		AssetDatabase.ExportPackage(new string[] {"Assets/SqlCipher4Unity3D"}, $"../SqlCipher4Unity3D-{version}.unitypackage", ExportPackageOptions.Recurse);
	}
}
