using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[RequireComponent (typeof (LightProbeGroup))]
public class LightProbeGroupBuilder : MonoBehaviour {

    static Color VolumeColor = new Color (1f, 0.9f, 0.25f);
    static Color BoundsColor = new Color (0.5f, 0.5f, 0.9f);

    [Header ("Volume")]
    [SerializeField] Vector3 offset;
    [SerializeField] Vector3 size;
    [SerializeField] Vector3Int density = new Vector3Int (2, 2, 2);

    [Header ("Renderers")]
    [SerializeField] float boundsExtent = 0f;

#if UNITY_EDITOR
    List<Renderer> renderersInVolume = new List<Renderer> ();

    public int currentProbeNumber {
        get {
            return GetComponent<LightProbeGroup> ().probePositions.Length;
        }
    }

    public int currentRendererNumber {
        get {
            return renderersInVolume.Count;
        }
    }

    List<Renderer> FindRenderersInVolume () {
        var renderersInScene = FindObjectsOfType<GameObject> ()
            .Where (go => (GameObjectUtility.GetStaticEditorFlags (go) & StaticEditorFlags.LightmapStatic) != 0)
            .Select (go => go.GetComponent<Renderer> ())
            .Where (r => r != null)
            .ToList ();

        var renderersInVolume = new List<Renderer> ();
        renderersInScene.ForEach (
            r => {
                renderersInVolume.Add (r);
            }
        );
        return renderersInVolume;
    }

    List<Vector3> GetProbePositions (bool worldSpace = false) {
        var probePositions = new List<Vector3> ();
        if (density.x > 1 && density.y > 1 && density.z > 1) {
            var cellSize = new Vector3 (size.x / (density.x - 1), size.y / (density.y - 1), size.z / (density.z - 1));
            for (int x = 0; x < density.x; x++) {
                for (int y = 0; y < density.y; y++) {
                    for (int z = 0; z < density.z; z++) {
                        var pos = offset + size * -0.5f + new Vector3 (cellSize.x * x, cellSize.y * y, cellSize.z * z);
                        if (worldSpace) {
                            pos = transform.TransformPoint (pos);
                        }
                        probePositions.Add (pos);
                    }
                }
            }
        }
        return probePositions;
    }

    [ContextMenu ("Update Renderers in Volume")]
    public void UpdateRenderersInVolume () {
        renderersInVolume = FindRenderersInVolume ();
    }

    [ContextMenu ("Build Light Probes")]
    public void BuildLightProbeGroup () {
        var group = GetComponent<LightProbeGroup> ();
        group.probePositions = GetProbePositions ().ToArray ();
        EditorSceneManager.MarkSceneDirty (this.gameObject.scene);
    }

    void OnDrawGizmosSelected () {
        Gizmos.color = VolumeColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube (offset, size);

        if (renderersInVolume != null) {
            Gizmos.color = BoundsColor;
            Gizmos.matrix = Matrix4x4.identity;
            foreach (var renderer in renderersInVolume) {
                var bounds = renderer.bounds;
                Gizmos.DrawWireCube (bounds.center, (bounds.extents + new Vector3 (boundsExtent, boundsExtent, boundsExtent)) * 2f);
            }
        }

        var probePositions = GetProbePositions (true);
        for (int i = 0; i < probePositions.Count; i++) {
            Gizmos.DrawIcon (probePositions[i], "NONE", false);
        }
    }
#endif
}