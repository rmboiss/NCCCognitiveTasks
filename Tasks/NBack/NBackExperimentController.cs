using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Timers;
using System.Linq;
using UnityEngine.XR;
using Newtonsoft.Json;
using PupilLabs;
using Misc_Utilities;

/* NBack Memory Task */

public class NBackExperimentController : BaseExperimentController
{
    protected static int NBackLength = 2; // Number between 1 and 4
    protected static int NOptions = 4; // Number between 2 and 9

    public static NBackExperimentController custom = null;

    private void Populate(int multiplier)
    {
        System.Random randomGenerator = new System.Random();

        // Starting the initial n-back list of number
        int[] number_to_memorize = new int[NBackLength];
        for (int count=0; count < NBackLength; count++)
        {
            number_to_memorize[count] = UnityEngine.Random.Range(0, NOptions);
        }

        int new_number = 0;
        int target_offset = 0;

        for (int i = 0; i < multiplier; i++) // multiplier loop
        {
            new_number = UnityEngine.Random.Range(0, NOptions);

            if (new_number == number_to_memorize[0])
            {
                target_offset = 1;
            }
            else
            {
                target_offset = 0;
            }
            AssignTrialProperties(/*_modifiers,*/ new_number, target_offset);
            for (int count = 0; count < NBackLength-1; count++)
            {
                number_to_memorize[count] = number_to_memorize[count+1];
            }
            number_to_memorize[NBackLength-1] = new_number;
        }
    }

    void AssignTrialProperties(int new_number, int target_offset)
    {
        NBackTaskInfo myTaskInfo = gameObject.GetComponent<NBackTaskInfo>();
        NBackTrialState.TargetObject[] TargetObjectList = new NBackTrialState.TargetObject[1];
        TargetObjectList[0] = new NBackTrialState.TargetObject(new_number);

        var taskParams = new NBackTrialState()
        {
            TaskName = taskInfo.taskName,
            //Condition = myTaskInfo.myConditionTypes[2], // target_offset is 2 for number to be memorized
            //Response = myTaskInfo.myResponseTypes[0],
            TargetPositionIndex = target_offset,
            TargetObjectL = TargetObjectList,
            TargetObjectIndex = new_number,
            SelectedPositionIndex = target_offset
        };

        allTrials.Add(taskParams);
    }

  
    public void OnEnable()
    {
        SetInstance();
        AddListeners();
        // IMPORTANT. Singleton access from BaseExperimentController calls must point to this

        base.taskInfo = GetComponent<NBackTaskInfo>();
        base.currentTrial = new NBackTrialState();

        for (int target_index = 0; target_index <= 10; target_index++)
        {
            ExperimentObject target = taskInfo.targetObjects[target_index].GetComponent<ExperimentObject>();
            target.transform.GetChild(0).GetComponent<Renderer>().enabled = false;
        }
    }

    public override void PrepareAllTrials()
    {
        UnityEngine.Random.InitState(DateTime.Now.Millisecond);

        Populate(50); // TODO: How do we decide this?

    }

    // This script prepares the trial during the intertrial phase
    public override void PrepareTrial()
    {
        ResetObjects();

        currentTrial.quiet = true;
        if (allTrials.Count != 0)
        {
            currentTrial = allTrials[0];
            allTrials.RemoveAt(0);
        }
        if (allTrials.Count == 0)
            PrepareAllTrials();

        currentTrial.TrialIndex = currentTrialIndex;
        currentTrialIndex++;
        currentTrial.IsCorrect = false;
        currentTrial.quiet = false;

        PrepareTargets();
    }

    public override void PrepareTargets()
    {

        ExperimentObject target = taskInfo.targetObjects[currentTrial.TargetObjectL[0].tindex].GetComponent<ExperimentObject>();
        target.transform.GetChild(0).GetComponent<Renderer>().enabled = true;
        target.gameObject.transform.localPosition = taskInfo.targetOffsets[0];
        target.IsVisible = false;
        target.PointingTo = Camera.main.transform.position;

    }

    public void SetFixationVisibility(bool isVisible)
    {
        ExperimentObject fixPoint = taskInfo.fixationPoint.GetComponent<ExperimentObject>();

        fixPoint.IsVisible = isVisible;
        if (Camera.main != null) fixPoint.PointingTo = Camera.main.transform.position;
    }

