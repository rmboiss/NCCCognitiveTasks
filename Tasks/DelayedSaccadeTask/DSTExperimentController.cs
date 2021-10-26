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

/* Decision Making Task */

public class DSTExperimentController : BaseExperimentController
{
    public static DSTExperimentController custom = null;

    private void Populate(int multiplier)
    {
        System.Random randomGenerator = new System.Random();

        int target_offset = 0;
        int cue_offset = 0;

        for (int i = 0; i < multiplier; i++) // multiplier loop
        {
            target_offset = UnityEngine.Random.Range(0, 4);
            cue_offset = target_offset;

            AssignTrialProperties(/*_modifiers,*/ target_offset, cue_offset);
        }
    }

    void AssignTrialProperties(int target_offset, int cue_offset)
    {
        DSTTaskInfo myTaskInfo = gameObject.GetComponent<DSTTaskInfo>();
        DSTTrialState.TargetObject[] TargetObjectList = new DSTTrialState.TargetObject[1];
        TargetObjectList[0] = new DSTTrialState.TargetObject(target_offset);

        var taskParams = new DSTTrialState()
        {
            TaskName = taskInfo.taskName,
            TargetPositionIndex = target_offset,
            CuePositionIndex = cue_offset,
            TargetObjectL = TargetObjectList,
            TargetObjectIndex = target_offset,
            SelectedPositionIndex = target_offset
        };

        allTrials.Add(taskParams);
    }

  
    public void OnEnable()
    {
        SetInstance();
        AddListeners();
        // IMPORTANT. Singleton access from BaseExperimentController calls must point to this

        base.taskInfo = GetComponent<DSTTaskInfo>();
        base.currentTrial = new DSTTrialState();
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

        for (int target_index = 0; target_index < 4; target_index++)
        {
            ExperimentObject target = taskInfo.targetObjects[target_index].GetComponent<ExperimentObject>();
            target.IsVisible = false;
        }

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
        ExperimentObject cue = taskInfo.targetObjects[currentTrial.CuePositionIndex].GetComponent<ExperimentObject>();
        cue.transform.GetChild(0).GetComponent<Renderer>().enabled = true;
        cue.IsVisible = true;
 
    }

    public override void HideCues()
    {

        ExperimentObject cue = taskInfo.targetObjects[currentTrial.CuePositionIndex].GetComponent<ExperimentObject>();
        cue.IsVisible = false;

    }

    public override void ShowTargets()
    {

    }

    public override void HideTargets()
    {

    }

    public override void ShowImperative()
    {

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

        if (selectedObject == SelectedObjectClass.Fixation)
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
            }
            else if ((Time.time - targetTimer) > taskInfo.targetHold)
            {
                if (selectedPositionIndex == currentTrial.TargetPositionIndex)
                {
                    ResponseID = 1; // Go to feedback
                    OutcomeEnum = Trial_Outcomes.GoodTrial;
                    currentTrial.IsCorrect = true;
                }
                else
                {
                    ResponseID = 0; // Go to feedback
                    OutcomeEnum = Trial_Outcomes.IncorrectTarget;
                    selectedPositionIndex = currentTrial.CuePositionIndex;
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

        return ResponseID;
    }

    public override void GiveFeedback()
    {
        feedbackObj = taskInfo.targetWalls[currentTrial.SelectedPositionIndex];
        if (feedbackObj)
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
            UpdateReactionTime();

            selectedObject = SelectedObjectClass.Target;
            selectedTargetIndex = taskInfo.targetObjects.IndexOf(go);
        }
        else if (taskInfo.antisaccadeObjects.Contains(go))
        {
            UpdateReactionTime();

            selectedObject = SelectedObjectClass.Wall;
            selectedPositionIndex = taskInfo.antisaccadeObjects.IndexOf(go);
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
