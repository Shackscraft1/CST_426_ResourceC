using UnityEngine;

/*
 * FollowCamera holds the camera at a fixed offset from a target. The offset is
 * set in the inspector; the owning player assigns itself as the target when it
 * spawns. Purely local and presentational, so this is a plain MonoBehaviour.
 */

public class FollowCamera : MonoBehaviour
{
    [SerializeField] Vector3 _offset;

    public Transform Target { get; set; }

    void LateUpdate()
    {
        if (Target == null) return;

        transform.position = Target.position + _offset;
    }
}
