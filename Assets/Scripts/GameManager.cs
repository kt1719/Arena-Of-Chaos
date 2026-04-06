using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private Transform defaultPlayerSpawnPoint;
    public Transform DefaultPlayerSpawnPoint => defaultPlayerSpawnPoint;

    private void Awake()
    {
        Instance = this;
        if (defaultPlayerSpawnPoint == null)
            Debug.LogWarning("[GameManager] defaultPlayerSpawnPoint is not assigned.");
    }
}
