using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class GameHUDController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField]
    private PlayerHealth playerHealth;

    [Header("UI Settings")]
    [SerializeField, Min(0.1f)]
    private float infectionUpdateInterval = 0.5f;

    private ProgressBar healthBar;
    private ProgressBar healthpackCooldown;
    private Label infectedCounter;
    private VisualElement healthpackPanel;

    private MedkitStation nearestStation;

    private float infectionUpdateTimer;

    private void OnEnable()
    {
        UIDocument document = GetComponent<UIDocument>();

        VisualElement root = document.rootVisualElement;

        healthBar = root.Q<ProgressBar>("HealthBar");

        healthpackCooldown =
            root.Q<ProgressBar>("HealthpackCooldown");

        infectedCounter =
            root.Q<Label>("InfectedCounter");

        healthpackPanel =
            root.Q<VisualElement>("Healthpack");

        if (healthBar == null ||
            healthpackCooldown == null ||
            infectedCounter == null ||
            healthpackPanel == null)
        {
            Debug.LogError(
                "GameHUD: Не найдены элементы интерфейса."
            );

            enabled = false;
            return;
        }

        if (playerHealth == null)
        {
            playerHealth =
                FindFirstObjectByType<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += SetHealth;

            SetHealth(
                playerHealth.CurrentHealth,
                playerHealth.MaxHealth
            );
        }
        else
        {
            Debug.LogWarning(
                "GameHUD: PlayerHealth не найден."
            );
        }

        SetInfectedCount(
            ECSInfectionCounter.GetInfectedCount()
        );

        ShowHealthpack(false);

        infectionUpdateTimer = 0f;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= SetHealth;
        }
    }

    private void Update()
    {
        UpdateNearestStation();
        UpdateHealthpackUI();

        infectionUpdateTimer -= Time.deltaTime;

        if (infectionUpdateTimer <= 0f)
        {
            SetInfectedCount(
                ECSInfectionCounter.GetInfectedCount()
            );

            infectionUpdateTimer = infectionUpdateInterval;
        }

        if (nearestStation != null &&
            playerHealth != null &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            nearestStation.TryHeal(playerHealth);

            UpdateHealthpackUI();
        }
    }

    private void UpdateNearestStation()
    {
        nearestStation = null;

        if (playerHealth == null)
            return;

        Vector3 playerPosition =
            playerHealth.transform.position;

        float nearestDistanceSqr = float.MaxValue;

        foreach (MedkitStation station in MedkitStation.Stations)
        {
            if (station == null)
                continue;

            float distanceSqr =
                station.GetDistanceSqr(playerPosition);

            if (!station.IsInRange(playerPosition))
                continue;

            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestStation = station;
            }
        }
    }

    private void UpdateHealthpackUI()
    {
        if (nearestStation == null)
        {
            ShowHealthpack(false);
            return;
        }

        ShowHealthpack(true);

        SetHealthpackCooldown(
            nearestStation.RemainingCooldown,
            nearestStation.CooldownDuration
        );
    }

    public void SetHealth(float current, float maximum)
    {
        if (healthBar == null)
            return;

        float percent = maximum > 0f
            ? Mathf.Clamp01(current / maximum) * 100f
            : 0f;

        healthBar.value = percent;

        healthBar.title =
            $"HP: {Mathf.CeilToInt(current)} / " +
            $"{Mathf.CeilToInt(maximum)}";
    }

    public void SetInfectedCount(int count)
    {
        if (infectedCounter == null)
            return;

        infectedCounter.text =
            $"Заражено: {Mathf.Max(0, count)}";
    }

    public void SetHealthpackCooldown(
        float remaining,
        float duration)
    {
        if (healthpackCooldown == null)
            return;

        if (remaining <= 0f || duration <= 0f)
        {
            healthpackCooldown.value = 100f;
            healthpackCooldown.title = "Готова";
            return;
        }

        float progress =
            1f - Mathf.Clamp01(remaining / duration);

        healthpackCooldown.value = progress * 100f;

        healthpackCooldown.title =
            $"{remaining:F1} сек.";
    }

    public void ShowHealthpack(bool visible)
    {
        if (healthpackPanel == null)
            return;

        healthpackPanel.style.display = visible
            ? DisplayStyle.Flex
            : DisplayStyle.None;
    }
}