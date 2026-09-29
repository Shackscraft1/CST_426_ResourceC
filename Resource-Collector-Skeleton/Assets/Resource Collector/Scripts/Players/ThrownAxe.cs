using UnityEngine;

/*
 * ThrownAxe keeps the axe's held pose so it can be attached to the hand
 * after a throw. Launch physics and collision response belong here;
 * PlayerController decides when to throw and recall it.
 */

public class ThrownAxe : MonoBehaviour
{
    public Rigidbody rigidbody;
    public Collider axeCollider;
    public float spinSpeed = 1000f;

    PlayerController _playerController;
    CharacterController _thrower;
    Transform _hand;
    Vector3 _heldLocalPosition;
    Quaternion _heldLocalRotation;
    Vector3 _previousPhysicsPosition;
    TrailRenderer _trailRenderer;
    ParticleSystem _particleSystem;
    AudioSource _audioSource;

    static AudioClip _throwClip;
    static AudioClip _recallClip;
    static AudioClip _catchClip;

    public Vector3 CatchPosition => _hand.TransformPoint(_heldLocalPosition);

    public void Initialize(PlayerController playerController, CharacterController thrower)
    {
        _playerController = playerController;
        _thrower = thrower;
        _hand = transform.parent;
        _heldLocalPosition = transform.localPosition;
        _heldLocalRotation = transform.localRotation;

        SetupPhysics();
        SetupTrail();
        SetupParticles();
        SetupAudio();
        AttachToHand();
    }

    public void Launch(Vector3 direction, float impulse)
    {
        Physics.IgnoreCollision(axeCollider, _thrower);

        transform.SetParent(null);
        transform.right = direction;

        rigidbody.isKinematic = false;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        axeCollider.enabled = true;
        _trailRenderer.emitting = true;
        _previousPhysicsPosition = rigidbody.position;

        rigidbody.WakeUp();
        rigidbody.AddForce(direction * impulse, ForceMode.VelocityChange);
        rigidbody.AddTorque(transform.forward * -(spinSpeed * Mathf.Deg2Rad), ForceMode.VelocityChange);
    }

    public void BeginRemoteFlight(Vector3 position, Quaternion rotation)
    {
        transform.SetParent(null);
        transform.SetPositionAndRotation(position, rotation);
        rigidbody.isKinematic = true;
        axeCollider.enabled = false;
        _trailRenderer.emitting = true;
    }

    public void BeginReturn()
    {
        transform.SetParent(null);
        rigidbody.isKinematic = true;
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        axeCollider.enabled = false;
        _trailRenderer.emitting = true;
    }

    public void SetReturnPose(Vector3 position, Vector3 tangent, float spin)
    {
        transform.position = position;

        if (tangent.sqrMagnitude > 0.0001f)
        {
            Quaternion pathRotation = Quaternion.FromToRotation(Vector3.right, tangent.normalized);
            transform.rotation = pathRotation * Quaternion.AngleAxis(spin, Vector3.forward);
        }
    }

    public void Stick()
    {
        rigidbody.isKinematic = true;
        rigidbody.interpolation = RigidbodyInterpolation.None;
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        axeCollider.enabled = true;
        _trailRenderer.emitting = false;
    }

    public bool TryGetSweptHit(out Collider hitCollider)
    {
        Vector3 startPosition = _previousPhysicsPosition;
        Vector3 currentPosition = rigidbody.position;
        Vector3 movement = currentPosition - startPosition;
        _previousPhysicsPosition = currentPosition;

        if (movement.sqrMagnitude <= Mathf.Epsilon)
        {
            hitCollider = null;
            return false;
        }

        RaycastHit[] hits = Physics.SphereCastAll(startPosition, 0.08f, movement.normalized,
            movement.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == axeCollider || hit.collider.transform.IsChildOf(_thrower.transform))
                continue;

            hitCollider = hit.collider;
            return true;
        }

