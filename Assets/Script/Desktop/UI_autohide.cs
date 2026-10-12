using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_autohide : MonoBehaviour, IPointerDownHandler
{
    public GameObject targetGameobiect;
    public void OnPointerDown(PointerEventData eventData)
    {
        targetGameobiect.SetActive(false);
    }
}
