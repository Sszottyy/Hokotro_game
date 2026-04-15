using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapTester))]
public class MapTesterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MapTester tester = (MapTester)target;
        if (GUILayout.Button("Város Generálása"))
        {
            tester.RunTest();
        }
    }
}