        hitCollider = null;
        return false;
    }

    public void BeginRemoteStick(Vector3 position, Quaternion rotation)
    {
        transform.SetParent(null);
        transform.SetPositionAndRotation(position, rotation);
        rigidbody.isKinematic = true;
        axeCollider.enabled = false;
        _trailRenderer.emitting = false;
    }

    public void AttachToHand()
    {
        transform.SetParent(_hand);
        transform.SetLocalPositionAndRotation(_heldLocalPosition, _heldLocalRotation);
        rigidbody.isKinematic = true;
        rigidbody.interpolation = RigidbodyInterpolation.None;
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        axeCollider.enabled = false;
        _trailRenderer.emitting = false;
        _trailRenderer.Clear();
    }

    public void FollowHand()
    {
        if (transform.parent != _hand)
            transform.SetParent(_hand);

        transform.SetLocalPositionAndRotation(_heldLocalPosition, _heldLocalRotation);
    }

    public void PlayThrowFeedback()
    {
        _audioSource.PlayOneShot(_throwClip);
    }

    public void PlayRecallFeedback()
    {
        _audioSource.PlayOneShot(_recallClip);
    }

    public void PlayImpactFeedback()
    {
        _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _particleSystem.Play();
    }

    public void PlayCatchFeedback()
    {
        _audioSource.PlayOneShot(_catchClip);
        PlayImpactFeedback();
    }

    void OnCollisionEnter(Collision collision)
    {
        _playerController.HandleAxeCollision(collision);
    }

    void SetupPhysics()
    {
        rigidbody = GetComponent<Rigidbody>();
        if (rigidbody == null)
            rigidbody = gameObject.AddComponent<Rigidbody>();

        axeCollider = GetComponent<Collider>();
        if (axeCollider == null)
        {
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            FitColliderToRenderers(boxCollider);
            axeCollider = boxCollider;
        }

        rigidbody.mass = 1f;
        rigidbody.useGravity = true;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rigidbody.isKinematic = true;
        axeCollider.enabled = false;
    }

    void FitColliderToRenderers(BoxCollider boxCollider)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        if (renderers.Length == 0) return;

        Bounds bounds = new Bounds(transform.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
        foreach (Renderer renderer in renderers)
        {
            Vector3 min = renderer.bounds.min;
            Vector3 max = renderer.bounds.max;
            bounds.Encapsulate(transform.InverseTransformPoint(new Vector3(min.x, min.y, min.z)));
            bounds.Encapsulate(transform.InverseTransformPoint(new Vector3(min.x, min.y, max.z)));
            bounds.Encapsulate(transform.InverseTransformPoint(new Vector3(min.x, max.y, min.z)));
            bounds.Encapsulate(transform.InverseTransformPoint(new Vector3(min.x, max.y, max.z)));
            bounds.Encapsulate(transform.InverseTransformPoint(new Vector3(max.x, min.y, min.z)));
            bounds.Encapsulate(transform.InverseTransformPoint(new Vector3(max.x, min.y, max.z)));
            bounds.Encapsulate(transform.InverseTransformPoint(new Vector3(max.x, max.y, min.z)));
            bounds.Encapsulate(transform.InverseTransformPoint(new Vector3(max.x, max.y, max.z)));
        }

        boxCollider.center = bounds.center;
        boxCollider.size = bounds.size;
    }

    void SetupTrail()
    {
        _trailRenderer = GetComponent<TrailRenderer>();
        if (_trailRenderer == null)
            _trailRenderer = gameObject.AddComponent<TrailRenderer>();

        _trailRenderer.time = 0.3f;
        _trailRenderer.minVertexDistance = 0.05f;
        _trailRenderer.startWidth = 0.1f;
        _trailRenderer.endWidth = 0f;
        _trailRenderer.emitting = false;
        _trailRenderer.material = CreateEffectMaterial(new Color(0.4f, 0.9f, 1f));
    }

    void SetupParticles()
    {
        _particleSystem = GetComponent<ParticleSystem>();
        if (_particleSystem == null)
            _particleSystem = gameObject.AddComponent<ParticleSystem>();

        _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = _particleSystem.main;
        main.loop = false;
        main.duration = 0.2f;
        main.startLifetime = 0.35f;
        main.startSpeed = 1.5f;
        main.startSize = 0.12f;
        main.startColor = new Color(0.5f, 0.9f, 1f);
        main.playOnAwake = false;

        ParticleSystem.EmissionModule emission = _particleSystem.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

        ParticleSystem.ShapeModule shape = _particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        ParticleSystemRenderer particleRenderer = _particleSystem.GetComponent<ParticleSystemRenderer>();
        particleRenderer.material = CreateEffectMaterial(new Color(0.5f, 0.9f, 1f));
        _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void SetupAudio()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 1f;
        _audioSource.minDistance = 2f;
        _audioSource.maxDistance = 25f;

        _throwClip ??= CreateTone("Axe Throw", 180f, 0.18f);
        _recallClip ??= CreateTone("Axe Recall", 440f, 0.25f);
        _catchClip ??= CreateTone("Axe Catch", 260f, 0.12f);
    }

    static Material CreateEffectMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    static AudioClip CreateTone(string clipName, float frequency, float duration)
    {
        const int sampleRate = 22050;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float fade = 1f - i / (float)sampleCount;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * fade * 0.25f;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
