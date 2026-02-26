using UnityEngine;

public class MirrorPosition : MonoBehaviour
{
    void Awake()
    {
        // Debug.Log(transform.position);
        GetComponentInChildren<HingeJoint2D>().connectedAnchor = transform.position;

        // Debug.Log(transform.position);
    }
}
