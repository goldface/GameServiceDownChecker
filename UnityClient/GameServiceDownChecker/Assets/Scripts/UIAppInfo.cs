using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIAppInfo : MonoBehaviour
{
    public Text vAppNameText;

    void Awake()
    {
        Debug.Assert(vAppNameText != null);
    }

    public void SetAppNameText(string aAppName)
    {
        vAppNameText.text = aAppName;
    }
}
