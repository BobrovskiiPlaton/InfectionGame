using UnityEngine;

public class NPC : MonoBehaviour
{
    [SerializeField] private NPCConfigSO config;
    [SerializeField] private NPCRuntimeSetSO npcRuntimeSet;
    [SerializeField] private EnemyRuntimeSetSO enemyRuntimeSet;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    private void OnEnable()
    {
        if (npcRuntimeSet != null)
            npcRuntimeSet.Add(this);
    }

    private void OnDisable()
    {
        if (npcRuntimeSet != null)
            npcRuntimeSet.Remove(this);
    }

    private void FixedUpdate()
    {
        Enemy nearestEnemy = GetNearestEnemy();

        if (nearestEnemy == null)
        {
            StopMovement();
            return;
        }

        Vector3 fromEnemy =
            transform.position - nearestEnemy.transform.position;

        fromEnemy.y = 0f;

        float distance = fromEnemy.magnitude;

        if (distance > config.fleeRadius)
        {
            StopMovement();
            return;
        }

        Vector3 direction = fromEnemy.normalized;

        Vector3 velocity = rb.linearVelocity;

        velocity.x = direction.x * config.moveSpeed;
        velocity.z = direction.z * config.moveSpeed;

        rb.linearVelocity = velocity;

        if (direction != Vector3.zero)
            transform.forward = direction;
    }

    private Enemy GetNearestEnemy()
    {
        if (enemyRuntimeSet == null)
            return null;

        Enemy nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Enemy enemy in enemyRuntimeSet.Enemies)
        {
            if (enemy == null)
                continue;

            float distance =
                (enemy.transform.position - transform.position)
                .sqrMagnitude;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = enemy;
            }
        }

        return nearest;
    }

    private void StopMovement()
    {
        Vector3 velocity = rb.linearVelocity;

        velocity.x = 0f;
        velocity.z = 0f;

        rb.linearVelocity = velocity;
    }
}