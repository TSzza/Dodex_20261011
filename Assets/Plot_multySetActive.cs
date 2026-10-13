using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plot_multySetActive : Plot_Base
{
    public List<GameObject> inactiveObjects;
    public List<GameObject> activeObjects;

    protected override IEnumerator PlotCoroutine()
    {
        foreach (GameObject obj in inactiveObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
        foreach (GameObject obj in activeObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
        yield return null;
    }
}
