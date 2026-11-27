using Unity.Entities;

public struct InfectionComponent : IComponentData
{
    public InfectionState State;

    // Сколько секунд осталось до конверсии в врага
    public float TimeRemaining;

    // Сколько лечения осталось до выздоровления
    public float TreatmentProgress;

    // Значение TreatmentProgress при заражении
    public float TreatmentRequired;

    // Длительность заражения до конверсии
    public float InfectionDuration;

    // Принято ударов с последнего выздоровления
    public int ReceivedHits;

    // Сколько ударов нужно для заражения
    public int HitsToInfect;

    public enum InfectionState : byte
    {
        Healthy,
        Damaged,
        Infected,
        Recovered,
        Immune
    }
}
