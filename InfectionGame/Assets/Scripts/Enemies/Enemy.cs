using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Scriptable Objects")]

    [SerializeField]
    private EnemyConfigSO config;

    [SerializeField]
    private PlayerReferenceSO playerReference;

    [SerializeField]
    private EnemyRuntimeSetSO runtimeSet;

    private bool isChasing;

    private void OnEnable()
    {
        if (runtimeSet != null)
        {
            runtimeSet.Add(this);
        }
    }

    private void OnDisable()
    {
        if (runtimeSet != null)
        {
            runtimeSet.Remove(this);
        }
    }

    private void Update()
    {
        if (config == null)
            return;

        if (playerReference == null)
            return;

        Transform player = playerReference.Player;

        if (player == null)
            return;

        float distanceToPlayer = Vector3.Distance(
            transform.position,
            player.position
        );

        isChasing = distanceToPlayer <= config.detectionRadius;

        if (isChasing)
        {
            ChasePlayer(player);
        }
    }

    private void ChasePlayer(Transform player)
    {
        Vector3 direction = player.position - transform.position;

        // Чтобы враг не летал вверх/вниз
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        direction.Normalize();

        transform.position +=
            direction *
            config.moveSpeed *
            Time.deltaTime;

        transform.forward = direction;
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