using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyConfigSO config;

    [SerializeField] private EnemyRuntimeSetSO enemyRuntimeSet;
    [SerializeField] private NPCRuntimeSetSO npcRuntimeSet;
    [SerializeField] private PlayerReferenceSO playerReference;

    private Rigidbody rb;

    private Vector3 passiveDirection;
    private float directionTimer;

    private Transform currentTarget;
    private NPC currentTargetNPC;

    private Coroutine attackCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    private void OnEnable()
    {
        if (enemyRuntimeSet != null)
            enemyRuntimeSet.Add(this);
    }

    private void OnDisable()
    {
        if (enemyRuntimeSet != null)
            enemyRuntimeSet.Remove(this);

        StopAttack();
    }

    private void Start()
    {
        ChooseNewPassiveDirection();
    }

    private void FixedUpdate()
    {
        if (config == null)
            return;

        FindTarget();

        if (currentTarget == null)
        {
            StopAttack();
            PassiveMove();
            return;
        }

        Vector3 toTarget =
            currentTarget.position - transform.position;

        toTarget.y = 0f;

        float distance = toTarget.magnitude;

        if (distance > config.detectionRadius)
        {
            currentTarget = null;
            currentTargetNPC = null;

            StopAttack();
            PassiveMove();
            return;
        }

        // Если преследуем NPC
        if (currentTargetNPC != null)
        {
            NPCHealthState health =
                currentTargetNPC.GetComponent<NPCHealthState>();

            if (health != null &&
                health.CurrentState == NPCState.Infected)
            {
                currentTarget = null;
                currentTargetNPC = null;

                StopAttack();
                return;
            }

            // Запускаем корутину один раз
            StartAttack();

            if (distance <= config.attackDistance)
            {
                StopMovement();
            }
            else
            {
                Chase(toTarget.normalized);
            }

            return;
        }

        // Если цель — Player
        StopAttack();
        Chase(toTarget.normalized);
    }

    private void FindTarget()
    {
        currentTarget = null;
        currentTargetNPC = null;

        float nearestDistance = float.MaxValue;

        // ===== PLAYER =====

        if (playerReference != null &&
            playerReference.Player != null)
        {
            float distance =
                (playerReference.Player.position -
                 transform.position)
                .sqrMagnitude;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                currentTarget = playerReference.Player;
                currentTargetNPC = null;
            }
        }

        // ===== NPC =====

        if (npcRuntimeSet == null)
            return;

        foreach (NPC npc in npcRuntimeSet.NPCs)
        {
            if (npc == null)
                continue;

            NPCHealthState health =
                npc.GetComponent<NPCHealthState>();

            // Игнорируем заражённых NPC
            if (health != null &&
                health.CurrentState == NPCState.Infected)
            {
                continue;
            }

            float distance =
                (npc.transform.position -
                 transform.position)
                .sqrMagnitude;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;

                currentTarget = npc.transform;
                currentTargetNPC = npc;
            }
        }
    }

    private void Chase(Vector3 direction)
    {
        if (direction == Vector3.zero)
            return;

        Vector3 velocity = rb.linearVelocity;

        velocity.x =
            direction.x * config.moveSpeed;

        velocity.z =
            direction.z * config.moveSpeed;

        rb.linearVelocity = velocity;

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

        velocity.x =
            passiveDirection.x *
            config.passiveMoveSpeed;

        velocity.z =
            passiveDirection.z *
            config.passiveMoveSpeed;

        rb.linearVelocity = velocity;

        if (passiveDirection != Vector3.zero)
        {
            transform.forward =
                passiveDirection;
        }
    }

    private void ChooseNewPassiveDirection()
    {
        Vector2 randomDirection =
            Random.insideUnitCircle.normalized;

        passiveDirection =
            new Vector3(
                randomDirection.x,
                0f,
                randomDirection.y
            );

        directionTimer =
            config.changeDirectionTime;
    }

    private void StartAttack()
    {
        if (attackCoroutine != null)
            return;

        attackCoroutine =
            StartCoroutine(
                AttackCoroutine()
            );
    }

    private IEnumerator AttackCoroutine()
    {
        while (currentTargetNPC != null)
        {
            yield return new WaitForSeconds(
                config.attackInterval
            );

            if (currentTargetNPC == null)
                break;

            NPCHealthState health =
                currentTargetNPC.GetComponent<NPCHealthState>();

            if (health == null)
                break;

            if (health.CurrentState == NPCState.Infected)
                break;

            Vector3 toNPC =
                currentTargetNPC.transform.position -
                transform.position;

            toNPC.y = 0f;

            float distance = toNPC.magnitude;

            // Удар только если враг действительно догнал NPC
            if (distance <= config.attackDistance)
            {
                health.ReceiveEnemyHit();
            }
        }

        attackCoroutine = null;
    }

    private void StopAttack()
    {
        if (attackCoroutine == null)
            return;

        StopCoroutine(attackCoroutine);
        attackCoroutine = null;
    }

    private void StopMovement()
    {
        Vector3 velocity = rb.linearVelocity;

        velocity.x = 0f;
        velocity.z = 0f;

        rb.linearVelocity = velocity;
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