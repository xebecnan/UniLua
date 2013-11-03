
using UnityEngine;
using UnityEditor;

public class ExportAssetBundles {
	[MenuItem("Assets/Build AssetBundle")]
	static void Export() {
		foreach(Object o in Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets)) {
			BuildPipeline.BuildAssetBundle( o, null, "Assets/StreamingAssets/" + o.name + ".unity3d", BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets, BuildTarget.StandaloneWindows );
		}
	}
}
