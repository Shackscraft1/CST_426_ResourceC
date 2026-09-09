using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

/*
 * PlayerNameLabel sets the overhead nameplate text from this player's replicated
 * NetworkObject owner id and keeps the nameplate facing the main camera.
 */

public class PlayerNameLabel : NetworkBehaviour
{
    [SerializeField] TextMeshProUGUI _label;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // TODO Slice 1.1: label this player with the NetworkObject owner id.
        _label.text = $"Player {OwnerClientId}";
    }

    void LateUpdate()
    {
        // TODO Slice 1.2: rotate the world-space label to face the main
        // camera, keeping the look direction flat by setting its y to zero.
        // </> end of Slice 1
        Vector3 target = transform.position + (transform.position - Camera.main.transform.position);
        _label.transform.LookAt(Camera.main.transform);
    }
}
