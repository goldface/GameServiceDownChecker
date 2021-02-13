using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIProgressPanel : MonoBehaviour
{
    public Slider vProgressSlider;
    public Text vProgressCountText;

    private void Awake()
    {
        Debug.Assert(vProgressSlider != null);
        Debug.Assert(vProgressCountText != null);
    }

    public void SetVisible(bool aIsVisible)
    {
        gameObject.SetActive(aIsVisible);
    }

    public void SetProgress(int aCurrentCount, int aMaxCount)
    {
        _SetProgressCountText(aCurrentCount, aMaxCount);
        _SetProgressSlider(aCurrentCount, aMaxCount);
    }

    private void _SetProgressSlider(int aCurrentCount, int aMaxCount)
    {
        float lValue = 0f;
        if(aMaxCount != 0)
            lValue = (float)aCurrentCount / aMaxCount;
        vProgressSlider.value = lValue;
    }
    
    private void _SetProgressCountText(int aCurrentCount, int aMaxCount)
    {
        vProgressCountText.text = $"{aCurrentCount}/{aMaxCount}";
    }
}
