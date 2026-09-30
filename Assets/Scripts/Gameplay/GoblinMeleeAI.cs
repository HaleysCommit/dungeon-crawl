using System.Collections;
using UnityEngine;
using MoreMountains.TopDownEngine;

namespace DungeonCrawl.Gameplay
{
    /// <summary>
    /// Chases the player on foot (CharacterController + gravity, same grounded movement style as CatCompanion)
    /// and lands melee hits on a cooldown once in range. The goblin model previously only had GoblinIdleAnim
    /// (idle breathing/ear-twitch) and no Health/AI/attack of any kind.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Health))]
    public class GoblinMeleeAI : MonoBehaviour
    {
        [Header("Detection")]
        [Tooltip("Distance at which the goblin notices and starts chasing the player.")]
        [SerializeField] private float aggroRange = 10f;
        [Tooltip("Distance beyond which a goblin already chasing gives up.")]
        [SerializeField] private float loseAggroRange = 16f;

        [Header("Movement")]
        [SerializeField] private float chaseSpeed = 3.5f;
        [SerializeField] private float acceleration = 14f;
        [SerializeField] private float deceleration = 18f;
        [SerializeField] private float turnSpeedDegrees = 360f;
        [SerializeField] private float gravity = -20f;

        [Header("Attack")]
        [SerializeField] private float attackRange = 1.6f;
        [SerializeField] private float attackCooldown = 1.4f;
        [SerializeField] private float attackWindup = 0.35f;
        [SerializeField] private float damage = 8f;
        [Tooltip("Optional weapon arm bone to swing during the attack windup.")]
        [SerializeField] private Transform weaponArm;
        [SerializeField] private AudioClip attackSound;

        private CharacterController _controller;
        private Health _health;
        private Transform _player;
        private Health _playerHealth;
        private Vector3 _planarVelocity;
        private float _verticalVelocity;
        private float _nextAttackTime;
        private bool _isAttacking;
        private Quaternion _weaponArmRestRotation;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _health = GetComponent<Health>();
            if (weaponArm != null)
            {
                _weaponArmRestRotation = weaponArm.localRotation;
            }
        }

        private void Start()
        {
            FindPlayer();
        }

        private void FindPlayer()
        {
            foreach (var character in Object.FindObjectsByType<Character>(FindObjectsInactive.Exclude))
            {
                if (character.CharacterType == Character.CharacterTypes.Player)
                {
                    _player = character.transform;
                    _playerHealth = character.CharacterHealth != null ? character.CharacterHealth : character.GetComponent<Health>();
                    break;
                }
            }
        }

        private void Update()
        {
            if (_health != null && _health.CurrentHealth <= 0f)
            {
                _planarVelocity = Vector3.zero;
                return;
            }

            if (_player == null)
            {
                FindPlayer();
                return;
            }

            Vector3 toPlayer = _player.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            bool inAttackRange = distance <= attackRange;

            Vector3 desiredVelocity = Vector3.zero;

            if (!_isAttacking && distance <= (_planarVelocity.sqrMagnitude > 0.01f ? loseAggroRange : aggroRange))
            {
                if (toPlayer.sqrMagnitude > 0.0001f)
                {
                    Quaternion desiredRotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, turnSpeedDegrees * Time.deltaTime);
                }

                if (!inAttackRange)
                {
                    desiredVelocity = toPlayer.normalized * chaseSpeed;
                }
                else if (Time.time >= _nextAttackTime)
                {
                    StartCoroutine(AttackRoutine());
                }
            }

            float rate = desiredVelocity.sqrMagnitude > _planarVelocity.sqrMagnitude ? acceleration : deceleration;
            _planarVelocity = Vector3.MoveTowards(_planarVelocity, desiredVelocity, rate * Time.deltaTime);

            _verticalVelocity = _controller.isGrounded ? -1f : _verticalVelocity + gravity * Time.deltaTime;
            _controller.Move((_planarVelocity + Vector3.up * _verticalVelocity) * Time.deltaTime);
        }

        private IEnumerator AttackRoutine()
        {
            _isAttacking = true;
            _nextAttackTime = Time.time + attackCooldown;
            _planarVelocity = Vector3.zero;

            if (weaponArm != null)
            {
                float elapsed = 0f;
                while (elapsed < attackWindup)
                {
                    elapsed += Time.deltaTime;
                    float swing = Mathf.Sin((elapsed / attackWindup) * Mathf.PI) * -50f;
                    weaponArm.localRotation = _weaponArmRestRotation * Quaternion.Euler(swing, 0f, 0f);
                    yield return null;
                }
                weaponArm.localRotation = _weaponArmRestRotation;
            }
            else
            {
                yield return new WaitForSeconds(attackWindup);
            }

            if (attackSound != null)
            {
                AudioSource.PlayClipAtPoint(attackSound, transform.position, 0.8f);
            }

            // only land the hit if the player is still in range after the windup
            if (_player != null && _playerHealth != null
                && Vector3.Distance(transform.position, _player.position) <= attackRange + 0.3f)
            {
                Vector3 direction = (_player.position - transform.position).normalized;
                _playerHealth.Damage(damage, gameObject, 0.2f, 0.5f, direction);
            }

            _isAttacking = false;
        }
    }
}
