using UnityEngine;

namespace DungeonCrawl.Gameplay
{
    /// <summary>
    /// Controls stylized/chibi quadruped feline animations:
    /// - Resting/Sitting idle pose (hind legs folded, front paws planted, curled tail, gentle breathing)
    /// - 4-legged quadruped walk cycle (diagonal trot gait, body sway, head bob)
    /// - Fast trot/run cycle (bounding quadruped stride, spine flex, dynamic tail stream)
    /// - Alert casting pose (faces target, braced quadruped stance, tail straight up with alert hook)
    /// - Camera-facing overhead companion crest.
    /// </summary>
    public class CatFelineVisuals : MonoBehaviour
    {
        [Header("Hierarchy References")]
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private Transform head;
        [SerializeField] private Transform princessCrown;
        [SerializeField] private Transform leftEar;
        [SerializeField] private Transform rightEar;

        [Header("Quadruped Legs")]
        [SerializeField] private Transform legFL;
        [SerializeField] private Transform legFR;
        [SerializeField] private Transform legBL;
        [SerializeField] private Transform legBR;

        [Header("Multi-Segment Tail")]
        [SerializeField] private Transform[] tailSegments;

        [Header("Overhead Billboard")]
        [SerializeField] private Transform overheadIcon;

        [Header("Animation Tuning")]
        [SerializeField] private float walkGaitFrequency = 2.4f;
        [SerializeField] private float walkStrideAngle = 26f;
        [SerializeField] private float runGaitFrequency = 3.8f;
        [SerializeField] private float runStrideAngle = 40f;
        [SerializeField] private float sitTransitionSpeed = 4f;

        private CatCompanion _companion;
        private Camera _mainCamera;

        // Rest / default transforms
        private Vector3 _bodyRestPos;
        private Quaternion _bodyRestRot;
        private Quaternion _headRestRot;
        private Quaternion _leftEarRestRot;
        private Quaternion _rightEarRestRot;
        private Quaternion _flRestRot, _frRestRot, _blRestRot, _brRestRot;
        private Quaternion[] _tailRestRots;

        // Animation state weights
        private float _sitWeight = 1f; // 1 = fully sitting, 0 = standing/moving
        private float _walkWeight = 0f;
        private float _runWeight = 0f;
        private float _castWeight = 0f;
        private float _gaitPhase;

        // Ear twitch state
        private float _nextTwitchTime;
        private float _twitchTimer;
        private int _twitchEar;

        private void Awake()
        {
            _companion = GetComponentInParent<CatCompanion>();
            CacheRestPoses();
        }

        private void Start()
        {
            _mainCamera = Camera.main;
            _nextTwitchTime = Time.time + Random.Range(2f, 5f);
        }

        public void CacheRestPoses()
        {
            if (bodyRoot != null)
            {
                _bodyRestPos = bodyRoot.localPosition;
                _bodyRestRot = bodyRoot.localRotation;
            }
            if (head != null) _headRestRot = head.localRotation;
            if (leftEar != null) _leftEarRestRot = leftEar.localRotation;
            if (rightEar != null) _rightEarRestRot = rightEar.localRotation;

            if (legFL != null) _flRestRot = legFL.localRotation;
            if (legFR != null) _frRestRot = legFR.localRotation;
            if (legBL != null) _blRestRot = legBL.localRotation;
            if (legBR != null) _brRestRot = legBR.localRotation;

            if (tailSegments != null && tailSegments.Length > 0)
            {
                _tailRestRots = new Quaternion[tailSegments.Length];
                for (int i = 0; i < tailSegments.Length; i++)
                {
                    if (tailSegments[i] != null) _tailRestRots[i] = tailSegments[i].localRotation;
                }
            }
        }

        private void LateUpdate()
        {
            UpdateOverheadBillboard();
            UpdateAnimationWeights();
            ApplyQuadrupedLocomotion();
            ApplyTailAnimation();
            ApplyEarTwitch();
        }

