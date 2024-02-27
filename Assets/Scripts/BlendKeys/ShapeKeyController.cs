using System.Collections;
using UnityEngine;

public class ShapeKeyController : MonoBehaviour
{
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public string shapeKeyName;
    public float speed = 1f;

    private int shapeKeyIndex;
    private float t = 0;

    void Start()
    {
        shapeKeyIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(shapeKeyName);
    }

    void Update()
    {
        t += Time.deltaTime * speed;
        float value = (Mathf.Sin(t) + 1) / 2; // This moves the value between 0 and 1
        skinnedMeshRenderer.SetBlendShapeWeight(shapeKeyIndex, value * 100); // Unity uses 0-100 range for blend shapes
    }
}

