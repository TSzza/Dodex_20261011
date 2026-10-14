using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game_autohide : MonoBehaviour
{
    void Awake()
    {
        gameObject.SetActive(false);
    }
}
