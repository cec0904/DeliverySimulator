using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GTAMissionMarkerVFX))]
public class GTAMissionMarkerVFXEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        GUILayout.Space(8f);
        if (GUILayout.Button("Rebuild Preview"))
        {
            GTAMissionMarkerVFX marker = (GTAMissionMarkerVFX)target;
            marker.Rebuild();
            EditorUtility.SetDirty(marker);
            SceneView.RepaintAll();
        }
    }

    [MenuItem("GameObject/Effects/GTA Mission Marker VFX", false, 10)]
    private static void CreateMarker(MenuCommand command)
    {
        GameObject markerObject = new GameObject("GTA Mission Marker");
        GameObjectUtility.SetParentAndAlign(markerObject, command.context as GameObject);
        Undo.RegisterCreatedObjectUndo(markerObject, "Create GTA Mission Marker");
        markerObject.AddComponent<GTAMissionMarkerVFX>();
        Selection.activeGameObject = markerObject;
    }
}
