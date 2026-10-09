using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyConfigSO config;
    [SerializeField] private PlayerReferenceSO playerReference;
    [SerializeField] private EnemyRuntimeSetSO runtimeSet;

    private Rigidbody rb;

    private Vector3 passiveDirection;
    private float directionTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    private void OnEnable()
    {
        if (runtimeSet != null)
            runtimeSet.Add(this);
    }

    private void OnDisable()
    {
        if (runtimeSet != null)
            runtimeSet.Remove(this);
    }

    private void Start()
    {
        ChooseNewPassiveDirection();
    }

    private void FixedUpdate()
    {
        if (config == null)
            return;

        Transform player = null;

        if (playerReference != null)
            player = playerReference.Player;

        if (player != null)
        {
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;

            float distance = toPlayer.magnitude;

            if (distance <= config.detectionRadius)
            {
                ChasePlayer(toPlayer.normalized);
                return;
            }
        }

        PassiveMove();
    }

    private void ChasePlayer(Vector3 direction)
    {
        Vector3 velocity = rb.linearVelocity;

        velocity.x = direction.x * config.moveSpeed;
        velocity.z = direction.z * config.moveSpeed;

        rb.linearVelocity = velocity;

        if (direction != Vector3.zero)
            transform.forward = direction;
    }

    private void PassiveMove()
    {
        directionTimer -= Time.fixedDeltaTime;

        if (directionTimer <= 0f)
        {
            ChooseNewPassiveDirection();
        }

        Vector3 velocity = rb.linearVelocity;

        velocity.x = passiveDirection.x * config.passiveMoveSpeed;
        velocity.z = passiveDirection.z * config.passiveMoveSpeed;

        rb.linearVelocity = velocity;

        if (passiveDirection != Vector3.zero)
            transform.forward = passiveDirection;
    }

    private void ChooseNewPassiveDirection()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;

        passiveDirection = new Vector3(
            randomDirection.x,
            0f,
            randomDirection.y
        );

        directionTimer = config.changeDirectionTime;
    }

    private void OnDrawGizmosSelected()
    {
        if (config == null)
            return;

        Gizmos.DrawWireSphere(
            transform.position,
            config.detectionRadius
        );
    }
}