using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/*
 * PlayerHeldItem tracks which resource or tool a player is carrying and shows
 * the matching held model on every client.
 *
 * This is the core NetworkVariable pattern for the lesson: server changes the
 * value, every client subscribes in OnNetworkSpawn, and each client applies the
 * current value immediately for late-join correctness.
 */

public class PlayerHeldItem : NetworkBehaviour
{
    [Serializable]
    public struct ItemCatalogEntry
    {
        public ObjectType type;
        public GameObject model;
        public NetworkObject prefab;
    }

    [Header("Item Catalog")]
    [SerializeField] List<ItemCatalogEntry> _itemCatalog = new();

    public ObjectType ObjectType => _heldObjectType.Value;

    readonly NetworkVariable<ObjectType> _heldObjectType = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // TODO Slice 6.9: subscribe to held-item changes and apply the current value.
    }

    public override void OnNetworkPreDespawn()
    {
        base.OnNetworkPreDespawn();

        // TODO Slice 7.3: on the server, drop the held item unless the host is shutting down. </> end of Slice 7
    }

    public override void OnNetworkDespawn()
    {
        // TODO Slice 6.10: unsubscribe from held-item changes. </> end of Slice 6
        base.OnNetworkDespawn();
    }

    public void SetHeldItem(ObjectType objectType)
    {
        if (!IsServer) return;

        // TODO Slice 6.6: store the authoritative held item.
    }

    public void Clear()
    {
        if (!IsServer) return;

        // TODO Slice 6.7: clear the authoritative held item by storing ObjectType.None.
    }

    // Spawns the held item back into the world at the player's feet, then
    // empties the hand. Used for swap and for drop-on-disconnect.
    public void SpawnHeldItemAsNewPickup(Vector3 position)
    {
        if (!IsServer) return;

        // TODO Slice 7.1: if the hand is empty, return. Otherwise find the matching
        // catalog prefab, spawn it with NetworkObject.InstantiateAndSpawn, then
        // empty the hand.
        // Next: Slice 7.2 in ItemPickup.Interact.
    }

    void HandleObjectTypeChanged(ObjectType previousValue, ObjectType newValue)
    {
        // TODO Slice 6.8: show only the held model matching newValue.
    }
}
