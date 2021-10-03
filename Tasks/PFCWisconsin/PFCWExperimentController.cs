using UnityEngine;
using System;
using System.Linq;
using Misc_Utilities;

/* Wisconsin Card:
 * For random trial length (3:7), the rule to be discovered will be 
 * set randomly between Color (1), Shape (2), Number (3). */

public class PFCWExperimentController : BaseExperimentController
{

    public static PFCWExperimentController custom = null;

    private void Populate(int multiplier)
    {
        System.Random randomGenerator = new System.Random();

        // Shape options, relate to TargetObject indexes (0:Cube, 1: Cross, 2: Spheres, 3: Star)
        int[] shape = { 0, 1, 2, 3 };
        // Number options
        int[] number = { 1, 2, 3, 4 };
        Color lightblue = new Color(173f / 255f, 216f / 255f, 230 / 255f);
        // Color options
        Color[] color = { Color.red, lightblue, Color.green, Color.yellow };
        
        int[] _sShape;
        int[] _sNumber;
        Color[] _sColor;

        PFCWTaskInfo myTaskInfo = gameObject.GetComponent<PFCWTaskInfo>();
        for (int i = 0; i < multiplier; i++) // multiplier loop
        {
            // rule will be set randomly from 1..3 (1:Color, 2: Shape, 3: Number)
            int rule = UnityEngine.Random.Range(1, 4);
            int target_offset = 0;
            // rule length will last a minimum of 3 to maximum of 7 trials
            int trialRuleLength = UnityEngine.Random.Range(3, 8);
            for (int _rule_length = 0; _rule_length < trialRuleLength; _rule_length++)
            {
                // Shuffled the target object options
                _sColor = color.OrderBy(x => randomGenerator.Next(0, 3)).ToArray();
                _sShape = shape.OrderBy(x => randomGenerator.Next(0, 3)).ToArray();
                _sNumber = number.OrderBy(x => randomGenerator.Next(0, 3)).ToArray();

                // Generate the Cue Card
                Array.Resize(ref _sColor, _sColor.Length + 1);
                Array.Resize(ref _sShape, _sShape.Length + 1);
                Array.Resize(ref _sNumber, _sNumber.Length + 1);
                 _sColor[_sColor.Length - 1] = color[UnityEngine.Random.Range(0, 4)];

                // Adding offset of 4 to access cue target objects which are following the 4 targets.
                _sShape[_sShape.Length - 1] = shape[UnityEngine.Random.Range(0, 4)] + 4;
                _sNumber[_sNumber.Length - 1] = number[UnityEngine.Random.Range(0, 4)];
                
                // Set the right target offset depending on active rule
                switch (rule)
                {
                    case 1:
                        Color target_color = _sColor[_sColor.Length - 1];
                        target_offset = Array.FindIndex(_sColor, tcolor => tcolor == target_color);
                        break;
                    case 2:
                        int target_shape = _sShape[_sShape.Length - 1] - 4;
                        target_offset = Array.FindIndex(_sShape, tshape => tshape == target_shape);
                        break;
                    case 3:
                        int target_index = _sNumber[_sNumber.Length - 1];
                        target_offset = Array.FindIndex(_sNumber, tnumber => tnumber == target_index);
                        break;
                    default:
                        break;
                }
                AssignTrialProperties(/*_modifiers,*/ rule, target_offset, _sColor, _sShape, _sNumber, trialRuleLength);
                Array.Clear(_sColor, 0, _sColor.Length);
                Array.Clear(_sShape, 0, _sShape.Length);
                Array.Clear(_sNumber, 0, _sNumber.Length);
            }
        }
    }

    void AssignTrialProperties(int rule, int target_offset, Color[] _color, int[] _shape, int[] _number, int ruleLength)
    {
        PFCWTaskInfo myTaskInfo = gameObject.GetComponent<PFCWTaskInfo>();
        PFCWTrialState.TargetObject[] TargetObjectList = new PFCWTrialState.TargetObject[5];
        TargetObjectList[0] = new PFCWTrialState.TargetObject(_shape[0], _color[0], _number[0]);
        TargetObjectList[1] = new PFCWTrialState.TargetObject(_shape[1], _color[1], _number[1]);
        TargetObjectList[2] = new PFCWTrialState.TargetObject(_shape[2], _color[2], _number[2]);
        TargetObjectList[3] = new PFCWTrialState.TargetObject(_shape[3], _color[3], _number[3]);
        TargetObjectList[4] = new PFCWTrialState.TargetObject(_shape[4], _color[4], _number[4]);

        var taskParams = new PFCWTrialState()
        {
            TaskName = taskInfo.taskName,
            Condition = myTaskInfo.myConditionTypes[rule-1],
            Response = myTaskInfo.myResponseTypes[target_offset],
            TargetPositionIndex = target_offset,
            TargetObjectL = TargetObjectList,
            SelectedPositionIndex = target_offset,
            TrialRule = rule,
            TrialRuleLength = ruleLength
        };

        allTrials.Add(taskParams);
    }

