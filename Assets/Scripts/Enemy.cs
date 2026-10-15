using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Enemy : MonoBehaviour
{
    public TextMeshPro hpText;
    public Transform target;
    EnemyNav enemyNav;
    MeshRenderer meshRenderer;

    public int hp;
    public int maxHp;
    public int walkSpeed;
    public int sprintSpeed;

    public bool isAttack = false;

    enum EnemyPosture
    {
        idle,
        walk,
        sprint
    }

    [SerializeField]
    EnemyPosture enemyPosture; // 현재 적의 자세 상태

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        enemyNav = GetComponent<EnemyNav>();
        hp = maxHp;
    }

    void Update()
    {
        UpdateMovement();

        transform.LookAt(target);

        hpText.text = hp.ToString();
    }

    public void Hit(int damage)
    {
        hp -= damage;
        hpText.color = Color.red;
        isAttack = true;
        Invoke("ChangeOriginColor", 0.2f);
        if (hp <= 0)
        {
            Destroy(gameObject);
            return;
        }
    }

    void ChangeOriginColor()
    {
        hpText.color = Color.white;
    }

    void UpdateMovement()
    {
        switch(enemyPosture)
        {
            case EnemyPosture.idle:
                enemyNav.agent.speed = 0;
                break;

            case EnemyPosture.walk:
                enemyNav.agent.speed = walkSpeed;
                break;

            case EnemyPosture.sprint:
                enemyNav.agent.speed = sprintSpeed;
                break;
        }
    }
}
