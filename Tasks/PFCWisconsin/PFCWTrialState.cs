using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PFCWTrialState : BaseTrialState
{
    [SerializeField]
    private float stopSignalDelay;
    public float StopSignalDelay {
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
        public Color tcolor;
        public int tnumber;

        public TargetObject(int index, Color color, int number)
        {
            tindex = index;
            tcolor = color;
            tnumber = number;
        }
    }

    // Rule Information
    [SerializeField]
    private int trialRule = 0;
    public int TrialRule
    {

        get { return trialRule; }
        set
        {
            trialRule = value;
            Publish();
        }
    }

    [SerializeField]
    private int trialRuleLength = 0;
    public int TrialRuleLength
    {

        get { return trialRuleLength; }
        set
        {
            trialRuleLength = value;
            Publish();
        }
    }

    // Target information
    [SerializeField]
    private TargetObject[] targetObjects = new TargetObject[5];
    public PFCWTrialState.TargetObject[] TargetObjectL
    {
        get { return targetObjects; }
        set
        {
            targetObjects = value;
            Publish();
        }
    }

}
