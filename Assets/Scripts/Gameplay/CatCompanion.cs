using UnityEngine;
using MoreMountains.TopDownEngine;

/// <summary>
/// Makes a cat companion follow the player at a set distance, catching up when it falls behind
/// and idling with a small bob when already close enough. No pathfinding/NavMesh required.
/// </summary>
public class CatCompanion : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Transform to follow. If left empty, the scene's Player character is used automatically.")]
    [SerializeField] private Transform target;

    [Header("Movement")]
    [SerializeField] private float followDistance = 1.5f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Idle Bob")]
    [SerializeField] private float bobHeight = 0.05f;
    [SerializeField] private float bobSpeed = 4f;

    private Vector3 _velocity;
    private float _baseY;

    private void Start()
    {
        _baseY = transform.position.y;

        if (target == null && LevelManager.HasInstance
            && LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0)
        {
            target = LevelManager.Instance.Players[0].transform;
        }
    }

    private void Update()
    {
        if (target == null)
        {
            return;
        }

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;

        if (distance > followDistance)
        {
            Vector3 direction = toTarget.normalized;
            Vector3 desiredPosition = target.position - direction * followDistance;
            desiredPosition.y = _baseY + bobOffset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, 0.2f, moveSpeed);

            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            Vector3 idlePosition = transform.position;
            idlePosition.y = _baseY + bobOffset;
            transform.position = idlePosition;
        }
    }

    public void SetTarget(Transform newTarget) => target = newTarget;
}
