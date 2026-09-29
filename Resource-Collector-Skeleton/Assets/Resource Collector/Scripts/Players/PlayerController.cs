using System.Collections;
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

    [Header("Axe")]
    [SerializeField] float _throwImpulse = 25f;
    [SerializeField] float _returnDuration = 1f;
    [SerializeField] float _bowAmount = 2.5f;
    [SerializeField] float _aimDistance = 20f;

    enum AxeState { Held, Throwing, Away, Stuck, Returning }

    Interactable _closestTarget;
    ResourceNode _aimTarget;
    Vector2 _smoothedInput;
    ThrownAxe _axe;
    LineRenderer _lineRenderer;
    bool _axeThrowRequested;
    Coroutine _returnRoutine;

    readonly NetworkVariable<AxeState> _axeState = new();
    readonly NetworkVariable<Vector3> _axePosition = new();
    readonly NetworkVariable<Quaternion> _axeRotation = new();

    void Awake()
    {
        GameObject axeModel = _heldItem.GetHeldModel(ObjectType.Axe);
        _axe = axeModel.GetComponentInParent<ThrownAxe>();
        if (_axe == null)
        {
            Transform axeParent = axeModel.transform.parent;
            Vector3 axeLocalPosition = axeModel.transform.localPosition;
            Quaternion axeLocalRotation = axeModel.transform.localRotation;
            Vector3 axeLocalScale = axeModel.transform.localScale;

            GameObject axeRoot = new GameObject("Thrown Axe");
            axeRoot.layer = axeModel.layer;
            axeRoot.transform.SetParent(axeParent);
            axeRoot.transform.SetLocalPositionAndRotation(axeLocalPosition, axeLocalRotation);
            axeRoot.transform.localScale = Vector3.one;

            axeModel.transform.SetParent(axeRoot.transform);
            axeModel.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            axeModel.transform.localScale = axeLocalScale;

            _axe = axeRoot.AddComponent<ThrownAxe>();
        }

        _axe.Initialize(this, _characterController);
        SetupLineRenderer();
    }

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

        bool handledAxeInput = UpdateAxeInput();

        // PROVIDED Slice 6.1:
        // 1. Detect E or left-click this frame.
        // 2. Call HandleInteractionPressed.
        // Check: Play Mode, Host, highlight the axe, press E.
        // The Interact clip plays. The axe still stays on the ground.
        if (!handledAxeInput && InputSystem.actions.FindAction("Player/Interact").WasPressedThisFrame())
            HandleInteractionPressed();

        UpdateAimVisual();
    }

    void FixedUpdate()
    {
        if (!IsServer || _axe == null) return;
        if (_axeState.Value == AxeState.Held || _axeState.Value == AxeState.Throwing) return;

        if (_axeState.Value == AxeState.Away && _axe.TryGetSweptHit(out Collider sweptCollider))
            HandleAxeHit(sweptCollider);

        PublishAxeTransform();
    }

    void LateUpdate()
    {
        if (!IsSpawned || _axe == null) return;

        if (_axeState.Value == AxeState.Held || _axeState.Value == AxeState.Throwing)
        {
            _axe.FollowHand();
            return;
        }

        if (IsServer) return;

        float interpolation = 1f - Mathf.Exp(-20f * Time.deltaTime);
        _axe.transform.position = Vector3.Lerp(_axe.transform.position, _axePosition.Value, interpolation);
        _axe.transform.rotation = Quaternion.Slerp(_axe.transform.rotation, _axeRotation.Value, interpolation);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // PROVIDED Slice 2.6: make the main camera follow only its local player. </> end of Slice 2
        if (IsOwner)
            Camera.main.GetComponent<FollowCamera>().Target = transform;

        _axeState.OnValueChanged += HandleAxeStateChanged;
        HandleAxeStateChanged(_axeState.Value, _axeState.Value);
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            // PROVIDED Slice 5.2: turn off the current target's Highlightable,
            // then clear _closestTarget.
            ClearSelection();
            SetAimTarget(null);
        }

        _axeState.OnValueChanged -= HandleAxeStateChanged;
        _lineRenderer.positionCount = 0;
        _axe.AttachToHand();

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
    }

    static Vector2 ReadMovementInput()
    {
        // PROVIDED Slice 2.1: return WASD input as a two-dimensional vector.
        return InputSystem.actions.FindAction("Player/Move").ReadValue<Vector2>();
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

    public void RequestInteraction()
    {
        if (!IsOwner) return;

        if (_axeThrowRequested)
        {
            _axeThrowRequested = false;
            ReleaseAxeRpc();
            return;
        }

        if (_closestTarget == null) return;

        RequestInteractRpc(_closestTarget.NetworkObjectId);
    }

    void ClearSelection()
    {
        if (_closestTarget != null && _closestTarget != _aimTarget)
            _closestTarget.GetComponent<Highlightable>().SetHighlighted(false);

        _closestTarget = null;
    }

    bool UpdateAxeInput()
    {
        if (_heldItem.ObjectType != ObjectType.Axe) return false;

        if (_axeState.Value == AxeState.Held && !_axeThrowRequested &&
            InputSystem.actions.FindAction("Player/Attack").WasPressedThisFrame())
        {
            _axeThrowRequested = true;
            _animator.SetTrigger("Interact");
            PrepareAxeThrowRpc();
            return true;
        }

        bool recallPressed = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        recallPressed |= Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;

        if ((_axeState.Value == AxeState.Away || _axeState.Value == AxeState.Stuck) && recallPressed)
        {
            RequestAxeRecallRpc();
            return true;
        }

        return false;
    }

    void UpdateAimVisual()
    {
        if (_heldItem.ObjectType != ObjectType.Axe)
        {
            _lineRenderer.positionCount = 0;
            SetAimTarget(null);
            return;
        }

        switch (_axeState.Value)
        {
            case AxeState.Held:
                DrawAimLine();
                break;
            case AxeState.Away:
            case AxeState.Stuck:
                SetAimTarget(null);
                DrawReturnPath();
                break;
            default:
                _lineRenderer.positionCount = 0;
                SetAimTarget(null);
                break;
        }
    }

    void DrawAimLine()
    {
        Vector3 start = _axe.CatchPosition;
        if (!TryGetAimHit(start, out RaycastHit hit))
        {
            _lineRenderer.positionCount = 0;
            SetAimTarget(null);
            return;
        }

        ResourceNode resourceNode = hit.collider.GetComponentInParent<ResourceNode>();
        if (resourceNode == null || !resourceNode.CanInteract(ObjectType.Axe) ||
            !resourceNode.TryGetComponent(out Highlightable _))
        {
            _lineRenderer.positionCount = 0;
            SetAimTarget(null);
            return;
        }

        _lineRenderer.positionCount = 2;
        _lineRenderer.SetPosition(0, start);
        _lineRenderer.SetPosition(1, hit.point);
        SetAimTarget(resourceNode);
    }

    bool TryGetAimHit(Vector3 start, out RaycastHit aimHit)
    {
        RaycastHit[] hits = Physics.SphereCastAll(start, 0.08f, transform.forward,
            _aimDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform.IsChildOf(_axe.transform))
                continue;

            aimHit = hit;
            return true;
        }

        aimHit = default;
        return false;
    }

    void SetAimTarget(ResourceNode target)
    {
        if (_aimTarget == target) return;

        if (_aimTarget != null && _aimTarget != _closestTarget)
            _aimTarget.GetComponent<Highlightable>().SetHighlighted(false);

        _aimTarget = target;

        if (_aimTarget != null)
            _aimTarget.GetComponent<Highlightable>().SetHighlighted(true);
    }

    void DrawReturnPath()
    {
        const int sampleCount = 20;
        (Vector3 p0, Vector3 p1, Vector3 p2) = GetReturnControlPoints(_axe.transform.position);

        _lineRenderer.positionCount = sampleCount;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (sampleCount - 1f);
            _lineRenderer.SetPosition(i, QuadraticBezierMath.SamplePointBernstein(p0, p1, p2, t));
        }
    }

    (Vector3 p0, Vector3 p1, Vector3 p2) GetReturnControlPoints(Vector3 start)
    {
        Vector3 end = _axe.CatchPosition;
        Vector3 middle = (start + end) * 0.5f + transform.right * _bowAmount + Vector3.up * 1.5f;
        return (start, middle, end);
    }

    IEnumerator ReturnAxe()
    {
        _axeState.Value = AxeState.Returning;
        _axe.BeginReturn();

        (Vector3 p0, Vector3 p1, Vector3 p2) = GetReturnControlPoints(_axe.transform.position);
        QuadraticBezierMath.ComputePowerBasisCoefficients(p0, p1, p2,
            out Vector3 c0, out Vector3 c1, out Vector3 c2);

        float elapsedTime = 0f;
        while (elapsedTime < _returnDuration)
        {
            float t = Mathf.Clamp01(elapsedTime / _returnDuration);
            Vector3 position = QuadraticBezierMath.SamplePointPowerBasis(c0, c1, c2, t);
            Vector3 tangent = QuadraticBezierMath.SampleTangentPowerBasis(c1, c2, t);
            _axe.SetReturnPose(position, tangent, elapsedTime * _axe.spinSpeed);
            PublishAxeTransform();

            yield return null;
            elapsedTime += Time.deltaTime;
        }

        _axe.AttachToHand();
        _axeState.Value = AxeState.Held;
        _returnRoutine = null;
    }

    IEnumerator ReturnAxeAfterDelay()
    {
        yield return new WaitForSeconds(0.35f);
        yield return ReturnAxe();
    }

    public void HandleAxeCollision(Collision collision)
    {
        if (!IsServer || _axeState.Value != AxeState.Away) return;

        HandleAxeHit(collision.collider);
    }

    void HandleAxeHit(Collider hitCollider)
    {
        if (!IsServer || _axeState.Value != AxeState.Away) return;

        ResourceNode resourceNode = hitCollider.GetComponentInParent<ResourceNode>();
        bool targetDepleted = resourceNode != null && resourceNode.CanInteract(ObjectType.Axe) &&
            resourceNode.ServerHitByThrownAxe();

        _axe.Stick();
        PublishAxeTransform();
        _axeState.Value = AxeState.Stuck;

        if (targetDepleted)
            _returnRoutine = StartCoroutine(ReturnAxeAfterDelay());
    }

    void HandleAxeStateChanged(AxeState previousValue, AxeState newValue)
    {
        switch (newValue)
        {
            case AxeState.Held:
                _axe.AttachToHand();
                if (previousValue == AxeState.Returning)
                    _axe.PlayCatchFeedback();
                break;
            case AxeState.Away:
                if (!IsServer)
                    _axe.BeginRemoteFlight(_axePosition.Value, _axeRotation.Value);
                _axe.PlayThrowFeedback();
                break;
            case AxeState.Stuck:
                if (!IsServer)
                    _axe.BeginRemoteStick(_axePosition.Value, _axeRotation.Value);
                _axe.PlayImpactFeedback();
                break;
            case AxeState.Returning:
                if (!IsServer)
                    _axe.BeginReturn();
                _axe.PlayRecallFeedback();
                break;
        }
    }

    void PublishAxeTransform()
    {
        _axePosition.Value = _axe.transform.position;
        _axeRotation.Value = _axe.transform.rotation;
    }

    void SetupLineRenderer()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        if (_lineRenderer == null)
            _lineRenderer = gameObject.AddComponent<LineRenderer>();

        _lineRenderer.useWorldSpace = true;
        _lineRenderer.startWidth = 0.04f;
        _lineRenderer.endWidth = 0.01f;
        _lineRenderer.positionCount = 0;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.startColor = new Color(0.4f, 0.9f, 1f, 0.9f);
        _lineRenderer.endColor = new Color(1f, 1f, 1f, 0.2f);
    }

    [Rpc(SendTo.Server)]
    void PrepareAxeThrowRpc()
    {
        if (_heldItem.ObjectType != ObjectType.Axe) return;
        if (_axeState.Value != AxeState.Held) return;

        _axeState.Value = AxeState.Throwing;
    }

    [Rpc(SendTo.Server)]
    void ReleaseAxeRpc()
    {
        if (_heldItem.ObjectType != ObjectType.Axe) return;
        if (_axeState.Value != AxeState.Throwing) return;

        Vector3 direction = transform.forward;
        direction.y = 0f;
        direction.Normalize();

        _axe.Launch(direction, _throwImpulse);
        PublishAxeTransform();
        _axeState.Value = AxeState.Away;
    }

    [Rpc(SendTo.Server)]
    void RequestAxeRecallRpc()
    {
        if (_heldItem.ObjectType != ObjectType.Axe) return;
        if (_axeState.Value != AxeState.Away && _axeState.Value != AxeState.Stuck) return;
        if (_returnRoutine != null) return;

        _returnRoutine = StartCoroutine(ReturnAxe());
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
