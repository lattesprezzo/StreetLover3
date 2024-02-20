using System.Collections;
using UnityEngine;

public class LayerWeightChanger : MonoBehaviour
{
    public Animator animator; // Assign your Animator in the inspector
    public int layerIndex; // Set the index of the layer you want to change in the inspector
    public float targetWeight; // Set the target weight in the inspector
    public float duration; // Set the duration of the weight change in the inspector

    void Update()
    {

            StartCoroutine(ChangeLayerWeight());
         }

    IEnumerator ChangeLayerWeight()
    {
        float startTime = Time.time;
        float startWeight = animator.GetLayerWeight(layerIndex);

        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float newWeight = Mathf.Lerp(startWeight, targetWeight, t);
            animator.SetLayerWeight(layerIndex, newWeight);
            yield return null;
        }

        animator.SetLayerWeight(layerIndex, targetWeight);
    }
}
