using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerReference",
    menuName = "Game/References/Player Reference"
)]
public class PlayerReferenceSO : ScriptableObject
{
    public Transform Player { get; private set; }

    public void SetPlayer(Transform player)
    {
        Player = player;
    }

    public void Clear()
    {
        Player = null;
    }
}