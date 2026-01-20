using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Trial state for sentence trials
public class SentenceTrialState : BaseTrialState
{
    [SerializeField]
    private string sentence = "yes";
    public string Sentence
    {
        get => sentence;
        set
        {
            sentence = value;
            Publish();
        }
    }

    [SerializeField]
    private bool modeImagined = true;
    public bool ModeImagined
    {
        get => modeImagined;
        set
        {
            modeImagined = value;
            Publish();
        }
    }

    [SerializeField]
    private float reactionTime = 0f;
    public float ReactionTime
    {
        get => reactionTime;
        set
        {
            reactionTime = value;
            Publish();
        }
    }
}