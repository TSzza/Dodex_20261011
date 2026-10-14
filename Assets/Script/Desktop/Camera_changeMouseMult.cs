using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_changeMouseMult : MonoBehaviour
{
    Camera myCamera;
    public float Lastsize;
    void Start()
    {
        myCamera = GetComponent<Camera>();
        Lastsize = myCamera.orthographicSize;
        updateMouseMult();
    }
    void Update()
    {
        if (!Mathf.Approximately(myCamera.orthographicSize , Lastsize))
        {
            Lastsize = myCamera.orthographicSize;
            updateMouseMult();
        }
    }
    void updateMouseMult()
    {
        Root.Instance.SetMouseMult(Lastsize / 216f * 0.4f); // 当size为216时，mousemult为0.4
        //Debug.Log("Mult "+Root.Instance.MouseMult);
    }
}
