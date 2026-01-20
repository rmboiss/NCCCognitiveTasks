using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Misc_Utilities;

// Simple TaskInfo container for Sentence UI elements

public class SentenceTaskInfo : BaseTaskInfo
{
    [Header("UI Elements")]
    public GameObject sentenceCueCanvas;  // parent GameObject containing cue text
    public Text sentenceCueText;          // UI Text showing sentence
    public Text decodedSentenceCueText;   // UI Text showing sentence
    public GameObject imagineBorder;      // visual border displayed during imagine window
    public Text feedbackText;             // simple feedback text

    [Header("Trial config (optional)")]
    public float cueDuration = 1.5f;
    public float interTrialInterval = 1.0f;
}
