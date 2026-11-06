using UnityEngine;

public class PlayerRegister : MonoBehaviour
{
    [SerializeField]
    private PlayerReferenceSO playerReference;

    private void OnEnable()
    {
        playerReference.SetPlayer(transform);
    }

    private void OnDisable()
    {
        playerReference.Clear();
    }
}