using UnityEngine;

public class BoneController : MonoBehaviour
{
    public string leftArmBoneName = "mixamorig:LeftArm";
    public string rightArmBoneName = "mixamorig:RightArm";
    public string leftArm;
    public Vector3 leftArmPosition; 

    [SerializeField]
    private Transform leftArmBone;
    private Transform rightArmBone;

    void Start()
    {
        // Find the arm bones
        leftArmBone = FindDeepChild(this.transform, leftArmBoneName);
        rightArmBone = FindDeepChild(this.transform, rightArmBoneName);


        leftArm = leftArmBoneName.Substring(3, 10);
        leftArm = leftArmBoneName.Replace("mixamorig:", "");
    }

    void Update()
    {
        // Rotate the arm bones a little bit each frame
        if (leftArmBone != null && rightArmBone != null)
        {
            leftArmBone.Rotate(leftArmPosition);
            rightArmBone.Rotate(new Vector3(10, 0, 0));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("BoneControlActivator"))
        {
            // Reset the arm bones to the T-Pose
            if (leftArmBone != null && rightArmBone != null)
            {
                leftArmBone.localRotation = Quaternion.identity;
                rightArmBone.localRotation = Quaternion.identity;
            }
        }
    }

    // Recursive method to find a child with a given name in the hierarchy
    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
}