    public override void ResetObjects()
    {
        taskInfo.fixationPoint.transform.parent.gameObject.SetActive(true);
        // Reset fixation point.
        SetFixationVisibility(true);

        HideTargets();
    }

    public override void ShowCues()
    {
        taskInfo.fixationPoint.GetComponent<ExperimentObject>().IsVisible = false;

        // Targets should have already been placed in their relative positions during an earlier state but made invisible.
        // Here we only need to make them visible.
        ExperimentObject target = taskInfo.targetObjects[currentTrial.TargetObjectL[0].tindex].GetComponent<ExperimentObject>();
        target.IsVisible = true;
    }

    public override void HideCues()
    {
        // Change fixation color to default
        taskInfo.fixationPoint.GetComponent<ExperimentObject>().IsVisible = false;

        ExperimentObject target = taskInfo.targetObjects[currentTrial.TargetObjectL[0].tindex].GetComponent<ExperimentObject>();
        target.IsVisible = false;
    }

    public override void ShowTargets()
    {
        
    }

    public override void HideTargets()
    {
        
    }

    public override void ShowImperative()
    {
        // Fixation point disappears cues the subject to initiate a saccade
        taskInfo.fixationPoint.GetComponent<ExperimentObject>().IsVisible = false;
        ShowTargets();
    }

    override public void EndResponse ()
    {
        if (currentTrial.Response != Misc_Utilities.ResponseTypes.Hold) return;
        taskInfo.fixationPoint.GetComponent<ExperimentObject>().ResetColor();
        taskInfo.fixationPoint.GetComponent<ExperimentObject>().IsVisible = false;

        currentTrial.SelectedPositionIndex = selectedPositionIndex;
        currentTrial.SelectedObjectIndex = selectedTargetIndex;
    }

    public override int CheckResponse()
    {
        int ResponseID = -1; // Stay in Response phase
        selectedPositionIndex = selectedTargetIndex;
        if ( currentTrialIndex > NBackLength )
        {
            // Targets should have already been placed in their relative positions during an earlier state but made invisible.
            // Here we only need to make them visible.
            for (int target_index = 0; target_index <= 10; target_index++)
            {
                ExperimentObject target = taskInfo.targetObjects[target_index].GetComponent<ExperimentObject>();
                if (target_index >= 9)
                {
                    target.transform.GetChild(0).GetComponent<Renderer>().enabled = true;
                }
                SphereCollider sc = target.GetComponent<SphereCollider>();
                if (sc)
                {
                    sc.radius = 0.0f;
                }
            }
        }

        if (currentTrialIndex <= NBackLength)
        {
            OutcomeEnum = Trial_Outcomes.Skipped;
            ResponseID = 1; // Go to feedback
        }
        else if (selectedObject == SelectedObjectClass.Fixation)
        {
            OutcomeEnum = Trial_Outcomes.Ignored;
            targetTimer = Mathf.Infinity;
        }
        else if (selectedObject == SelectedObjectClass.Wall)
        {
            if (targetTimer == Mathf.Infinity) // just started looking at target
            {
                targetTimer = Time.time;
                // set to late response in case the state ends before target timer 
                // is done. 
                OutcomeEnum = Trial_Outcomes.LateResponse;
                Publish("{\"Response hold starting - selected position\": " + JsonConvert.SerializeObject(selectedPositionIndex, Formatting.None) + "}");
            }
            else if ((Time.time - targetTimer) > taskInfo.targetHold)
            {
                // now check whether it is the correct target 
                if (selectedPositionIndex == currentTrial.TargetPositionIndex)
                {
                    ResponseID = 1; // Go to feedback
                    OutcomeEnum = Trial_Outcomes.GoodTrial;
                    selectedPositionIndex = currentTrial.TargetPositionIndex;
                    currentTrial.IsCorrect = true;
                }
                else
                {
                    ResponseID = 0; // Go to feedback
                    OutcomeEnum = Trial_Outcomes.IncorrectTarget;
                }
                Publish("{\"Response hold end - selected position\": " + JsonConvert.SerializeObject(selectedPositionIndex, Formatting.None) + "}");
            }
        }
        else if (selectedObject == SelectedObjectClass.Background) // will be selecting background when fix point disappear and moving toward target
        {
            OutcomeEnum = Trial_Outcomes.NoResponse;
            targetTimer = Mathf.Infinity;
        }
        else
        {
            ResponseID = 1; // Feedback
            OutcomeEnum = Trial_Outcomes.InvalidResponse;
        }
        //currentTrial.SelectedPositionIndex = selectedPositionIndex;
        //currentTrial.SelectedObjectIndex = selectedTargetIndex;

        return ResponseID;

    }

