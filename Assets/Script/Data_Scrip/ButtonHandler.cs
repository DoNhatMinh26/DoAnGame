using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonHandler : MonoBehaviour
{
    public void CallReset()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.Click_ResetAllGameData();
        }
    }
}
