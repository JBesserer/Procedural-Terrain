using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapView))]
public class MapViewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapView mapView = (MapView)target;


        if(DrawDefaultInspector ())
        {
            if(mapView.autoUpdate){mapView.DrawMapInEditor();}
        }
        if(GUILayout.Button("Generate"))
        {
            mapView.DrawMapInEditor();
        }
    }
}
