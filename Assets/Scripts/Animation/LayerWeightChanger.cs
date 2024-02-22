using System.Collections;
using UnityEngine;

public class LayerWeightChanger : MonoBehaviour
{
    public Animator animator; // Assign your Animator in the inspector
    public int layerIndex; // Set the index of the layer you want to change in the inspector
    public float targetWeight; // Set the target weight in the inspector
    public float duration; // Set the duration of the weight change in the inspector

    ComplexPlayerMovement sprintValue; // Yhteys p‰‰scriptiin, mist‰ tarvitaan juoksu-boolean
    Coroutine runningCoroutine; // Referenssi Coroutineen


    private void OnEnable()
    {
        sprintValue = GetComponent<ComplexPlayerMovement>();
        sprintValue.OnStartSprint += StartSprint;
        sprintValue.OnStopSprint += StopSprint;
    }

    private void OnDisable()
    {
        sprintValue.OnStartSprint -= StartSprint;
        sprintValue.OnStopSprint -= StopSprint;
    }

    private void StartSprint()
    {
        layerIndex = 1; // The sprint layer index
        runningCoroutine = StartCoroutine(ChangeLayerWeight(1)); // Set the target weight to 1 for the sprint layer
    }

    private void StopSprint()
    {
        // If a ChangeLayerWeight coroutine is already running, stop it
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
        }

        layerIndex = 1; // The sprint layer index
        StartCoroutine(ChangeLayerWeight(0)); // Set the target weight to 0 for the sprint layer
    }


    IEnumerator ChangeLayerWeight(float targetWeight)
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