        private void UpdateOverheadBillboard()
        {
            if (overheadIcon != null)
            {
                if (_mainCamera == null) _mainCamera = Camera.main;
                if (_mainCamera != null)
                {
                    overheadIcon.rotation = _mainCamera.transform.rotation;
                }
            }
        }

        private void UpdateAnimationWeights()
        {
            float speed = _companion != null ? _companion.CurrentSpeed : 0f;
            bool isCasting = _companion != null && _companion.IsCasting;

            float targetCast = isCasting ? 1f : 0f;
            float targetSit = (!isCasting && speed < 0.15f) ? 1f : 0f;

            float targetWalk = 0f;
            float targetRun = 0f;
            if (!isCasting && speed >= 0.15f)
            {
                float runThreshold = _companion != null ? _companion.WalkSpeed + 0.6f : 3.8f;
                if (speed < runThreshold)
                {
                    targetWalk = Mathf.Clamp01(speed / 2.5f);
                    targetRun = 0f;
                }
                else
                {
                    targetWalk = 0f;
                    targetRun = Mathf.Clamp01((speed - runThreshold) / 2.5f + 0.5f);
                }
            }

            float dt = Time.deltaTime;
            _sitWeight = Mathf.MoveTowards(_sitWeight, targetSit, sitTransitionSpeed * dt);
            _walkWeight = Mathf.MoveTowards(_walkWeight, targetWalk, 6f * dt);
            _runWeight = Mathf.MoveTowards(_runWeight, targetRun, 6f * dt);
            _castWeight = Mathf.MoveTowards(_castWeight, targetCast, 8f * dt);

            // Advance gait phase when walking or running
            float movingSpeed = Mathf.Max(speed, 0.2f);
            float currentFreq = Mathf.Lerp(walkGaitFrequency, runGaitFrequency, _runWeight);
            if (speed > 0.1f)
            {
                _gaitPhase += movingSpeed * currentFreq * dt;
            }
            else
            {
                _gaitPhase = Mathf.MoveTowards(_gaitPhase, Mathf.Round(_gaitPhase), 4f * dt);
            }
        }

