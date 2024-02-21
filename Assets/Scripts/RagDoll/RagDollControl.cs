
using UnityEngine;


public class RagDollControl : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("RagDollActivator"))
        {
            animator.enabled = false;
        }
    }


}
