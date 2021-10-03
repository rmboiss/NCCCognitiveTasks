using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DMTTrialState : BaseTrialState
{
    [SerializeField]
    private float stopSignalDelay;
    public float StopSignalDelay
    {
        get => stopSignalDelay;
        set
        {
            stopSignalDelay = value;
            Publish();
        }
    }

    public struct TargetColor
    {
        public Color tvalue;

        public TargetColor(Color value) 
        {
            tvalue = value;
        }
    }

    //// Target information
    [SerializeField]
    private TargetColor[] targetColors = new TargetColor[4];
    public DMTTrialState.TargetColor[] TargetColorL
    {
        get { return targetColors; }
        set
        {
            targetColors = value;
            Publish();
        }
    }
}
