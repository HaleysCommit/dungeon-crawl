using UnityEngine;

namespace DungeonCrawl.Gameplay
{
    /// <summary>
    /// Adds natural menacing idle animations to the goblin model:
    /// - Heavy breathing & hunch bob
    /// - Head tracking towards player when nearby
    /// - Ear twitch & snarling jaw motion
    /// - Spiked club sway
    /// </summary>
    public class GoblinIdleAnim : MonoBehaviour
    {
        [Header("Bones")]
        [SerializeField] private Transform torso;
        [SerializeField] private Transform head;
        [SerializeField] private Transform jaw;
        [SerializeField] private Transform leftEar;
        [SerializeField] private Transform rightEar;
        [SerializeField] private Transform weaponArm;

        [Header("Tuning")]
        [SerializeField] private float breathSpeed = 2.5f;
        [SerializeField] private float breathAmount = 0.035f;

        private Vector3 _torsoInitialPos;
        private Quaternion _torsoInitialRot;
        private Quaternion _headInitialRot;
        private Quaternion _earLInitialRot;
        private Quaternion _earRInitialRot;
        private Transform _playerTransform;
        private float _seed;

        private void Start()
        {
            _seed = Random.Range(0f, 100f);
            if (torso != null)
            {
                _torsoInitialPos = torso.localPosition;
                _torsoInitialRot = torso.localRotation;
            }
            if (head != null) _headInitialRot = head.localRotation;
            if (leftEar != null) _earLInitialRot = leftEar.localRotation;
            if (rightEar != null) _earRInitialRot = rightEar.localRotation;

            var player = GameObject.Find("Colonel");
            if (player != null) _playerTransform = player.transform;
        }

        private void Update()
        {
            float t = Time.time * breathSpeed + _seed;

            // 1. Heavy breathing & menacing hunch
            if (torso != null)
            {
                float bob = Mathf.Sin(t) * breathAmount;
                float hunch = Mathf.Sin(t * 0.5f) * 3f;
                torso.localPosition = _torsoInitialPos + Vector3.up * bob;
                torso.localRotation = _torsoInitialRot * Quaternion.Euler(hunch, 0f, 0f);
            }

            // 2. Head snarling & occasional glance towards player
            if (head != null)
            {
                float headShake = Mathf.Sin(t * 1.8f) * 4f;
                Quaternion lookRot = Quaternion.identity;

                if (_playerTransform != null)
                {
                    Vector3 toPlayer = _playerTransform.position - transform.position;
                    toPlayer.y = 0f;
                    if (toPlayer.sqrMagnitude < 100f && toPlayer.sqrMagnitude > 0.01f)
                    {
                        Quaternion targetLook = Quaternion.LookRotation(transform.InverseTransformDirection(toPlayer.normalized), Vector3.up);
                        lookRot = Quaternion.Slerp(Quaternion.identity, targetLook, 0.35f);
                    }
                }

                head.localRotation = _headInitialRot * lookRot * Quaternion.Euler(Mathf.Sin(t * 2.2f) * 3f, headShake, 0f);
            }

            // 3. Ear twitch
            if (leftEar != null)
            {
                float twitch = Mathf.Sin(t * 0.7f) > 0.92f ? Mathf.Sin(Time.time * 25f) * 12f : 0f;
                leftEar.localRotation = _earLInitialRot * Quaternion.Euler(0f, twitch, 0f);
            }
            if (rightEar != null)
            {
                float twitch = Mathf.Sin(t * 0.9f) > 0.92f ? Mathf.Sin(Time.time * 25f) * -12f : 0f;
                rightEar.localRotation = _earRInitialRot * Quaternion.Euler(0f, twitch, 0f);
            }

            // 4. Snarling jaw motion
            if (jaw != null)
            {
                float snarl = Mathf.PingPong(t * 1.5f, 1f) * 8f;
                jaw.localRotation = Quaternion.Euler(snarl, 0f, 0f);
            }

            // 5. Spiked weapon arm idle sway
            if (weaponArm != null)
            {
                float armSway = Mathf.Sin(t) * 5f;
                weaponArm.localRotation = Quaternion.Euler(25f + armSway, -15f, armSway * 0.5f);
            }
        }
    }
}
