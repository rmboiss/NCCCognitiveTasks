using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class VSATrialState : BaseTrialState
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

    [SerializeField]
    private float rotationDelayTime;
    public float RotationDelayTime
    {
        get { return rotationDelayTime; }
        set
        {
            rotationDelayTime = value;
        }
    }

    [SerializeField]
    private float rotationAngle;
    public float RotationAngle
    {
        get { return rotationAngle; }
        set
        {
            rotationAngle = value;
        }
    }

    [SerializeField]
    private int responseType;
    public int ResponseType
    {
        get { return responseType; }
        set
        {
            responseType = value;
        }
    }

    // Target information
    [SerializeField]
    private TargetObject[] targetObjects = new TargetObject[1];
    public VSATrialState.TargetObject[] TargetObjectL
    {
        get { return targetObjects; }
        set
        {
            targetObjects = value;
            Publish();
        }
    }

}
