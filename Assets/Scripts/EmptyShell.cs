using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmptyShell : MonoBehaviour
{
    private void Awake()
    {
        Destroy(gameObject, 5f);
    }
}
