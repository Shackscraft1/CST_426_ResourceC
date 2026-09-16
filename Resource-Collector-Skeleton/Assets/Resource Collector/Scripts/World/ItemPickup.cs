using Unity.Netcode;
using UnityEngine;

/*
 * ItemPickup is an item lying in the world. Picking it up copies its type onto
 * the player and despawns this object. In-scene pickups keep their GameObject
 * (Despawn(false)); catalog drops are destroyed. Late joiners do not see taken
 * items: dynamic ones are gone, and a taken scene pickup is hidden below.
 */

public class ItemPickup : Interactable
{
    [SerializeField] ObjectType _objectType;

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // Despawn(false) leaves the GameObject active and visible. Netcode also
        // runs this callback on a late joiner's copy of a taken scene pickup.
        // A session ending despawns everything too; hiding then would leave
        // uncollected pickups inactive for the next join.
        if (NetworkObject.InScenePlaced && !NetworkManager.ShutdownInProgress)
            gameObject.SetActive(false);
    }

    public override bool CanInteract(ObjectType heldType)
    {
        // TODO Slice 5.3: a spawned pickup is valid to collect. </> end of Slice 5
        return true;
    }

    protected override void Interact(PlayerHeldItem heldItem)
    {
        heldItem.DropHeldItem(transform.position);

        // PROVIDED Slice 7.2: DropHeldItem first so a swap returns the old type.

        // Next: Slice 7.3 in PlayerHeldItem.OnNetworkPreDespawn.

        // PROVIDED Slice 6.5: Set the held item to be this item pickup
        heldItem.SetHeldItem(_objectType);

        // Next: Slice 6.6 in PlayerHeldItem.SetHeldItem.

        // PROVIDED Slice 6.10:
        // 1. Despawn this pickup.
        // 2. Destroy catalog drops; keep scene pickups.
        // Check: Host + Client. The ground axe disappears in both Game views.
        // A late joiner sees the held axe and no ground axe. </> end of Slice 6
        // Next: Slice 7.1 in Players/PlayerHeldItem.cs — DropHeldItem and Clear.

        //NetworkObject.Despawn(false);
        NetworkObject.Despawn(!NetworkObject.InScenePlaced);
    }
}