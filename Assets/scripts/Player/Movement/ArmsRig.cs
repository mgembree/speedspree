using UnityEngine;

/// <summary>
/// Attach to ArmsRig. Drop your FPS arms prefab/model as a child of ArmsRig
/// and this script will automatically assign the Arms layer to every renderer,
/// so ArmsCamera renders it on top of the world camera.
/// </summary>
public class ArmsRig : MonoBehaviour
{
    [Tooltip("Assign the ArmsCamera here, or it will be found automatically.")]
    [SerializeField] Camera armsCamera;

    [Tooltip("Match this to the main camera FOV if they differ.")]
    [SerializeField] bool syncFovWithMainCamera = true;

    const string ArmsLayerName = "Arms";

    void Awake()
    {
        EnforceArmsLayer();

        if (armsCamera == null)
        {
            var go = GameObject.Find("ArmsCamera");
            if (go != null) armsCamera = go.GetComponent<Camera>();
        }
    }

    void Update()
    {
        if (syncFovWithMainCamera && armsCamera != null && Camera.main != null)
            armsCamera.fieldOfView = Camera.main.fieldOfView;
    }

    /// <summary>
    /// Recursively sets every child renderer to the Arms layer.
    /// Call this after swapping arms models at runtime.
    /// </summary>
    public void EnforceArmsLayer()
    {
        int layer = LayerMask.NameToLayer(ArmsLayerName);
        if (layer < 0)
        {
            Debug.LogWarning("[ArmsRig] 'Arms' layer not found. Add it in Project Settings > Tags & Layers.");
            return;
        }

        SetLayerRecursive(transform, layer);
    }

    void SetLayerRecursive(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursive(t.GetChild(i), layer);
    }

    // Called automatically when a child is added in the Editor
    void OnTransformChildrenChanged() => EnforceArmsLayer();
}