    public void OnEnable()
    {
        SetInstance();
        AddListeners();
        // IMPORTANT. Singleton access from BaseExperimentController calls must point to this

        base.taskInfo = GetComponent<PFCWTaskInfo>();
        base.currentTrial = new PFCWTrialState();
    }

    public override void PrepareAllTrials()
    {
        UnityEngine.Random.InitState(DateTime.Now.Millisecond);

        Populate(50); // TODO: How do we decide this?

        // Commands below can be optimized
        Debug.Log("Finished populating trials.");
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
        for (int target_index = 0; target_index < 5; target_index++)
        {
            if (currentTrial.TargetObjectL[target_index].tindex != -1)
            {
                ExperimentObject target = taskInfo.targetObjects[currentTrial.TargetObjectL[target_index].tindex].GetComponent<ExperimentObject>();
                target.IsVisible = true;
                for (int child_index = 0; child_index < target.transform.childCount; child_index++)
                {

                    if (child_index <= currentTrial.TargetObjectL[target_index].tnumber - 1)
                    {
                        target.transform.GetChild(child_index).GetComponent<Renderer>().enabled = true;
                        target.transform.GetChild(child_index).GetComponent<Renderer>().material.color = currentTrial.TargetObjectL[target_index].tcolor;
                    }
                    else
                    {
                        target.transform.GetChild(child_index).GetComponent<Renderer>().enabled = false;
                    }
                }

                target.gameObject.transform.localPosition = taskInfo.targetOffsets[target_index];

                target.IsVisible = false;
                target.PointingTo = Camera.main.transform.position;

                target.IsSkinOn = true;
            }
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
        ShowTargets();
    }

    public override void HideCues()
    {
        // Change fixation color to default
        taskInfo.fixationPoint.GetComponent<ExperimentObject>().IsVisible = false;
    }

    public override void ShowTargets()
    {
        taskInfo.fixationPoint.GetComponent<ExperimentObject>().IsVisible = false;

        // Targets should have already been placed in their relative positions during an earlier state but made invisible.
        // Here we only need to make them visible.
        for (int target_index = 0; target_index < 5; target_index++)
        {
            if (currentTrial.TargetObjectL[target_index].tindex != -1)
            {
                ExperimentObject target = taskInfo.targetObjects[currentTrial.TargetObjectL[target_index].tindex].GetComponent<ExperimentObject>();
                target.IsVisible = true;
            }
        }
    }

    public override void HideTargets()
    {
        for (int target_index = 0; target_index < 8; target_index++)
        {
            ExperimentObject target = taskInfo.targetObjects[target_index].GetComponent<ExperimentObject>();
            target.ResetColor();
            target.IsVisible = false;
        }
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
                // now check whether it is the correct target 
                if (selectedPositionIndex == currentTrial.TargetPositionIndex)
                {
                    ResponseID = 1; // Go to feedback
                    OutcomeEnum = Trial_Outcomes.GoodTrial;
                    //selectedPositionIndex = currentTrial.TargetPositionIndex;
                    currentTrial.IsCorrect = true;
                }
                else
                {
                    ResponseID = 0; // Go to feedback
                    OutcomeEnum = Trial_Outcomes.IncorrectTarget;

                    //if (currentTrial.FixPositionIndex != -1)
                    //    selectedPositionIndex = taskInfo.targetOffsets.IndexOf(
                    //            taskInfo.targetObjects[selectedTargetIndex].GetComponent<ExperimentObject>().Position -
                    //            taskInfo.fixationOffsets[currentTrial.FixPositionIndex]);
                    //else
                    //    selectedPositionIndex = taskInfo.targetOffsets.IndexOf(
                    //        taskInfo.targetObjects[selectedTargetIndex].GetComponent<ExperimentObject>().Position);
                }
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
        Debug.Log("TargetObject: " + selectedTargetIndex);
        feedbackObj = taskInfo.targetWalls[selectedTargetIndex];
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
