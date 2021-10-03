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

/* VSA Memory Task */

public class VSAExperimentController : BaseExperimentController
{
    protected float responseTimer = Mathf.Infinity;
    protected float rotateDelayTimer = Mathf.Infinity;
    protected float flipBackDelayTimer = Mathf.Infinity;
    protected float responseTimeout = 3f;
    protected float flipBackDelay = 0.1f;
    protected float flipBackDelayAdjustment = 0.01f;
    protected int runningSuccessCount = 0;

    protected bool rotateTarget = false;

    public static VSAExperimentController custom = null;

    private void Populate(int multiplier)
    {
        // Trial Propreties
        // 1) Spatial attention cue location
        // 2) NoDistractor, distractor (response has to timeout, "ignore" response is GoodTrial), or both at same time
        // 3) Rotation angle
        // 4) Delay before rotation
        // 5) Max response time allowed

        System.Random randomGenerator = new System.Random();

        int trial_type = 0; // go (0), nogo (1), goWithDistractor (2)
        int cue_offset = 0;
        int target_offset = 0;
        int rotation_angle = 40;
        int delay_before_rotation = 2;
        //int max_response_time = 1;


        for (int i = 0; i < multiplier; i++) // multiplier loop
        {
            trial_type = UnityEngine.Random.Range(0, 2);// 0;  //0: Pro  -  1: Hold (do not saccade)
            target_offset = UnityEngine.Random.Range(0, 4);
            cue_offset = target_offset;
            if ( trial_type == 1)
            {
                while (cue_offset == target_offset)
                {
                    cue_offset = UnityEngine.Random.Range(0, 4);
                }
            }
            rotation_angle = UnityEngine.Random.Range(15, 70);
            delay_before_rotation = UnityEngine.Random.Range(1, 7);
            AssignTrialProperties(target_offset, cue_offset, trial_type, rotation_angle, delay_before_rotation);
        }
    }

    void AssignTrialProperties(int target_offset, int cue_offset, int trial_type, int angle, int delay)
    {
        VSATaskInfo myTaskInfo = gameObject.GetComponent<VSATaskInfo>();
        VSATrialState.TargetObject[] TargetObjectList = new VSATrialState.TargetObject[1];
        TargetObjectList[0] = new VSATrialState.TargetObject(target_offset);

        var taskParams = new VSATrialState()
        {
            TaskName = taskInfo.taskName,
            Condition = myTaskInfo.myConditionTypes[2], 
            Response = myTaskInfo.myResponseTypes[trial_type], //0: Pro  -  1: Hold (do not saccade)
            ResponseType = trial_type, //0: Pro  -  1: Hold (do not saccade)
            TargetPositionIndex = target_offset,
            CuePositionIndex = cue_offset,
            TargetObjectL = TargetObjectList,
            TargetObjectIndex = target_offset,
            RotationDelayTime = delay,
            RotationAngle = angle,
            SelectedPositionIndex = target_offset
        };

        allTrials.Add(taskParams);
    }

  
    public void OnEnable()
    {
        SetInstance();
        AddListeners();
        // IMPORTANT. Singleton access from BaseExperimentController calls must point to this

        base.taskInfo = GetComponent<VSATaskInfo>();
        base.currentTrial = new VSATrialState();
    }

    public override void PrepareAllTrials()
    {
        UnityEngine.Random.InitState(DateTime.Now.Millisecond);

        Populate(50); // TODO: How do we decide this?

    }

    // This script prepares the trial during the intertrial phase
    public override void PrepareTrial()
    {
        responseTimer = Mathf.Infinity;
        rotateDelayTimer = Mathf.Infinity;
        rotateTarget = false;
        
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
            target.transform.GetChild(0).localRotation = target.transform.parent.rotation; 
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
        ExperimentObject cue = taskInfo.targetObjects[4].GetComponent<ExperimentObject>();
        cue.IsVisible = true;
        //cue.transform.GetChild(0).GetComponent<Renderer>().enabled = true;

        cue.gameObject.transform.localPosition = taskInfo.cueOffsets[currentTrial.CuePositionIndex];
        cue.PointingTo = Camera.main.transform.position;

        cue.IsSkinOn = true;
    }

