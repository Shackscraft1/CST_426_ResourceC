using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/*
 * ResourceNode is a harvestable object like a tree or stone. Replicated health
 * counts down as players hit it with the right tool; at zero the server spawns
 * resource pickups and every client hides the depleted node.
 */

public class ResourceNode : Interactable
{
    [SerializeField] List<ObjectType> _toolTypeRequired = new();
    [SerializeField] NetworkObject _producedPrefab;
    [SerializeField] int _amountToSpawn = 3;
    [SerializeField] int _startingHealth = 1;
    [SerializeField] AudioClip _audioClip;

    readonly NetworkVariable<int> _health = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // TODO Slice 8.1: on the server, set health to _startingHealth. Then
        // subscribe to health changes and apply the current health.
    }

    public override void OnNetworkDespawn()
    {
        // TODO Slice 8.4: unsubscribe from replicated health changes.
        base.OnNetworkDespawn();
    }

    public override bool CanInteract(ObjectType heldType)
    {
        // TODO Slice 8.5: require a living node and an accepted tool.
        return false;
    }

    protected override void Interact(PlayerHeldItem heldItem)
    {
        // TODO Slice 8.7: reduce health and play feedback. At zero, spawn
        // _amountToSpawn copies of _producedPrefab with InstantiateAndSpawn.
        // Place each on the ground with a small random XZ offset and random yaw.
        // </> end of Slice 8
    }

    [Rpc(SendTo.ClientsAndHost)]
    void HitFeedbackRpc()
    {
        // TODO Slice 8.6: play the authored hit sound on each observer.
    }

    void HandleHealthChanged(int previousValue, int newValue)
    {
        // TODO Slice 8.3: apply replicated health locally.
    }

    void ApplyHealth()
    {
        // TODO Slice 8.2: hide depleted nodes and disable their collider.
    }
}
