using UnityEngine;

public class DentController : MonoBehaviour
{

    public SkinnedMeshRenderer skinnedMeshRenderer;
    public string shapeKeyName;
    private int shapeKeyIndex;
    public float shapeSpeed;
    private float t = 0;
    public float value;

    //----- Lerp variables
    readonly float a = 0;
    readonly float b = 100;
    public float c = 0;
    public float lerpValue;


    void Start()
    {
        shapeKeyIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(shapeKeyName);

    }


    void Update()
    {
        t += Time.deltaTime * shapeSpeed; // Shape kasvaa loputtomiin
        value = (Mathf.Sin(t) + 1) / 2; // Veivaa eessuntaassun 0 ja 1 välillä

       //skinnedMeshRenderer.SetBlendShapeWeight(shapeKeyIndex, value * 100);

        // Using Lerp:
        lerpValue = Mathf.Lerp(a, b, c);
        c=c < 1? c += Time.deltaTime * shapeSpeed : 1;
        skinnedMeshRenderer.SetBlendShapeWeight(shapeKeyIndex, lerpValue);


    }
}
