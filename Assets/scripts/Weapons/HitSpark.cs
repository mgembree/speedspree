using System.Collections;
using UnityEngine;

public class HitSpark : MonoBehaviour
{
    const float Lifetime   = 0.25f;
    const float StartScale = 0.12f;

    public static void Spawn(Vector3 position, Vector3 normal = default)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "HitSpark";
        go.transform.position   = position + normal * 0.02f;
        go.transform.localScale = Vector3.one * StartScale;

        Destroy(go.GetComponent<Collider>());

        var rend = go.GetComponent<Renderer>();
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows    = false;

        // Set color for both Built-in and URP shaders
        var mat = rend.material;
        mat.SetColor("_Color",     Color.red);
        mat.SetColor("_BaseColor", Color.red);

        go.AddComponent<HitSpark>();
    }

    void Awake() => StartCoroutine(ShrinkRoutine());

    IEnumerator ShrinkRoutine()
    {
        Vector3 start   = Vector3.one * StartScale;
        float   elapsed = 0f;
        while (elapsed < Lifetime)
        {
            elapsed             += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, Vector3.zero, elapsed / Lifetime);
            yield return null;
        }
        Destroy(gameObject);
    }
}