        private void ApplyQuadrupedLocomotion()
        {
            if (bodyRoot == null) return;

            // 1. Sitting Pose offsets
            // Rear drops by 0.08m and body pitches up 15 degrees so chest stays upright and rear is on floor
            Vector3 sitPosOffset = new Vector3(0f, -0.075f, -0.02f);
            Quaternion sitBodyRot = Quaternion.Euler(15f, 0f, 0f);

            // 2. Walking & Running dynamics
            float strideAngle = Mathf.Lerp(walkStrideAngle, runStrideAngle, _runWeight);
            float swing = Mathf.Sin(_gaitPhase * Mathf.PI * 2f) * strideAngle;
            float moveWeight = Mathf.Max(_walkWeight, _runWeight);

            // Body sway and double bounce during stride
            float bodyBob = Mathf.Abs(Mathf.Sin(_gaitPhase * Mathf.PI * 2f)) * (0.012f + _runWeight * 0.015f) * moveWeight;
            float bodyRoll = Mathf.Sin(_gaitPhase * Mathf.PI * 2f) * (2.2f + _runWeight * 2f) * moveWeight;
            float bodyPitch = Mathf.Cos(_gaitPhase * Mathf.PI * 2f) * (1.5f + _runWeight * 2.5f) * moveWeight;

            // Breathing pulse in idle
            float breathe = Mathf.Sin(Time.time * 2.2f) * 0.012f * _sitWeight;

            // Compose body transform
            Vector3 finalBodyPos = _bodyRestPos + Vector3.Lerp(Vector3.zero, sitPosOffset, _sitWeight) + Vector3.up * (bodyBob + breathe);
            Quaternion walkBodyRot = Quaternion.Euler(bodyPitch, 0f, bodyRoll);
            Quaternion finalBodyRot = _bodyRestRot * Quaternion.Slerp(walkBodyRot, sitBodyRot, _sitWeight);

            // Alert casting stance: braced, proud chest
            if (_castWeight > 0.001f)
            {
                finalBodyPos = Vector3.Lerp(finalBodyPos, _bodyRestPos + new Vector3(0f, -0.02f, 0f), _castWeight);
                finalBodyRot = Quaternion.Slerp(finalBodyRot, _bodyRestRot * Quaternion.Euler(-5f, 0f, 0f), _castWeight);
            }

            bodyRoot.localPosition = finalBodyPos;
            bodyRoot.localRotation = finalBodyRot;

            // Head counter-rotation and idle curiosity
            if (head != null)
            {
                // Counteract sitting body pitch so head looks straight ahead
                Quaternion sitHeadComp = Quaternion.Euler(-13f, 0f, 0f);
                float headBob = Mathf.Cos(_gaitPhase * Mathf.PI * 2f) * -2.5f * moveWeight;
                Quaternion headDyn = Quaternion.Euler(headBob, 0f, 0f);
                Quaternion finalHeadRot = _headRestRot * Quaternion.Slerp(headDyn, sitHeadComp, _sitWeight);

                // In casting pose, head is held alert and high facing the target
                if (_castWeight > 0.001f)
                {
                    finalHeadRot = Quaternion.Slerp(finalHeadRot, _headRestRot * Quaternion.Euler(8f, 0f, 0f), _castWeight);
                }
                head.localRotation = finalHeadRot;
            }

            // 3. Leg Rotations: Trot gait (FL & BR together, FR & BL opposite)
            // Sitting leg pose: front legs straight upright under chest; back legs tucked into hips
            Quaternion flSit = _flRestRot * Quaternion.Euler(-14f, 0f, 0f);
            Quaternion frSit = _frRestRot * Quaternion.Euler(-14f, 0f, 0f);
            Quaternion blSit = _blRestRot * Quaternion.Euler(-52f, 12f, 0f);
            Quaternion brSit = _brRestRot * Quaternion.Euler(-52f, -12f, 0f);

            // Walking / running swing
            Quaternion flWalk = _flRestRot * Quaternion.Euler(swing, 0f, 0f);
            Quaternion frWalk = _frRestRot * Quaternion.Euler(-swing, 0f, 0f);
            Quaternion blWalk = _blRestRot * Quaternion.Euler(-swing, 0f, 0f);
            Quaternion brWalk = _brRestRot * Quaternion.Euler(swing, 0f, 0f);

            // Alert casting leg stance: braced, stable quadruped
            Quaternion flCast = _flRestRot * Quaternion.Euler(4f, -6f, 0f);
            Quaternion frCast = _frRestRot * Quaternion.Euler(4f, 6f, 0f);
            Quaternion blCast = _blRestRot * Quaternion.Euler(-6f, -4f, 0f);
            Quaternion brCast = _brRestRot * Quaternion.Euler(-6f, 4f, 0f);

            if (legFL != null)
            {
                Quaternion q = Quaternion.Slerp(flWalk, flSit, _sitWeight);
                legFL.localRotation = Quaternion.Slerp(q, flCast, _castWeight);
            }
            if (legFR != null)
            {
                Quaternion q = Quaternion.Slerp(frWalk, frSit, _sitWeight);
                legFR.localRotation = Quaternion.Slerp(q, frCast, _castWeight);
            }
            if (legBL != null)
            {
                Quaternion q = Quaternion.Slerp(blWalk, blSit, _sitWeight);
                legBL.localRotation = Quaternion.Slerp(q, blCast, _castWeight);
            }
            if (legBR != null)
            {
                Quaternion q = Quaternion.Slerp(brWalk, brSit, _sitWeight);
                legBR.localRotation = Quaternion.Slerp(q, brCast, _castWeight);
            }
        }

