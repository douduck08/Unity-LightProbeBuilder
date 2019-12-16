using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (LightProbeGroupBuilder))]
public class LightProbeGroupBuilderEditor : Editor {
    public override void OnInspectorGUI () {
        base.OnInspectorGUI ();
        EditorGUILayout.Space ();

        var builder = (LightProbeGroupBuilder) target;
        GUI.enabled = false;
        EditorGUILayout.IntField ("Current Probe Number", builder.currentProbeNumber);
        EditorGUILayout.IntField ("Current Renderer Number", builder.currentRendererNumber);
        EditorGUILayout.IntField ("Current Collider Number", builder.currentColliderNumber);

        GUI.enabled = true;
        using (new EditorGUILayout.HorizontalScope ()) {
            if (GUILayout.Button ("Clear Light Probes")) {
                builder.ClearLightProbeGroup ();
            }
            if (GUILayout.Button ("Update Bounds")) {
                builder.UpdateBoundsInVolume ();
            }
            if (GUILayout.Button ("Build Light Probes")) {
                builder.BuildLightProbeGroup ();
            }
        }
    }
}