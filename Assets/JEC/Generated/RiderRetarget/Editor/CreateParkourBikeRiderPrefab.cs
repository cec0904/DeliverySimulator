using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CreateParkourBikeRiderPrefab
{
    private const string GeneratedFbx = "Assets/JEC/Generated/RiderRetarget/ParkourBikeRider_RagRig.fbx";
    private const string OriginalRigPrefab = "Assets/ExternalAsset/RayznGames/BicycleSystem/URP/Prefabs/Rag_Rig_URP.prefab";
    private const string OutputPrefab = "Assets/JEC/Generated/RiderRetarget/ParkourBikeRider_RagRig.prefab";

    [MenuItem("Tools/Delivery Simulator/Build Parkour Bike Rider Prefab")]
    public static void Build()
    {
        GameObject generatedModel = AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedFbx);
        GameObject originalRig = AssetDatabase.LoadAssetAtPath<GameObject>(OriginalRigPrefab);
        if (generatedModel == null || originalRig == null)
        {
            Debug.LogError($"Missing generated model or original rig. Generated={generatedModel}, Rig={originalRig}");
            return;
        }

        GameObject generatedInstance = Object.Instantiate(generatedModel);
        GameObject rigInstance = (GameObject)PrefabUtility.InstantiatePrefab(originalRig);
        generatedInstance.hideFlags = HideFlags.HideAndDontSave;

        try
        {
            SkinnedMeshRenderer generatedRenderer = generatedInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(r => r.sharedMesh != null ? r.sharedMesh.vertexCount : 0).FirstOrDefault();
            SkinnedMeshRenderer rigRenderer = rigInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(r => r.sharedMesh != null ? r.sharedMesh.vertexCount : 0).FirstOrDefault();
            if (generatedRenderer == null || rigRenderer == null)
            {
                Debug.LogError("Could not find SkinnedMeshRenderer on generated FBX or Rag rig prefab.");
                return;
            }

            Dictionary<string, Transform> rigBones = rigInstance.GetComponentsInChildren<Transform>(true).GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());
            Transform[] mappedBones = new Transform[generatedRenderer.bones.Length];
            for (int i = 0; i < generatedRenderer.bones.Length; i++)
            {
                Transform sourceBone = generatedRenderer.bones[i];
                if (sourceBone == null || !rigBones.TryGetValue(sourceBone.name, out Transform targetBone))
                {
                    Debug.LogError($"Bone mapping failed: {(sourceBone == null ? "<null>" : sourceBone.name)}");
                    return;
                }
                mappedBones[i] = targetBone;
            }

            Transform mappedRoot = null;
            if (generatedRenderer.rootBone != null) rigBones.TryGetValue(generatedRenderer.rootBone.name, out mappedRoot);
            if (mappedRoot == null) mappedRoot = mappedBones.FirstOrDefault();

            rigRenderer.sharedMesh = generatedRenderer.sharedMesh;
            rigRenderer.sharedMaterials = generatedRenderer.sharedMaterials;
            rigRenderer.bones = mappedBones;
            rigRenderer.rootBone = mappedRoot;
            rigRenderer.updateWhenOffscreen = true;
            rigRenderer.enabled = true;
            rigRenderer.gameObject.name = "ParkourBikeRider_Mesh";

            rigInstance.name = "ParkourBikeRider_RagRig";
            PrefabUtility.SaveAsPrefabAsset(rigInstance, OutputPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Object output = AssetDatabase.LoadAssetAtPath<Object>(OutputPrefab);
            Selection.activeObject = output;
            EditorGUIUtility.PingObject(output);
            Debug.Log($"Created rider prefab: {OutputPrefab}");
        }
        finally
        {
            Object.DestroyImmediate(generatedInstance);
            Object.DestroyImmediate(rigInstance);
        }
    }
}
