using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.TopDownEngine;
using DungeonCrawl.Gameplay;

/// <summary>
/// Makes a cat companion follow the player with real grounded, four-legged locomotion (CharacterController +
/// gravity + a procedural trot gait, matching the player's acceleration-based movement feel) and cast Magic
/// Missile spells at nearby enemies.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class CatCompanion : MonoBehaviour
{
    [Header("Follow Target")]
    [Tooltip("Transform to follow. If left empty, the scene's Player character is used automatically.")]
    [SerializeField] private Transform target;

    [Header("Grounded Movement")]
    [SerializeField] private float followDistance = 1.6f;
    [Tooltip("Speed while gently keeping up with the player.")]
    [SerializeField] private float walkSpeed = 3.2f;
    [Tooltip("Speed used when catching up after falling far behind.")]
    [SerializeField] private float runSpeed = 6f;
    [Tooltip("Distance beyond followDistance over which speed ramps from walk to run.")]
    [SerializeField] private float catchUpBlendDistance = 3f;
    [SerializeField] private float acceleration = 18f;
    [SerializeField] private float deceleration = 24f;
    [SerializeField] private float turnSpeedDegrees = 480f;
    [SerializeField] private float gravity = -20f;

    [Header("Quadruped Gait")]
    [Tooltip("Assign the four leg pivot transforms to get a real four-legged trot instead of floating.")]
    [SerializeField] private Transform frontLeftLeg;
    [SerializeField] private Transform frontRightLeg;
    [SerializeField] private Transform backLeftLeg;
    [SerializeField] private Transform backRightLeg;
    [SerializeField] private float strideAmplitude = 28f;
    [SerializeField] private float strideFrequency = 2.2f;

    [Header("Animation (optional, for a rigged model)")]
    [Tooltip("If a rigged cat model with a walk cycle is assigned, its Animator is driven instead of the procedural legs above.")]
    [SerializeField] private Animator animator;

    [Header("Magic Missile Spell")]
    [Tooltip("When enabled, the cat periodically scans for enemies and fires magic missiles.")]
    [SerializeField] private bool autoCast = true;
    [Tooltip("Maximum range to acquire enemies.")]
    [SerializeField] private float castRange = 14f;
    [Tooltip("Cooldown between spell casts in seconds.")]
    [SerializeField] private float castCooldown = 2.4f;
    [Tooltip("Number of magic missiles in each salvo.")]
    [SerializeField] private int missilesPerSalvo = 3;
    [Tooltip("Delay between each missile in a salvo.")]
    [SerializeField] private float salvoInterval = 0.12f;
    [Tooltip("Damage dealt by each magic missile.")]
    [SerializeField] private float missileDamage = 20f;
    [Tooltip("Layers representing hostile targets.")]
    [SerializeField] private LayerMask targetLayers;
    [Tooltip("Layers blocking line of sight.")]
    [SerializeField] private LayerMask obstacleLayers;
    [Tooltip("Custom prefab for magic missile. If null, a glowing arcane missile is created dynamically.")]
    [SerializeField] private GameObject magicMissilePrefab;
    [Tooltip("Origin transform for missiles. Defaults to cat head/top.")]
    [SerializeField] private Transform castPoint;

    [Header("Audio & Feedback")]
    [SerializeField] private AudioClip castSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private Color spellColor = new Color(0.2f, 0.85f, 1f, 1f); // Arcane cyan

    private CharacterController _controller;
    private Vector3 _planarVelocity;
    private float _verticalVelocity;
    private float _gaitPhase;
    private Quaternion _flRestRotation, _frRestRotation, _blRestRotation, _brRestRotation;
    private float _nextCastTime;
    private bool _isCasting;
    private Transform _currentTarget;
    private readonly Collider[] _scanColliders = new Collider[32];

    public bool CanCast => Time.time >= _nextCastTime && !_isCasting;
    public bool AutoCast { get => autoCast; set => autoCast = value; }
    public Transform CurrentTarget => _currentTarget;
    public bool IsCasting => _isCasting;
    public Vector3 PlanarVelocity => _planarVelocity;
    public float CurrentSpeed => _planarVelocity.magnitude;
    public float WalkSpeed => walkSpeed;
    public float RunSpeed => runSpeed;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        if (_controller == null)
        {
            _controller = gameObject.AddComponent<CharacterController>();
            _controller.height = 0.6f;
            _controller.radius = 0.28f;
            _controller.center = new Vector3(0f, 0.3f, 0f);
            _controller.stepOffset = 0.3f;
            _controller.minMoveDistance = 0.001f;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (frontLeftLeg != null) _flRestRotation = frontLeftLeg.localRotation;
        if (frontRightLeg != null) _frRestRotation = frontRightLeg.localRotation;
        if (backLeftLeg != null) _blRestRotation = backLeftLeg.localRotation;
        if (backRightLeg != null) _brRestRotation = backRightLeg.localRotation;
    }

    private void Start()
    {
        if (target == null && LevelManager.HasInstance
            && LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0)
        {
            target = LevelManager.Instance.Players[0].transform;
        }

        // Default layers if not set in Inspector
        if (targetLayers.value == 0)
        {
            targetLayers = LayerMask.GetMask("Enemies");
            if (targetLayers.value == 0)
            {
                // Fallback layer 13
                targetLayers = 1 << 13;
            }
        }

        if (obstacleLayers.value == 0)
        {
            obstacleLayers = LayerMask.GetMask("Obstacles", "ObstaclesDoors");
        }

        if (castPoint == null)
        {
            castPoint = transform;
        }
    }

    private void Update()
    {
        AcquirePlayerTargetIfNeeded();
        HandleMovement();

        if (autoCast && CanCast)
        {
            Transform bestEnemy = FindBestTarget();
            if (bestEnemy != null)
            {
                CastAtTarget(bestEnemy);
            }
        }
    }

    private void AcquirePlayerTargetIfNeeded()
    {
        if (target == null && LevelManager.HasInstance
            && LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0)
        {
            target = LevelManager.Instance.Players[0].transform;
        }
    }

    private void HandleMovement()
    {
        Vector3 desiredPlanarVelocity = Vector3.zero;
        Vector3 direction = Vector3.zero;

        if (target != null)
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (distance > followDistance)
            {
                direction = toTarget.normalized;
                // ramp from a walk to a run the further behind the cat falls, instead of teleporting to catch up
                float speedFactor = catchUpBlendDistance > 0f
                    ? Mathf.Clamp01((distance - followDistance) / catchUpBlendDistance)
                    : 1f;
                float targetSpeed = Mathf.Lerp(walkSpeed, runSpeed, speedFactor);
                desiredPlanarVelocity = direction * targetSpeed;
            }
        }

        // accelerate/decelerate towards the desired speed, same as a real walking gait rather than an instant snap
        float rate = desiredPlanarVelocity.sqrMagnitude > _planarVelocity.sqrMagnitude ? acceleration : deceleration;
        _planarVelocity = Vector3.MoveTowards(_planarVelocity, desiredPlanarVelocity, rate * Time.deltaTime);

        // turn towards movement direction at a real angular speed unless actively facing a spell target
        if (!_isCasting && direction.sqrMagnitude > 0.0001f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, turnSpeedDegrees * Time.deltaTime);
        }

        // when actively casting, face the enemy instead
        if (_isCasting && _currentTarget != null)
        {
            Vector3 toEnemy = _currentTarget.position - transform.position;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude > 0.001f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(toEnemy.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, (turnSpeedDegrees * 1.5f) * Time.deltaTime);
            }
        }

        // real gravity + ground snapping via CharacterController instead of a fixed floating height
        if (_controller != null)
        {
            _verticalVelocity = _controller.isGrounded ? -1f : _verticalVelocity + gravity * Time.deltaTime;
            _controller.Move((_planarVelocity + Vector3.up * _verticalVelocity) * Time.deltaTime);
        }
        else
        {
            transform.position += _planarVelocity * Time.deltaTime;
        }

        UpdateGait(_planarVelocity.magnitude);
    }

    /// <summary>
    /// Drives either the four procedural leg pivots (trot gait, diagonal pairs swinging in opposite phase) or,
    /// if a rigged model's Animator is assigned, its Speed/Walking parameters - so the cat actually walks on
    /// four legs instead of sliding around like a floating orb.
    /// </summary>
    private void UpdateGait(float speed)
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", speed);
            animator.SetBool("Walking", speed > 0.05f);
            return;
        }

        bool hasLegs = frontLeftLeg != null && frontRightLeg != null && backLeftLeg != null && backRightLeg != null;
        if (!hasLegs)
        {
            return;
        }

        if (speed > 0.05f)
        {
            _gaitPhase += speed * strideFrequency * Time.deltaTime;
        }
        else
        {
            // ease the stride back to a neutral standing pose instead of freezing mid-step
            _gaitPhase = Mathf.MoveTowards(_gaitPhase, Mathf.Round(_gaitPhase), 4f * Time.deltaTime);
        }

        float swing = Mathf.Sin(_gaitPhase * Mathf.PI * 2f) * strideAmplitude * Mathf.Clamp01(speed / walkSpeed);

        // diagonal trot: front-left/back-right swing together, front-right/back-left swing in opposite phase
        frontLeftLeg.localRotation = _flRestRotation * Quaternion.Euler(swing, 0f, 0f);
        backRightLeg.localRotation = _brRestRotation * Quaternion.Euler(swing, 0f, 0f);
        frontRightLeg.localRotation = _frRestRotation * Quaternion.Euler(-swing, 0f, 0f);
        backLeftLeg.localRotation = _blRestRotation * Quaternion.Euler(-swing, 0f, 0f);
    }

    /// <summary>
    /// Searches for the closest alive enemy within castRange.
    /// </summary>
    public Transform FindBestTarget()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        int hitCount = Physics.OverlapSphereNonAlloc(origin, castRange, _scanColliders, targetLayers, QueryTriggerInteraction.Ignore);

        Transform bestTarget = null;
        float closestDistanceSqr = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = _scanColliders[i];
            if (col == null || col.transform == transform) continue;

            // Check if object or parent is an alive damageable
            var health = col.GetComponentInParent<Health>();
            if (health != null && health.CurrentHealth <= 0) continue;

            // Check for AI character
            var character = col.GetComponentInParent<Character>();
            if (character != null && character.CharacterType == Character.CharacterTypes.Player)
            {
                continue; // Do not target players
            }

            // Line of sight check to avoid casting straight into walls
            Vector3 targetPos = col.bounds.center;
            Vector3 rayDir = targetPos - origin;
            float rayDist = rayDir.magnitude;

            if (Physics.Raycast(origin, rayDir.normalized, rayDist, obstacleLayers, QueryTriggerInteraction.Ignore))
            {
                // Obstructed by obstacle
                continue;
            }

            float distSqr = (targetPos - origin).sqrMagnitude;
            if (distSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distSqr;
                bestTarget = health != null ? health.transform : col.transform;
            }
        }

        return bestTarget;
    }

    /// <summary>
    /// Casts a magic missile salvo at the current or nearest target.
    /// </summary>
    public void CastMagicMissile()
    {
        if (!CanCast) return;
        Transform targetEnemy = FindBestTarget();
        if (targetEnemy != null)
        {
            CastAtTarget(targetEnemy);
        }
    }

    /// <summary>
    /// Casts a magic missile salvo specifically targeted at an enemy transform.
    /// </summary>
    public void CastAtTarget(Transform enemy)
    {
        if (enemy == null || _isCasting) return;
        StartCoroutine(CastSalvoRoutine(enemy));
    }

    private IEnumerator CastSalvoRoutine(Transform enemy)
    {
        _isCasting = true;
        _currentTarget = enemy;
        _nextCastTime = Time.time + castCooldown;

        // Visual cast flash on the cat
        PlayCastAuraVFX();

        // Audio cue
        if (castSound != null)
        {
            AudioSource.PlayClipAtPoint(castSound, transform.position, 0.7f);
        }

        // Fanned out angles for multi-missile salvo: e.g. [-28, 0, +28]
        float[] spreadAngles = GetSalvoSpreadAngles(missilesPerSalvo);

        for (int i = 0; i < missilesPerSalvo; i++)
        {
            if (enemy == null) break;

            Vector3 spawnPos = castPoint != null ? castPoint.position + Vector3.up * 0.3f : transform.position + Vector3.up * 0.5f;
            Vector3 toEnemy = (enemy.position + Vector3.up * 0.5f) - spawnPos;
            Vector3 forwardFlat = new Vector3(toEnemy.x, 0f, toEnemy.z).normalized;
            if (forwardFlat.sqrMagnitude < 0.001f) forwardFlat = transform.forward;

            // Arc outward laterally and upward
            float spreadAngle = spreadAngles[i];
            Quaternion spreadRot = Quaternion.AngleAxis(spreadAngle, Vector3.up);
            Vector3 launchDir = (spreadRot * forwardFlat + Vector3.up * 0.35f).normalized;

            SpawnMissile(spawnPos, launchDir, enemy);

            if (salvoInterval > 0f && i < missilesPerSalvo - 1)
            {
                yield return new WaitForSeconds(salvoInterval);
            }
        }

        yield return new WaitForSeconds(0.2f);
        _isCasting = false;
        _currentTarget = null;
    }

    private void SpawnMissile(Vector3 spawnPos, Vector3 launchDir, Transform targetEnemy)
    {
        GameObject missileGO;
        if (magicMissilePrefab != null)
        {
            missileGO = Instantiate(magicMissilePrefab, spawnPos, Quaternion.LookRotation(launchDir));
        }
        else
        {
            missileGO = CreateDefaultMagicMissileObject(spawnPos, launchDir);
        }

        var missile = missileGO.GetComponent<MagicMissile>();
        if (missile == null)
        {
            missile = missileGO.AddComponent<MagicMissile>();
        }

        missile.Initialize(
            targetEnemy,
            launchDir,
            missileDamage,
            gameObject,
            targetLayers,
            obstacleLayers,
            hitSound
        );
    }

    private GameObject CreateDefaultMagicMissileObject(Vector3 pos, Vector3 forward)
    {
        var missileGO = new GameObject("MagicMissile");
        missileGO.transform.position = pos;
        missileGO.transform.forward = forward;

        // Glowing core orb
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Core";
        sphere.transform.SetParent(missileGO.transform, false);
        sphere.transform.localScale = Vector3.one * 0.3f;
        Destroy(sphere.GetComponent<SphereCollider>());

        var renderer = sphere.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = spellColor;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", spellColor * 3.5f);
        renderer.sharedMaterial = mat;

        // Magical Trail
        var trail = missileGO.AddComponent<TrailRenderer>();
        trail.time = 0.35f;
        trail.startWidth = 0.25f;
        trail.endWidth = 0.02f;
        trail.material = mat;
        trail.startColor = spellColor;
        trail.endColor = new Color(spellColor.r, spellColor.g, spellColor.b, 0f);

        // Point light for magical ambiance
        var light = missileGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = spellColor;
        light.intensity = 2f;
        light.range = 3.5f;

        var col = missileGO.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.35f;

        return missileGO;
    }

    private void PlayCastAuraVFX()
    {
        var flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "CastFlash";
        flash.transform.position = (castPoint != null ? castPoint.position : transform.position) + Vector3.up * 0.4f;
        flash.transform.localScale = Vector3.one * 0.4f;
        Destroy(flash.GetComponent<Collider>());

        var ren = flash.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = spellColor;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", spellColor * 4f);
        ren.sharedMaterial = mat;

        Destroy(flash, 0.15f);
    }

    private static float[] GetSalvoSpreadAngles(int count)
    {
        if (count <= 1) return new float[] { 0f };
        if (count == 2) return new float[] { -20f, 20f };
        if (count == 3) return new float[] { -28f, 0f, 28f };
        if (count == 4) return new float[] { -30f, -10f, 10f, 30f };

        float[] angles = new float[count];
        float spread = 60f;
        float step = spread / (count - 1);
        for (int i = 0; i < count; i++)
        {
            angles[i] = -spread * 0.5f + step * i;
        }
        return angles;
    }

    public void SetTarget(Transform newTarget) => target = newTarget;
}

