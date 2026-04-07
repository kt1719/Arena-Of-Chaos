using System.Collections;
using Fusion;
using UnityEngine;

public class EnemyAI : NetworkBehaviour
{
    [SerializeField] private float roamChangeDirFloat = 2f;
    private enum State
    {
        Roaming,
    }

    private State state;
    private EnemyPathfinding enemyPathfinding;
    private Coroutine roamingCoroutine;

    private void Awake()
    {
        enemyPathfinding = GetComponent<EnemyPathfinding>();
        
        state = State.Roaming;
    }

    private void Start()
    {
        roamingCoroutine = StartCoroutine(RoamingRoutine());
    }

    private void OnDestroy()
    {
        StopCoroutine(roamingCoroutine);
    }

    private IEnumerator RoamingRoutine()
    {
        while (state == State.Roaming)
        {
            Vector2 roamPosition = GetRoamingPosition();
            enemyPathfinding.SetRoamPosition(roamPosition);
            yield return new WaitForSeconds(roamChangeDirFloat);
        }
    }

    private Vector2 GetRoamingPosition()
    {
        return new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
    }
}
