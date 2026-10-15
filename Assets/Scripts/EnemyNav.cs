using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyNav : MonoBehaviour
{
    Enemy enemy;
    EnemyAttack enemyAttack;
    public NavMeshAgent agent;
    public Transform[] waypoints; // 이동할 지점들을 담는 배열
    private int currentWaypointIndex = 0;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        enemyAttack = GetComponent<EnemyAttack>();
        agent = GetComponent<NavMeshAgent>();

        agent.updateRotation = false;

        if (waypoints.Length > 0)
        {
            SetDestinationToWaypoint();
        }
    }

    void Update()
    {
        // agent.SetDestination(enemy.target.position);

        // 1. 플레이어를 향한 방향 벡터 계산
        Vector3 direction = (enemy.target.position - transform.position).normalized;

        // 2. Y축(좌우)으로만 회전하도록 설정 (공중에 떠 있는 플레이어를 봐도 몸이 눕지 않게)
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            // 3. 목표 회전값 계산
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // 4. Slerp를 이용해 현재 회전에서 목표 회전까지 부드럽게 회전
            // 뒤의 숫자(5f)가 높을수록 회전 속도가 빨라집니다.
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        // 목적지에 거의 도착했는지 확인 (남은 거리가 0.5m 미만일 때)
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            if(!enemy.isAttack)
            {
                GoToNextWaypoint();
                Debug.Log("다음으로!");
            }
        }

        if(enemy.isAttack)
        {
            agent.SetDestination(enemy.target.position);
            Debug.Log("적 발견!!");
        }
    }

    void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;

        // 다음 지점 인덱스 계산 (마지막 지점이면 다시 0번으로)
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;

        SetDestinationToWaypoint();
    }

    void SetDestinationToWaypoint()
    {
        agent.SetDestination(waypoints[currentWaypointIndex].position);
    }
}