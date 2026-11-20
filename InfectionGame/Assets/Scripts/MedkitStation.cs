using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MedkitStation : MonoBehaviour
{
    [SerializeField]
    private MedkitConfigSO config;

    private static readonly List<MedkitStation> stations = new();

    public static IReadOnlyList<MedkitStation> Stations => stations;

    private Collider stationCollider;
    private CancellationTokenSource cooldownCancellation;

    public float RemainingCooldown { get; private set; }

    public float CooldownDuration =>
        config != null ? config.cooldownDuration : 0f;

    public float InteractionRadius =>
        config != null ? config.interactionRadius : 0f;

    public bool IsReady =>
        config != null && RemainingCooldown <= 0f;

    private void Awake()
    {
        stationCollider = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        if (!stations.Contains(this))
            stations.Add(this);
    }

    private void OnDisable()
    {
        stations.Remove(this);
        CancelCooldown();
    }

    private void OnDestroy()
    {
        CancelCooldown();
    }

    // Расстояние от игрока до ближайшей точки аптечки
    public float GetDistanceSqr(Vector3 position)
    {
        Vector3 closestPoint = stationCollider != null
            ? stationCollider.ClosestPoint(position)
            : transform.position;

        return (closestPoint - position).sqrMagnitude;
    }

    // Проверка нахождения игрока рядом с аптечкой
    public bool IsInRange(Vector3 position)
    {
        float radius = InteractionRadius;

        return GetDistanceSqr(position) <= radius * radius;
    }

    public bool TryHeal(PlayerHealth player)
    {
        if (!IsReady || player == null)
            return false;

        if (!IsInRange(player.transform.position))
            return false;

        if (player.CurrentHealth >= player.MaxHealth)
            return false;

        player.Heal(config.playerHealAmount);

        StartCooldown();

        return true;
    }

    private void StartCooldown()
    {
        CancelCooldown();

        RemainingCooldown = config.cooldownDuration;

        cooldownCancellation = new CancellationTokenSource();

        RunCooldownAsync(cooldownCancellation.Token);
    }

    private async Awaitable RunCooldownAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            while (RemainingCooldown > 0f)
            {
                float step = Mathf.Min(
                    0.05f,
                    RemainingCooldown
                );

                await Awaitable.WaitForSecondsAsync(
                    step,
                    cancellationToken
                );

                RemainingCooldown = Mathf.Max(
                    0f,
                    RemainingCooldown - step
                );
            }
        }
        catch (OperationCanceledException)
        {
            // Аптечка отключена.
        }
    }

    private void CancelCooldown()
    {
        if (cooldownCancellation == null)
            return;

        cooldownCancellation.Cancel();
        cooldownCancellation.Dispose();
        cooldownCancellation = null;
    }
}