    public override void HideCues()
    {
        // Change fixation color to default
        taskInfo.fixationPoint.GetComponent<ExperimentObject>().IsVisible = false;
        ExperimentObject cue = taskInfo.targetObjects[4].GetComponent<ExperimentObject>();
        cue.IsVisible = false;

        ShowTargets();
    }

    public override void ShowTargets()
    {

        // Targets should have already been placed in their relative positions during an earlier state but made invisible.
        // Here we only need to make them visible.
        for (int target_index = 0; target_index < 4; target_index++)
        {
            ExperimentObject target = taskInfo.targetObjects[target_index].GetComponent<ExperimentObject>();
            target.transform.GetChild(0).GetComponent<Renderer>().enabled = true;
            target.IsVisible = true;
        }
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
    }

    public override int CheckResponse()
    {
        int ResponseID = -1; // Stay in Response phase

        selectedPositionIndex = selectedTargetIndex;

        for (int target_index = 0; target_index <=4 ; target_index++)
        {
            ExperimentObject target = taskInfo.targetObjects[target_index].GetComponent<ExperimentObject>();
            target.transform.GetChild(0).GetComponent<Renderer>().enabled = true;
            SphereCollider sc = target.GetComponent<SphereCollider>();
            if (sc)
            {
                sc.radius = 0.0f;
            }
        }

        if (rotateDelayTimer == Mathf.Infinity)
        {
            rotateDelayTimer = Time.time;
        }
        //apply rotation of target, start response timer
        else if (((Time.time - rotateDelayTimer) > currentTrial.RotationDelayTime) && (rotateTarget == false)) 
        {
            ExperimentObject target = taskInfo.targetObjects[currentTrial.TargetPositionIndex].GetComponent<ExperimentObject>();
            rotateTarget = true;
            target.transform.GetChild(0).Rotate(0, 0, currentTrial.RotationAngle);
            responseTimer = Time.time;
            flipBackDelayTimer = Time.time;
            // Reset targetTimer has the situation has changed
            targetTimer = Time.time;
            Publish("{\"Rotate target\": " + JsonConvert.SerializeObject(currentTrial.TargetPositionIndex, Formatting.None) + "}");
        }

        if ((Time.time - responseTimer) > responseTimeout)
        {
            if (currentTrial.ResponseType == 1)
            {
                ResponseID = 1; // Feedback
                OutcomeEnum = Trial_Outcomes.GoodTrial;
                selectedPositionIndex = currentTrial.CuePositionIndex;
                currentTrial.IsCorrect = true;
            }
            else
            {
                ResponseID = 1; // Feedback
                OutcomeEnum = Trial_Outcomes.LateResponse;
                // to provide feedback on the correct target,
                selectedTargetIndex = currentTrial.CuePositionIndex;
            }
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
                if ((selectedPositionIndex == currentTrial.TargetPositionIndex) && (currentTrial.ResponseType == 0) && (rotateTarget == true))
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
                currentTrial.ReactionTime = (responseTimer - targetTimer);
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

        // Reset the rotated target
        if (((Time.time - flipBackDelayTimer) > flipBackDelay) && rotateTarget == true )
        {
            ExperimentObject target = taskInfo.targetObjects[currentTrial.TargetPositionIndex].GetComponent<ExperimentObject>();
            target.transform.GetChild(0).Rotate(0, 0, -currentTrial.RotationAngle);
            //target.transform.GetChild(0).GetComponent<Renderer>().enabled = false;
            flipBackDelayTimer = Mathf.Infinity;
            Publish("{\"Reset target\": " + JsonConvert.SerializeObject(currentTrial.TargetPositionIndex, Formatting.None) + "}");
        }

        return ResponseID;

    }

    public override void GiveFeedback()
    {
        if (currentTrial.IsCorrect == true)
        {
            runningSuccessCount = runningSuccessCount + 1;
            if (runningSuccessCount >= 3)
            {
                runningSuccessCount = 0;
                flipBackDelay = flipBackDelay - flipBackDelayAdjustment;
            }
        }
        else
        {
            runningSuccessCount = 0;
            flipBackDelay = flipBackDelay + flipBackDelayAdjustment;
        }
        Publish("{\"Give Feedback - flipBackDelay\": " + JsonConvert.SerializeObject(flipBackDelay, Formatting.None) + "}");
        
        feedbackObj = taskInfo.targetWalls[currentTrial.CuePositionIndex];
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