        private void ApplyTailAnimation()
        {
            if (tailSegments == null || tailSegments.Length == 0) return;

            float time = Time.time;
            float moveWeight = Mathf.Max(_walkWeight, _runWeight);

            // Walking/running tail sway wave
            float tailSway = Mathf.Sin(_gaitPhase * Mathf.PI * 2f) * 12f * moveWeight;

            for (int i = 0; i < tailSegments.Length; i++)
            {
                if (tailSegments[i] == null) continue;
                Quaternion rest = i < _tailRestRots.Length ? _tailRestRots[i] : Quaternion.identity;

                // 1. Sitting tail pose: curls gracefully along the right flank and paws
                float sitAngleY = (i + 1) * 22f;
                float sitTipCurl = i >= tailSegments.Length - 2 ? Mathf.Sin(time * 2f) * 8f : 0f;
                Quaternion sitRot = rest * Quaternion.Euler(-10f, sitAngleY + sitTipCurl, 0f);

                // 2. Walking / running tail: waves lazily behind the cat
                float waveLag = i * 0.45f;
                float waveY = Mathf.Sin((_gaitPhase * Mathf.PI * 2f) - waveLag) * (8f + i * 4f) * moveWeight;
                float runBounceX = _runWeight * (Mathf.Sin(time * 8f) * 6f);
                Quaternion moveRot = rest * Quaternion.Euler(runBounceX, waveY, 0f);

                // Blend between move and sit
                Quaternion blendedRot = Quaternion.Slerp(moveRot, sitRot, _sitWeight);

                // 3. ALERT CASTING POSE: TAIL STRAIGHT UP!
                // Segment 0 angles almost straight up (-75 deg pitch), middle segments maintain line,
                // and the tip forms an alert, charismatic feline hook/arch!
                float castPitch = i switch
                {
                    0 => -78f,
                    1 => -18f,
                    2 => 10f,
                    _ => 28f
                };
                float tipShimmer = i == tailSegments.Length - 1 ? Mathf.Sin(time * 35f) * 3f : 0f;
                Quaternion castRot = rest * Quaternion.Euler(castPitch + tipShimmer, 0f, 0f);

                tailSegments[i].localRotation = Quaternion.Slerp(blendedRot, castRot, _castWeight);
            }
        }

        private void ApplyEarTwitch()
        {
            // Perked alert ears when casting
            if (_castWeight > 0.001f)
            {
                if (leftEar != null) leftEar.localRotation = Quaternion.Slerp(_leftEarRestRot, _leftEarRestRot * Quaternion.Euler(14f, 0f, -8f), _castWeight);
                if (rightEar != null) rightEar.localRotation = Quaternion.Slerp(_rightEarRestRot, _rightEarRestRot * Quaternion.Euler(14f, 0f, 8f), _castWeight);
                return;
            }

            // Random idle ear twitches
            if (Time.time >= _nextTwitchTime && _twitchEar == 0)
            {
                _twitchEar = Random.value > 0.5f ? 1 : 2;
                _twitchTimer = 0f;
                _nextTwitchTime = Time.time + Random.Range(3f, 6.5f);
            }

            if (_twitchEar != 0)
            {
                _twitchTimer += Time.deltaTime;
                float twitch = Mathf.Sin(_twitchTimer * 30f) * 14f;

                if (_twitchEar == 1 && leftEar != null)
                {
                    leftEar.localRotation = _leftEarRestRot * Quaternion.Euler(0f, 0f, twitch);
                }
                else if (_twitchEar == 2 && rightEar != null)
                {
                    rightEar.localRotation = _rightEarRestRot * Quaternion.Euler(0f, 0f, -twitch);
                }

                if (_twitchTimer >= 0.22f)
                {
                    if (leftEar != null) leftEar.localRotation = _leftEarRestRot;
                    if (rightEar != null) rightEar.localRotation = _rightEarRestRot;
                    _twitchEar = 0;
                }
            }
        }
    }
}

