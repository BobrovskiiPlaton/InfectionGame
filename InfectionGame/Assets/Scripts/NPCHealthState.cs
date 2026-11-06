using System.Collections;
using UnityEngine;

public enum NPCState
{
    Healthy,
    Damaged,
    Infected,
    Recovered,
    Immune
}

public class NPCHealthState : MonoBehaviour
{
    [SerializeField]
    private NPCState currentState = NPCState.Healthy;

    [Header("Infection")]
    [SerializeField]
    private int hitsToInfect = 3;

    [SerializeField]
    private float infectionDuration = 30f;

    [Header("Treatment")]
    [SerializeField]
    private float infectionProgress = 100f;

    [Header("Colors")]
    [SerializeField]
    private Color healthyColor = Color.blue;

    [SerializeField]
    private Color damagedColor = Color.green;

    [SerializeField]
    private Color infectedColor = Color.red;

    [SerializeField] private EnemySpawnRequestSO enemySpawnRequest;

    private int receivedHits;

    private Renderer npcRenderer;

    private Coroutine infectionCoroutine;

    public NPCState CurrentState => currentState;

    private void Awake()
    {
        npcRenderer =
            GetComponentInChildren<Renderer>();
    }

    private void Start()
    {
        SetColor(healthyColor);
    }

    public void ReceiveEnemyHit()
    {
        if (currentState == NPCState.Infected ||
            currentState == NPCState.Immune)
            return;

        receivedHits++;

        Debug.Log(
            $"{name}: {receivedHits}/{hitsToInfect}"
        );

        if (receivedHits >= hitsToInfect)
        {
            BecomeInfected();
        }
        else
        {
            currentState = NPCState.Damaged;

            SetColor(damagedColor);
        }
    }

    private void BecomeInfected()
    {
        currentState = NPCState.Infected;

        infectionProgress = 100f;

        SetColor(infectedColor);

        infectionCoroutine =
            StartCoroutine(InfectionCoroutine());

        Debug.Log($"{name} infected");
    }

    private IEnumerator InfectionCoroutine()
    {
        yield return new WaitForSeconds(infectionDuration);

        if (currentState == NPCState.Infected)
        {
            ConvertToEnemy();
        }
    }

    public void Treat(float amount)
    {
        if (currentState != NPCState.Infected)
            return;

        infectionProgress -= amount;

        if (infectionProgress <= 0f)
        {
            Recover();
        }
    }

    private void Recover()
    {
        currentState = NPCState.Recovered;

        infectionProgress = 0f;
        receivedHits = 0;

        if (infectionCoroutine != null)
        {
            StopCoroutine(infectionCoroutine);
            infectionCoroutine = null;
        }

        SetColor(healthyColor);

        Debug.Log($"{name} recovered");
    }

    private void ConvertToEnemy()
    {
        Debug.Log($"{name}: converting to enemy");

        if (enemySpawnRequest == null)
        {
            Debug.LogError($"{name}: EnemySpawnRequest is not assigned!");
            return;
        }

        enemySpawnRequest.Raise(transform.position);

        Destroy(gameObject);
    }

    private void SetColor(Color color)
    {
        if (npcRenderer != null)
        {
            npcRenderer.material.color = color;
        }
    }
}