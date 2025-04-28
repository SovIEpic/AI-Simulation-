using UnityEngine;
using System.Collections.Generic;

public class testUnit : MonoBehaviour
{

    void Start()
    {
        UnitSelectionManager.Instance.allUnitsList.Add(gameObject);

    }

    private void OnDestroy()
    {
        UnitSelectionManager.Instance.allUnitsList.Remove(gameObject);
    }

}
