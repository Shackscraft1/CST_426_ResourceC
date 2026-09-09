using Unity.Netcode;

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

        // TODO Slice 6.4: validate CanInteract before calling the subclass behavior.
        // Next: Slice 6.5 in ItemPickup.Interact.
    }

    protected abstract void Interact(PlayerHeldItem heldItem);
}
