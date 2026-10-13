using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plot_cameraMovement : Plot_Base
{
    public Camera targetCamera;
    public Vector3 targetPosition = Vector3.zero;
    public float targetSize = 0;
    public float animTime = 1f;

    private bool posMove;
    private bool sizeMove;
    private Vector3 _originPos;
    private float _originOrthoSize;

    protected override IEnumerator PlotCoroutine()
    {
        if (targetCamera == null)
        {
            Debug.LogWarning($"[{name}] targetCamera为空");
            yield break;
        }
        if (targetPosition.magnitude > 0)
            posMove = true;
        if (targetSize > 0)
            sizeMove = true;

        // 记录相机状态
        _originPos = targetCamera.transform.position;
        _originOrthoSize = targetCamera.orthographicSize;

        float timer = 0f;
        while (timer < animTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / animTime);

            if (posMove)
            {
            Vector3 newPos = Vector3.Lerp(_originPos, targetPosition, t);
            targetCamera.transform.position = newPos;
            }
            if (sizeMove)
            {
            float newSize = Mathf.Lerp(_originOrthoSize, targetSize, t);
            targetCamera.orthographicSize = newSize;
            }
            yield return null;
        }

        // 强制设置最终值，消除浮点误差
            if (posMove)
        targetCamera.transform.position = targetPosition;
            if (sizeMove)
        targetCamera.orthographicSize = targetSize;
    }
}