    public override void GiveFeedback()
    {
        feedbackObj = taskInfo.targetWalls[selectedTargetIndex];
        if (feedbackObj && currentTrialIndex > NBackLength)
        {
            var feedbackTypes = feedbackObj.GetComponents(typeof(FeedbackModality));
            // Give all relevant feedbacks
            foreach (var component in feedbackTypes)
            {
                var feedback = (FeedbackModality)component;
                try
                {
                    feedback.GiveFeedback(currentTrial.IsCorrect, currentTrial.ReactionTime);
                }
                catch (Exception ex)
                {
                    Debug.Log("Progressing without particles" + ex.GetType());
                }
            }
        }
    }

    public override void CursorSelect(GameObject go)
    {
        if (go == goCache)
        {
            return;
        }
        goCache = go;

        // 'cursors' with colliderScript on them will call this function when they collide with a game object.
        // Debug.Log("You are currently selecting " + go.name);
        if (go != taskInfo.fixationPoint)
        {
            // TODO: Don't reset timer for momentary gaze glitches off fixation point
            // If using gaze tracking
            //     if other selected object is background, then start an 'away' timer. 
            // If 'away' from fp for more than some threshold, set gateTimer to infinity.
            if (awayTimer == Mathf.Infinity && fixationTimer != Mathf.Infinity) // Was fixating
            {
                awayTimer = Time.time;
            }
            else if (awayTimer != Mathf.Infinity && fixationTimer != Mathf.Infinity) // Away timing
            {
                if ((Time.time - awayTimer) > taskInfo.awayTolerance) // tolerance elapsed
                {
                    fixationTimer = Mathf.Infinity;
                    awayTimer = Mathf.Infinity;
                }
            }
            else // fixationTimer = Infinity == not fixating
            {
                fixationTimer = Mathf.Infinity;
                awayTimer = Mathf.Infinity;
            }
        }

        if (go == taskInfo.fixationPoint && taskInfo.fixationPoint != null)
        {
            awayTimer = Mathf.Infinity;

            if (fixationTimer == Mathf.Infinity) // Begin fixation
            {
                fixationTimer = Time.time;
            }
            selectedObject = SelectedObjectClass.Fixation;
        }
        else if ((go == taskInfo.colorCue && taskInfo.colorCue != null) ||
            (go == taskInfo.directionCue && taskInfo.directionCue != null))
        {
            // TODO: Also if go is one of the target objects but during the cue phase or in the cue position... how to check?
            selectedObject = SelectedObjectClass.Cue;
        }
        else if (taskInfo.targetObjects.Contains(go))
        {
            //UpdateReactionTime();

            selectedObject = SelectedObjectClass.Target;
            selectedTargetIndex = taskInfo.targetObjects.IndexOf(go);
        }
        else if (taskInfo.targetWalls.Contains(go))
        {
            selectedObject = SelectedObjectClass.Wall;
            selectedTargetIndex = taskInfo.targetWalls.IndexOf(go);
            selectedPositionIndex = taskInfo.targetWalls.IndexOf(go);
        }
        else if (go == taskInfo.backgroundObject)
        {
            selectedObject = SelectedObjectClass.Background;
        }
        else if (go == null)
        {
            // assing background object.
            selectedObject = SelectedObjectClass.Background;
            go = taskInfo.backgroundObject;
        }
        //CursorMarker cursorMarkerInfo = new CursorMarker
        //{
        //    trialIndex = currentTrial.TrialIndex,
        //    selectedObjectClass = selectedObject.ToString(),
        //    info = "Selected: " + go.name,
        //};
        //Publish("{\"Input\": " + JsonUtility.ToJson(cursorMarkerInfo) + "}");
    }
}
