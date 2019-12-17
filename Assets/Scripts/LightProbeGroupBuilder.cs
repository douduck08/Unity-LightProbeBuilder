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

#if UNITY_EDITOR
    [Header ("Volume")]
    [SerializeField] bool generateWithGrids = true;
    [SerializeField] Vector3 offset;
    [SerializeField] Vector3 size;
    [SerializeField] Vector3Int density = new Vector3Int (2, 2, 2);

    [Header ("Bounds")]
    [SerializeField] bool generateWithBounds = true;
    [SerializeField] bool useRendererBounds = true;
    [SerializeField] bool useColliderBounds = false;
    [SerializeField] float boundsExtent = 0f;

    [Header ("Terrain")]
    [SerializeField] bool generateWithTerrain;
    [SerializeField] float terrainOffset = 0.1f;
    [SerializeField] Vector2Int terrainGrid = new Vector2Int (10, 10);

    [Header ("Other Settings")]
    [SerializeField] bool removeOutsideVolume = true;

    List<Renderer> renderersInVolume = new List<Renderer> ();
    List<Collider> collidersInVolume = new List<Collider> ();
    List<Terrain> terrainsInVolume = new List<Terrain> ();

    public int currentProbeNumber {
        get {
            return GetComponent<LightProbeGroup> ().probePositions.Length;
        }
    }

    List<Renderer> FindRenderersInVolume () {
        var renderersInScene = FindObjectsOfType<GameObject> ()
            .Where (go => (GameObjectUtility.GetStaticEditorFlags (go) & StaticEditorFlags.LightmapStatic) != 0)
            .Select (go => go.GetComponent<Renderer> ())
            .Where (r => r != null)
            .ToList ();

        var volumeBounds = new Bounds (transform.position + offset, size);
        var result = new List<Renderer> ();
        renderersInScene.ForEach (
            r => {
                var bounds = r.bounds;
                bounds.size = bounds.size + new Vector3 (boundsExtent, boundsExtent, boundsExtent) * 2f;
                if (bounds.Intersects (volumeBounds)) {
                    result.Add (r);
                }
            }
        );
        return result;
    }

    List<Collider> FindCollidersInVolume () {
        var collidersInScene = FindObjectsOfType<GameObject> ()
            .Where (go => (GameObjectUtility.GetStaticEditorFlags (go) & StaticEditorFlags.LightmapStatic) != 0)
            .Select (go => go.GetComponent<Collider> ())
            .Where (c => c != null)
            .ToList ();

        var volumeBounds = new Bounds (transform.position + offset, size);
        var result = new List<Collider> ();
        collidersInScene.ForEach (
            c => {
                var bounds = c.bounds;
                bounds.size = bounds.size + new Vector3 (boundsExtent, boundsExtent, boundsExtent) * 2f;
                if (bounds.Intersects (volumeBounds)) {
                    result.Add (c);
                }
            }
        );
        return result;
    }

    List<Terrain> FindTerrainsInVolume () {
        var terrainsInScene = FindObjectsOfType<GameObject> ()
            .Where (go => (GameObjectUtility.GetStaticEditorFlags (go) & StaticEditorFlags.LightmapStatic) != 0)
            .Select (go => go.GetComponent<Terrain> ())
            .Where (c => c != null)
            .ToList ();

        var volumeBounds = new Bounds (transform.position + offset, size);
        var result = new List<Terrain> ();
        terrainsInScene.ForEach (
            t => {
                var bounds = new Bounds (t.transform.TransformPoint (t.terrainData.bounds.center), t.terrainData.bounds.size);
                bounds.size = bounds.size + new Vector3 (boundsExtent, boundsExtent, boundsExtent) * 2f;
                if (bounds.Intersects (volumeBounds)) {
                    result.Add (t);
                }
            }
        );
        return result;
    }

    void UpdateComponentLists () {
        if (useRendererBounds) {
            renderersInVolume = FindRenderersInVolume ();
        } else {
            renderersInVolume.Clear ();
        }
        if (useColliderBounds) {
            collidersInVolume = FindCollidersInVolume ();
        } else {
            collidersInVolume.Clear ();
        }
        if (generateWithTerrain) {
            terrainsInVolume = FindTerrainsInVolume ();
        } else {
            terrainsInVolume.Clear ();
        }
    }

    List<Vector3> GetGridProbePositions (bool worldSpace = false) {
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

    List<Vector3> GetBoundsProbePositions () {
        UpdateComponentLists ();
        var probePositions = new List<Vector3> ();
        if (renderersInVolume != null) {
            foreach (var renderer in renderersInVolume) {
                var bounds = renderer.bounds;
                var center = bounds.center;
                var extents = bounds.extents + new Vector3 (boundsExtent, boundsExtent, boundsExtent);
                probePositions.Add (center + new Vector3 (extents.x, extents.y, extents.z));
                probePositions.Add (center + new Vector3 (-extents.x, -extents.y, extents.z));
                probePositions.Add (center + new Vector3 (extents.x, -extents.y, extents.z));
                probePositions.Add (center + new Vector3 (-extents.x, extents.y, extents.z));
                probePositions.Add (center + new Vector3 (extents.x, extents.y, -extents.z));
                probePositions.Add (center + new Vector3 (-extents.x, -extents.y, -extents.z));
                probePositions.Add (center + new Vector3 (extents.x, -extents.y, -extents.z));
                probePositions.Add (center + new Vector3 (-extents.x, extents.y, -extents.z));
            }
        }
        if (collidersInVolume != null) {
            foreach (var collider in collidersInVolume) {
                var bounds = collider.bounds;
                var center = bounds.center;
                var extents = bounds.extents + new Vector3 (boundsExtent, boundsExtent, boundsExtent);
                probePositions.Add (center + new Vector3 (extents.x, extents.y, extents.z));
                probePositions.Add (center + new Vector3 (-extents.x, -extents.y, extents.z));
                probePositions.Add (center + new Vector3 (extents.x, -extents.y, extents.z));
                probePositions.Add (center + new Vector3 (-extents.x, extents.y, extents.z));
                probePositions.Add (center + new Vector3 (extents.x, extents.y, -extents.z));
                probePositions.Add (center + new Vector3 (-extents.x, -extents.y, -extents.z));
                probePositions.Add (center + new Vector3 (extents.x, -extents.y, -extents.z));
                probePositions.Add (center + new Vector3 (-extents.x, extents.y, -extents.z));
            }
        }
        return probePositions;
    }

    List<Vector3> GetTerrainProbePositions () {
        var probePositions = new List<Vector3> ();
        if (terrainsInVolume != null) {
            if (terrainGrid.x > 1 && terrainGrid.y > 1) {
                foreach (var terrain in terrainsInVolume) {
                    var terrainSize = terrain.terrainData.size;
                    var gridSize = new Vector2 (terrainSize.x / (terrainGrid.x + 1), terrainSize.z / (terrainGrid.y + 1));
                    var raycastHit = new RaycastHit ();
                    for (int x = 0; x < terrainGrid.x; x++) {
                        for (int y = 0; y < terrainGrid.y; y++) {
                            var pos = new Vector3 (gridSize.x * (x + 1), terrainSize.y + 10f, gridSize.y * (y + 1));
                            pos = terrain.transform.TransformPoint (pos);
                            var collider = terrain.GetComponent<TerrainCollider> ();
                            if (collider.Raycast (new Ray (pos, Vector3.down), out raycastHit, terrainSize.y + 20f)) {
                                probePositions.Add (raycastHit.point + new Vector3 (0, terrainOffset, 0));
                            }

                        }
                    }
                }
            }
        }
        return probePositions;
    }

    List<Vector3> GetAllProbePositions () {
        var result = new List<Vector3> ();
        if (generateWithGrids) {
            result.AddRange (GetGridProbePositions ());
        }
        if (generateWithBounds) {
            result.AddRange (GetBoundsProbePositions ());
        }
        if (generateWithTerrain) {
            result.AddRange (GetTerrainProbePositions ());
        }
        if (removeOutsideVolume) {
            var volumeBounds = new Bounds (transform.position + offset, size);
            result = result.Where (p => volumeBounds.Contains (p)).ToList ();
        }
        return result;
    }

    [ContextMenu ("Update Renderers in Volume")]
    public void ClearLightProbeGroup () {
        var group = GetComponent<LightProbeGroup> ();
        group.probePositions = new Vector3[0];
        EditorSceneManager.MarkSceneDirty (this.gameObject.scene);
    }

    [ContextMenu ("Update Renderers in Volume")]
    public void UpdateBoundsInVolume () {
        UpdateComponentLists ();
    }

    [ContextMenu ("Build Light Probes")]
    public void BuildLightProbeGroup () {
        var group = GetComponent<LightProbeGroup> ();
        group.probePositions = GetAllProbePositions ().ToArray ();
        EditorSceneManager.MarkSceneDirty (this.gameObject.scene);
    }

    void OnDrawGizmosSelected () {
        Gizmos.color = VolumeColor;
        Gizmos.DrawWireCube (transform.position + offset, size);

        Gizmos.color = BoundsColor;
        if (renderersInVolume != null) {
            foreach (var renderer in renderersInVolume) {
                var bounds = renderer.bounds;
                Gizmos.DrawWireCube (bounds.center, bounds.size + new Vector3 (boundsExtent, boundsExtent, boundsExtent) * 2f);
            }
        }
        if (collidersInVolume != null) {
            foreach (var collider in collidersInVolume) {
                var bounds = collider.bounds;
                Gizmos.DrawWireCube (bounds.center, bounds.size + new Vector3 (boundsExtent, boundsExtent, boundsExtent) * 2f);
            }
        }

        var probePositions = GetGridProbePositions (true);
        for (int i = 0; i < probePositions.Count; i++) {
            Gizmos.DrawIcon (probePositions[i], "NONE", false);
        }
    }
#endif
}