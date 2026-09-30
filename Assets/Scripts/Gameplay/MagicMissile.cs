using System.Collections;
using UnityEngine;
using MoreMountains.TopDownEngine;

namespace DungeonCrawl.Gameplay
{
    /// <summary>
    /// Homing arcane projectile cast by the pet cat.
    /// Arcs outwards on spawn and smoothly tracks enemies before detonating on impact.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class MagicMissile : MonoBehaviour
    {
        [Header("Flight Dynamics")]
        [SerializeField] private float initialSpeed = 10f;
        [SerializeField] private float maxSpeed = 22f;
        [SerializeField] private float acceleration = 30f;
        [SerializeField] private float turnRate = 480f; // degrees per second
        [SerializeField] private float maxLifetime = 5f;

        [Header("Damage")]
        [SerializeField] private float damage = 18f;

        [Header("Effects")]
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private GameObject hitEffectPrefab;

        private Transform _target;
        private Vector3 _lastKnownTargetPosition;
        private Vector3 _currentVelocity;
        private float _currentSpeed;
        private float _spawnTime;
        private GameObject _caster;
        private LayerMask _targetLayers;
        private LayerMask _obstacleLayers;
        private bool _detonated;

        public void Initialize(
            Transform target,
            Vector3 launchDirection,
            float spellDamage,
            GameObject caster,
            LayerMask targetLayers,
            LayerMask obstacleLayers,
            AudioClip hitAudio = null)
        {
            _target = target;
            if (_target != null)
            {
                _lastKnownTargetPosition = GetTargetCenter(_target);
            }
            else
            {
                _lastKnownTargetPosition = transform.position + launchDirection * 10f;
            }

            damage = spellDamage;
            _caster = caster;
            _targetLayers = targetLayers;
            _obstacleLayers = obstacleLayers;
            if (hitAudio != null) hitSound = hitAudio;

            _currentSpeed = initialSpeed;
            _currentVelocity = launchDirection.normalized * _currentSpeed;
            _spawnTime = Time.time;
            transform.forward = launchDirection.normalized;

            var col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.35f;

            // Ignore collision with caster and player
            IgnoreFriendlyColliders(col);
        }

        private void IgnoreFriendlyColliders(Collider myCol)
        {
            if (myCol == null) return;

            if (_caster != null)
            {
                foreach (var c in _caster.GetComponentsInChildren<Collider>())
                {
                    if (c != null && c != myCol) Physics.IgnoreCollision(myCol, c, true);
                }
            }

            var characters = Object.FindObjectsByType<Character>(FindObjectsInactive.Exclude);
            foreach (var ch in characters)
            {
                if (ch != null && ch.CharacterType == Character.CharacterTypes.Player)
                {
                    foreach (var c in ch.GetComponentsInChildren<Collider>())
                    {
                        if (c != null && c != myCol) Physics.IgnoreCollision(myCol, c, true);
                    }
                }
            }
        }

        private bool IsFriendlyOrPlayer(GameObject go)
        {
            if (go == null) return false;

            // Ignore caster or caster's children
            if (_caster != null && (go == _caster || go.transform.IsChildOf(_caster.transform)))
            {
                return true;
            }

            // Check TopDownEngine Character type
            var character = go.GetComponentInParent<Character>();
            if (character != null && character.CharacterType == Character.CharacterTypes.Player)
            {
                return true;
            }

            // Check tag or layer
            if (go.CompareTag("Player") || (go.transform.root != null && go.transform.root.CompareTag("Player")))
            {
                return true;
            }

            if (go.layer == LayerMask.NameToLayer("Player"))
            {
                return true;
            }

            return false;
        }

        private void Update()
        {
            if (_detonated) return;

            if (Time.time - _spawnTime >= maxLifetime)
            {
                Detonate(null);
                return;
            }

            // Update target destination
            if (_target != null && _target.gameObject.activeInHierarchy)
            {
                var health = _target.GetComponent<Health>();
                if (health == null || health.CurrentHealth > 0)
                {
                    _lastKnownTargetPosition = GetTargetCenter(_target);
                }
            }

            // Accelerate towards max speed
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, maxSpeed, acceleration * Time.deltaTime);

            // Steer towards target position
            Vector3 toTarget = _lastKnownTargetPosition - transform.position;
            if (toTarget.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnRate * Time.deltaTime);
            }

            _currentVelocity = transform.forward * _currentSpeed;
            transform.position += _currentVelocity * Time.deltaTime;

            // Direct proximity check in case fast movement skips collider trigger
            if (toTarget.sqrMagnitude < 0.6f * 0.6f)
            {
                Detonate(_target != null ? _target.gameObject : null);
            }
        }

        private Vector3 GetTargetCenter(Transform t)
        {
            var col = t.GetComponent<Collider>();
            if (col != null)
            {
                return col.bounds.center;
            }
            return t.position + Vector3.up * 0.6f;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_detonated) return;

            // Never hit friendly entities or player
            if (IsFriendlyOrPlayer(other.gameObject))
            {
                return;
            }

            if (other.GetComponent<MagicMissile>() != null)
            {
                return;
            }

            int layer = other.gameObject.layer;
            bool isTargetLayer = (_targetLayers.value & (1 << layer)) != 0;
            bool isObstacleLayer = (_obstacleLayers.value & (1 << layer)) != 0;

            // Only consider hostile health targets
            var health = other.GetComponentInParent<Health>();
            bool isHostileHealth = health != null && !IsFriendlyOrPlayer(health.gameObject);

            if (isTargetLayer || isHostileHealth || isObstacleLayer)
            {
                Detonate(other.gameObject);
            }
        }

        private void Detonate(GameObject hitObject)
        {
            if (_detonated) return;
            _detonated = true;

            // Apply TopDown Engine damage (never damage friendlies or player)
            if (hitObject != null && !IsFriendlyOrPlayer(hitObject))
            {
                var health = hitObject.GetComponentInParent<Health>();
                if (health != null && health.CurrentHealth > 0)
                {
                    health.Damage(damage, _caster != null ? _caster : gameObject, 0.1f, 0.1f, transform.forward);
                }
            }

            // Play hit audio
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, transform.position, 0.75f);
            }

            // Spawn hit VFX burst
            SpawnDetonationVFX();

            Destroy(gameObject, 0.05f);
        }

        private void SpawnDetonationVFX()
        {
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                return;
            }

            // Built-in lightweight dynamic burst if no prefab assigned
            var burstGO = new GameObject("MagicMissile_Detonation");
            burstGO.transform.position = transform.position;

            var pSystem = burstGO.AddComponent<ParticleSystem>();
            var pRenderer = burstGO.GetComponent<ParticleSystemRenderer>();
            pRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Lit"))
            {
                color = new Color(0.4f, 0.8f, 1f, 1f)
            };

            var main = pSystem.main;
            main.startLifetime = 0.35f;
            main.startSpeed = 6f;
            main.startSize = 0.25f;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.2f, 0.9f, 1f, 1f),
                new Color(0.8f, 0.4f, 1f, 1f)
            );
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = pSystem.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 25) });

            var shape = pSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            pSystem.Play();
        }
    }
}
