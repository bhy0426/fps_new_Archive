using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage; // ÃÑ¾Ë ´ë¹ÌÁö
    public float bulletSpeed; // ÃÑ¾Ë ¼Óµµ

    private void Awake()
    {
        Destroy(gameObject, 5f);
    }
}
