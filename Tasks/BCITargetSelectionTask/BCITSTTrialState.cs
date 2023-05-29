using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BCITSTTrialState : BaseTrialState
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
        public int tposition;
        public Color tcolor;

        public TargetObject(int position, Color color)
        {
            tposition = position;
            tcolor = color;
        }
    }

    // Target information
    [SerializeField]
    private TargetObject[] targetObjects = new TargetObject[1];
    public BCITSTTrialState.TargetObject[] TargetObjectL
    {
        get { return targetObjects; }
        set
        {
            targetObjects = value;
            Publish();
        }
    }
}
