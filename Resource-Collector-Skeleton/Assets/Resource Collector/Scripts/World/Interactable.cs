using Unity.Netcode;
using UnityEngine;

/*
 * Interactable is the shared server-owned entry point for every world object a
 * player can use. Only the server mutates state. ItemPickup, ResourceNode, and
 * Receptacle each replicate their own state.
 */

public abstract class Interactable : NetworkBehaviour
{
    public abstract bool CanInteract(ObjectType heldType);

    public void ServerInteract(PlayerHeldItem heldItem)
    {
        if (!IsServer) return;

        Debug.Log($"Requesting Interact for {heldItem.ObjectType}");

        // PROVIDED Slice 6.4:
        // 1. Test if we can interact.
        // 2. If so, call Interact.
        // We implement ItemPickup's Interact next; ResourceNode and Receptacle come later.
        // Next: Slice 6.5 in ItemPickup.Interact.
        if (CanInteract(heldItem.ObjectType))
            Interact(heldItem);
    }

    protected abstract void Interact(PlayerHeldItem heldItem);
}