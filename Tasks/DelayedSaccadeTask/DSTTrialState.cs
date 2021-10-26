using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DSTTrialState : BaseTrialState
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

    public struct TargetObject
    {
        public int tindex;

        public TargetObject(int index)
        {
            tindex = index;
        }
    }

    // Target information
    [SerializeField]
    private TargetObject[] targetObjects = new TargetObject[1];
    public DSTTrialState.TargetObject[] TargetObjectL
    {
        get { return targetObjects; }
        set
        {
            targetObjects = value;
            Publish();
        }
    }
}
