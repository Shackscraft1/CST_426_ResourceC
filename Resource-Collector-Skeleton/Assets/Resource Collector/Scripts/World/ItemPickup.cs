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
        if (NetworkObject.InScenePlaced == true)
            gameObject.SetActive(false);
    }

    public override bool CanInteract(ObjectType heldType)
    {
        // TODO Slice 5.3: a spawned pickup is valid to collect. </> end of Slice 5
        return false;
    }

    protected override void Interact(PlayerHeldItem heldItem)
    {
        // TODO Slice 6.5: put this item's type in the player's hand, then Despawn.
        //   Destroy catalog drops; keep scene pickups: Despawn(destroy: NetworkObject.InScenePlaced != true).
        // Next: Slice 6.6 in PlayerHeldItem.SetHeldItem.
        // TODO Slice 7.2: SpawnHeldItemAsNewPickup first so a swap returns the old type.
        // Next: Slice 7.3 in PlayerHeldItem.OnNetworkPreDespawn.
    }
}
