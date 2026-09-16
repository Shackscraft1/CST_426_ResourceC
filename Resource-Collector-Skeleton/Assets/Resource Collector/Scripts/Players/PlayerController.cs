using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * PlayerController is the owner's local input loop: movement, target
 * selection, and the client-to-server interaction request. The server
 * still owns every world mutation.
 */

public class PlayerController : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] CharacterController _characterController;
    [SerializeField] Animator _animator;
    [SerializeField] PlayerHeldItem _heldItem;

    [Header("Detection")]
    [SerializeField] float _detectionRadius = 3f;
    [SerializeField] float _detectionAngle = 60f;
    [SerializeField] LayerMask _pickupLayer;

    [Header("Movement")]
    [SerializeField] float _movementSpeed = 4f;
    [SerializeField] float _rotationSpeed = 200f;

    Interactable _closestTarget;
    Vector2 _smoothedInput;

    void Update()
    {
        if (!IsOwner) return;

        // PROVIDED Slice 2.2: read this owner's movement in Update.
        Vector2 movementInput = ReadMovementInput();
        // PROVIDED Slice 2.5: smooth _smoothedInput toward the raw input so the walk cycle does not pop.
        _smoothedInput = Vector2.MoveTowards(_smoothedInput, movementInput, Time.deltaTime * 10f);

        // PROVIDED Slice 2.3: rotate and move forward/back.
        transform.Rotate(Vector3.up, _smoothedInput.x * _rotationSpeed * Time.deltaTime);

        Vector3 motion = _characterController.transform.forward * _smoothedInput.y * _movementSpeed * Time.deltaTime;
        _characterController.Move(motion);

        // PROVIDED Slice 2.4: set the "Speed" animator float so walk speed matches input.
        _animator.SetFloat("Speed", _characterController.velocity.magnitude);

        UpdateInteractionTarget();

        // PROVIDED Slice 6.1:
        // 1. Detect E or left-click this frame.
        // 2. Call HandleInteractionPressed.
        // Check: Play Mode, Host, highlight the axe, press E.
        // The Interact clip plays. The axe still stays on the ground.
        if (Keyboard.current.eKey.wasPressedThisFrame || Mouse.current.leftButton.wasPressedThisFrame)
            HandleInteractionPressed();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // PROVIDED Slice 2.6: make the main camera follow only its local player. </> end of Slice 2
        if (IsOwner)
            Camera.main.GetComponent<FollowCamera>().Target = transform;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            // PROVIDED Slice 5.2: turn off the current target's Highlightable,
            // then clear _closestTarget.
            ClearSelection();
        }

        base.OnNetworkDespawn();
    }

    void HandleInteractionPressed()
    {
        if (!IsOwner) return;

        // PROVIDED Slice 6.2:
        // 1. If there is no target, return.
        // 2. Fire the Animator's "Interact" trigger.
        // 3. Send the target's NetworkObjectId to the server.
        if (_closestTarget == null) return;
        _animator.SetTrigger("Interact");
        RequestInteractRpc(_closestTarget.NetworkObjectId);
    }

    static Vector2 ReadMovementInput()
    {
        // PROVIDED Slice 2.1: return WASD input as a two-dimensional vector.
        Vector2 movementInput = Vector2.zero;
        movementInput.x += Keyboard.current.aKey.isPressed ? -1f : 0f;
        movementInput.x += Keyboard.current.dKey.isPressed ? 1f : 0f;
        movementInput.y += Keyboard.current.wKey.isPressed ? 1f : 0f;
        movementInput.y += Keyboard.current.sKey.isPressed ? -1f : 0f;

        return movementInput;
    }

    void UpdateInteractionTarget()
    {
        // PROVIDED Slice 5.1: find the closest valid Interactable in front of the player.
        // When the target changes, clear the old highlight and select the new one.
        Interactable candidate = FindClosestValidInteractable();
        if (candidate == _closestTarget) return;

        ClearSelection();

        if (candidate != null)
        {
            candidate.GetComponent<Highlightable>().SetHighlighted(true);
            _closestTarget = candidate;
        }
    }

    Interactable FindClosestValidInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _detectionRadius, _pickupLayer);
        float closestDistance = float.MaxValue;
        Interactable candidate = null;

        foreach (Collider hit in hits)
        {
            if (!hit.TryGetComponent(out Interactable target)) continue;
            if (!target.CanInteract(_heldItem.ObjectType)) continue;

            Vector3 directionToTarget = (hit.transform.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, directionToTarget) > _detectionAngle) continue;

            float distance = Vector3.Distance(transform.position, hit.transform.position);
            if (distance > closestDistance) continue;

            closestDistance = distance;
            candidate = target;
        }

        return candidate;
    }

    void ClearSelection()
    {
        if (_closestTarget != null)
            _closestTarget.GetComponent<Highlightable>().SetHighlighted(false);

        _closestTarget = null;
    }

    [Rpc(SendTo.Server)]
    void RequestInteractRpc(ulong networkObjectId)
    {
        // PROVIDED Slice 6.3:
        // 1. Look up networkObjectId in SpawnedObjects.
        // 2. If that object is gone, return. It may have despawned after you selected it.
        // 3. If it has an Interactable, call ServerInteract(_heldItem).
        Dictionary<ulong, NetworkObject> spawnedObjectMap = NetworkManager.SpawnManager.SpawnedObjects;
        if (!spawnedObjectMap.TryGetValue(networkObjectId, out NetworkObject spawnedObject))
        {
            Debug.LogError($"Couldn't find id: {networkObjectId}");
            return;
        }

        if (!spawnedObject.TryGetComponent(out Interactable interactable))
        {
            Debug.LogError("Object doesn't have interactable");
            return;
        }

        interactable.ServerInteract(_heldItem);

        // Check: E still only plays Interact. Console stays clean. The pickup
        // (e.g. axe) does not move yet.

        // Next: Slice 6.4 in World/Interactable.cs — ServerInteract.
    }
}
