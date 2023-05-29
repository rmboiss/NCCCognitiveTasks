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
using System.Runtime.InteropServices;
using LSL;
using UnityEngine.Serialization;

/* Cursor Control Task */

public class BCITSTExperimentController : BaseExperimentController
{
    public static BCITSTExperimentController custom = null;

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out MousePosition lpMousePosition);

    public enum NumberOfTargetsOptions
    {
        Option1 = 1,
        Option2 = 2,
        Option3 = 3,
        Option4 = 4
    };
    [FormerlySerializedAs("numberOfTargetsOptions")]
    [SerializeField]
    private NumberOfTargetsOptions m_NumberOfTargetsOptions = NumberOfTargetsOptions.Option1;
    public NumberOfTargetsOptions NumberTargets { get { return m_NumberOfTargetsOptions; } set { m_NumberOfTargetsOptions = value; } }

    public enum NumberOfTargetsPositions
    {
        Option2 = 2,
        Option4 = 4,
        Option16 = 16
    };
    [FormerlySerializedAs("numberOfTargetsPositions")]
    [SerializeField]
    private NumberOfTargetsPositions m_NumberOfTargetsPositions = NumberOfTargetsPositions.Option2;
    public NumberOfTargetsPositions NumberTargetPositions { get { return m_NumberOfTargetsPositions; } set { m_NumberOfTargetsPositions = value; } }

    private int numberOfTargets = 1; //{ 1, 2, 3, 4 };
    private int numberOfTargetPositions = 3; //{ 2, 4, 16 };
    public bool testRestCondition = false;
	public int decodingMode = 0;
    public float cueObjectScaleFactor = 2;
    private int targetIndex = 0;
    private readonly int maxNumberOfTargets = 4;
    public float cursorSpeed = 10.0f;
    private float timeInterval = 0.0f;
    private float trajectoryTime = 0.0f;
    public float updateEveryCount = 4.0f;
    private readonly float unityUpdateRate = 60.0f;
    private int updateCount = 1;
    public int rule = 0; //0:NoRule 1:ColorSeq1 2:ColorSeq2 3:SmallShape 4:Number

    private bool checkResponseEnter = true;
    private bool restPosition = false;
    private float targetHoldOriginal = 0.25f;
	public float userVelocityFactor = 7.0f;
	private float xdmp;
    private float ydmp;
    private float offsetX;
    private float offsetY;
    private float cursorToCueX;
    private float cursorToCueY;
	private double lastTimestamp = 0.0f;
    private float cueX;
    private float cueY;
    MousePosition mp;
    
    [StructLayout(LayoutKind.Sequential)]
    private struct MousePosition
    {
        public int x;
        public int y;
    }

    public static int[] ShuffleMe(int[] array)
    {
        int arrayLength = array.Length;
        for (int i = 0; i < arrayLength - 1; i++)
        {
            // Generate a random index between i and the end of the array
            int randomIndex = UnityEngine.Random.Range(i, arrayLength);

            // Swap the elements at indices i and randomIndex
            int temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }

        return array;
    }

    private void InitTaskSettings()
    {
        targetHoldOriginal = taskInfo.targetHold;
        if (NumberTargets == NumberOfTargetsOptions.Option2)
        {
            numberOfTargets = 2;
        }
        else if (NumberTargets == NumberOfTargetsOptions.Option3)
        {
            numberOfTargets = 3;
        }
        else if (NumberTargets == NumberOfTargetsOptions.Option4)
        {
            numberOfTargets = 4;
        }
        else
        {
            numberOfTargets = 1;
        }

        if (NumberTargetPositions == NumberOfTargetsPositions.Option4)
        {
            numberOfTargetPositions = 4;
        }
        else if (NumberTargetPositions == NumberOfTargetsPositions.Option16)
        {
            numberOfTargetPositions = 16;
        }
        else
        {
            numberOfTargetPositions = 2;
        }

        if (numberOfTargets == 1 && testRestCondition == true)
        {
            numberOfTargetPositions += 1;
        }
    }

    private void Populate(int multiplier)
    {
        InitTaskSettings();

        System.Random randomGenerator = new System.Random();

        int[] TargetPositionList = new int[numberOfTargets];
        int[] TargetColorList = new int[numberOfTargets];
        int[] TargetShapeList = new int[numberOfTargets];

        // Shape options, relate to TargetObject indexes (0:Cube, 1: Cross, 2: Spheres, 3: Star)
        int[] shape = { 1, 2, 3, 4 };

        // Position options
        List<int> positions = Enumerable.Range(0, numberOfTargetPositions).ToList();
        int[] positions2 = { 1, 2 };

        // Color options
        Color lightblue = new Color(173f / 255f, 216f / 255f, 230 / 255f);
        Color[] color = { Color.red, lightblue, Color.green, Color.yellow };

        List<Color> colors = new List<Color>();
        colors.Add(Color.red);
        colors.Add(Color.white);
        colors.Add(Color.green);
        colors.Add(Color.yellow);

        int[] _sPosition;
        Color[] _sColor = new Color[numberOfTargets];

        for (int i = 0; i < multiplier; i++) // multiplier loop
        {
            int startIndex = 0;
            Array.Copy(color, startIndex, _sColor, 0, numberOfTargets);
            _sPosition = ShuffleMe(positions.ToArray());

            AssignTrialProperties(_sPosition, _sColor);
        }
    }

    void AssignTrialProperties(int[] _position, Color[] _color)
    {
        BCITSTTaskInfo myTaskInfo = gameObject.GetComponent<BCITSTTaskInfo>();
        BCITSTTrialState.TargetObject[] TargetObjectList = new BCITSTTrialState.TargetObject[numberOfTargets];

        for (int i = 0; i < numberOfTargets; i++)
        {
            TargetObjectList[i] = new BCITSTTrialState.TargetObject(_position[i], _color[i]);
        }

        var taskParams = new BCITSTTrialState()
        {
            TaskName = taskInfo.taskName,
            TargetObjectL = TargetObjectList,
            TargetPositionIndex = 0,
            CuePositionIndex = _position[0],
            CueColorIndex = 0, //_color[0],
            TargetObjectIndex = 0
        };

        allTrials.Add(taskParams);
    }

  
    public void OnEnable()
    {
        SetInstance();
        AddListeners();
        // IMPORTANT. Singleton access from BaseExperimentController calls must point to this

        base.taskInfo = GetComponent<BCITSTTaskInfo>();
        base.currentTrial = new BCITSTTrialState();

        BCITSTLSLController myLSL = gameObject.GetComponent<BCITSTLSLController>();
        
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

        for (int target_index = 0; target_index < maxNumberOfTargets; target_index++)
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
        for (int i = 0; i < numberOfTargets; i++)
        {
            ExperimentObject target = taskInfo.targetObjects[i].GetComponent<ExperimentObject>();
            target.transform.position = Vector3.zero;  // replace zero with the original position
            target.transform.rotation = Quaternion.identity;  // replace identity with the original rotation
            target.transform.localScale = Vector3.one*cueObjectScaleFactor;
            
        }

        targetIndex = 0;
        taskInfo.targetHold = targetHoldOriginal;
        taskInfo.fixationPoint.transform.parent.gameObject.SetActive(true);
        // Reset fixation point.
        SetFixationVisibility(true);

        HideTargets();
    }

    public override void ShowCues()
    {
        for (int i=0; i<numberOfTargets; i++)
        {
            ExperimentObject cue = taskInfo.targetObjects[i].GetComponent<ExperimentObject>();
            cue.transform.GetChild(0).GetComponent<Renderer>().enabled = true;
            cue.transform.GetChild(0).GetComponent<Renderer>().material.color = currentTrial.TargetObjectL[i].tcolor;
            cue.transform.localScale = new Vector3(1.0f, 1.0f, 0.05f) * cueObjectScaleFactor;

            int col = 0;
            int row = 0;
            Vector3 pos = new Vector3(0.0f, 0.0f, 0.0f);
            Quaternion rot = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
            restPosition = false;
            if (numberOfTargetPositions == 2)
            {
                col = (int)(currentTrial.TargetObjectL[i].tposition % 2.0f);
                row = 0;
                pos = new Vector3(1.0f - 2.0f * (float)(col), (float)(row), 0.0f);
            }
            else if (numberOfTargetPositions == 3)
            {
                if (currentTrial.TargetObjectL[i].tposition == 2)
                {
                    // For rest target, allow for the fixation point to be in front and selectable
                    pos = new Vector3(0.0f, 0.0f, 1.5f);
                    taskInfo.targetHold = 0.05f;
                    restPosition = true; 
                }
                else
                {
                    col = (int)(currentTrial.TargetObjectL[i].tposition % 2.0f);
                    row = 0;
                    pos = new Vector3(1.0f - 2.0f * (float)(col), (float)(row), 0.0f);
                }
            }
            else if (numberOfTargetPositions == 4)
            {
                row = (int)(currentTrial.TargetObjectL[i].tposition / 2.0f);
                col = currentTrial.TargetObjectL[i].tposition % 2;
                pos = new Vector3(-0.5f + 1.0f * (float)(col), 0.5f - 1.0f * (float)(row), 0.0f);

            }
            else if (numberOfTargetPositions == 5)
            {
                if (currentTrial.TargetObjectL[i].tposition == 4)
                {
                    // For rest target, allow for the fixation point to be in front and selectable
                    pos = new Vector3(0.0f, 0.0f, 1.5f);
                    taskInfo.targetHold = 0.05f;
                    restPosition = true;
                }
                else
                {
                    row = (int)(currentTrial.TargetObjectL[i].tposition / 2.0f);
                    col = currentTrial.TargetObjectL[i].tposition % 2;
                    pos = new Vector3(-0.5f + 1.0f * (float)(col), 0.5f - 1.0f * (float)(row), 0.0f);
                }
            }
            else if (numberOfTargetPositions == 16)
            {
                row = (int)(currentTrial.TargetObjectL[i].tposition / 4.0f);
                col = currentTrial.TargetObjectL[i].tposition % 4;
                pos = new Vector3(-1.2f + 0.8f * (float)(col), 0.8f - 0.4f * (float)(row), 0.0f);
            }
            else if (numberOfTargetPositions == 17)
            {
                if (currentTrial.TargetObjectL[i].tposition == 16)
                {
                    // For rest target, allow for the fixation point to be in front and selectable
                    pos = new Vector3(0.0f, 0.0f, 1.5f);
                    taskInfo.targetHold = 0.05f;
                    restPosition = true;
                }
                else
                {
                    row = (int)(currentTrial.TargetObjectL[i].tposition / 4.0f);
                    col = currentTrial.TargetObjectL[i].tposition % 4;
                    pos = new Vector3(-1.2f + 0.8f * (float)(col), 0.8f - 0.4f * (float)(row), 1.5f);
                }
            }

            cue.transform.SetPositionAndRotation(pos, rot);
            cue.IsVisible = true;
         }
    }

    public override void HideCues()
    {
        
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
        ResetObjects();
        taskInfo.fixationPoint.GetComponent<ExperimentObject>().ResetColor();
        taskInfo.fixationPoint.GetComponent<ExperimentObject>().IsVisible = false;    
    }
       
    private void UpdateTargetCursorPosition(int targetIndex)
    {
        MousePosition currentMousePosition;
        if ( targetIndex == 0 )
        {
            Cursor.lockState = CursorLockMode.Confined;
            GetCursorPos(out mp);
            Cursor.lockState = CursorLockMode.None;
            currentMousePosition = mp;
        }
        else
        { 
            GetCursorPos(out currentMousePosition);
        }

        ExperimentObject cue = taskInfo.targetObjects[targetIndex].GetComponent<ExperimentObject>();
        cue.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f) * cueObjectScaleFactor;
        if (restPosition == true && checkResponseEnter == true)
        {
            // Needs to remove the Fixation point selected Target so the new Rest Target can be selected
            selectedTargetIndex = -1;

            Cursor.visible = false;
            cueX = mp.x;
            cursorToCueX = mp.x - Screen.width * 0.25f;
            cueY = mp.y;
            cursorToCueY = mp.y - Screen.height * 0.25f;

            float distX = cueX - cursorToCueX;
            float distY = cueY - cursorToCueY;
            float distT = (float)(Math.Sqrt(Math.Pow(distX, 2) + Math.Pow(distY, 2)));

            timeInterval = 1.0f / (unityUpdateRate / updateEveryCount);
            trajectoryTime = (distT*2.0f) / (cursorSpeed); // increase duration for the rest target
            
        }
        else
        {
            Vector3 cueTPos = cue.transform.position;

            //laptop: scale factor: Vector3 cueScreenPosition = Camera.main.WorldToScreenPoint(new Vector3(cueTPos.x, cueTPos.y, cueTPos.z-0.9f));// 0.3f -(float)(scaleFactor)));// 0.3f + Camera.main.nearClipPlane));// Camera.main.nearClipPlane - 0.3f +(float)(scaleFactor)));// + 1.352f)); // 0.9f)); //-Camera.main.nearClipPlane));  //-0.5f)); //-Camera.main.nearClipPlane));
            Vector3 cueScreenPosition = Camera.main.WorldToScreenPoint(new Vector3(cueTPos.x, cueTPos.y, cueTPos.z));// 0.3f -(float)(scaleFactor)));// 0.3f + Camera.main.nearClipPlane));// Camera.main.nearClipPlane - 0.3f +(float)(scaleFactor)));// + 1.352f)); // 0.9f)); //-Camera.main.nearClipPlane));  //-0.5f)); //-Camera.main.nearClipPlane));

            offsetX = mp.x - (Screen.width >> 1);
            offsetY = mp.y - (Screen.height >> 1);
            cueX = cueScreenPosition.x + offsetX;
            cueY = (Screen.height - cueScreenPosition.y) + offsetY;

            cursorToCueX = currentMousePosition.x;
            cursorToCueY = currentMousePosition.y;

            float distX = cueX - cursorToCueX;
            float distY = cueY - cursorToCueY;
            float distT = (float)(Math.Sqrt(Math.Pow(distX, 2) + Math.Pow(distY, 2)));

            timeInterval = 1.0f / (unityUpdateRate / updateEveryCount);
            trajectoryTime = distT / cursorSpeed;
        }
        
    }


    public override int CheckResponse()
    {
        int ResponseID = -1; // Stay in Response phase

        if (checkResponseEnter == true)
        {
            updateCount = (int)(updateEveryCount);
            UpdateTargetCursorPosition(targetIndex);
			if (decodingMode == 1)
			{
				trajectoryTime = trajectoryTime*30;
			}
            checkResponseEnter = false;          
        }

        Vector3 mouseLoc = Input.mousePosition;
        if ((mouseLoc.x > 0) && (mouseLoc.x < Screen.width) &&
                (mouseLoc.y > 0) && (mouseLoc.y < Screen.height))
        {
            if (checkResponseEnter == false)
            {
                if (trajectoryTime > timeInterval)
                {
                    if (updateCount >= updateEveryCount)
                    {
						if (decodingMode == 1)
						{
							BCITSTLSLController myLSL = gameObject.GetComponent<BCITSTLSLController>();
							if (lastTimestamp != 0.0)
							{
								double timedelta = Time.time - lastTimestamp;
								double posDeltaX = myLSL.velocity_x* userVelocityFactor * timedelta;
								double posDeltaY = myLSL.velocity_y* userVelocityFactor * timedelta;

								float newPosX = ((float)(cursorToCueX) + (float)(posDeltaX));
								float newPosY = ((float)(cursorToCueY) + (float)(posDeltaY));

  							    cursorToCueX = newPosX;
							    cursorToCueY = newPosY;
							}
							
							lastTimestamp = myLSL.timestamp;
						}
						else
						{
							float nx = UnityEngine.Random.Range(-10.0f, 10.0f);
							float ny = UnityEngine.Random.Range(-10.0f, 10.0f);

							float redRatio = 1.0f / trajectoryTime;

							xdmp = (cueX - cursorToCueX) * redRatio;
							ydmp = (cueY - cursorToCueY) * redRatio;

							cursorToCueX = cursorToCueX + xdmp + nx;
							cursorToCueY = cursorToCueY + ydmp + ny;

						}
						SetCursorPos((int)(cursorToCueX), (int)(cursorToCueY));
						updateCount = 1;

                    }
                    trajectoryTime -= timeInterval;
                    updateCount += 1;
                    if (trajectoryTime <= timeInterval)
                    {
                        SetCursorPos((int)(cueX), (int)(cueY));
                    }
                }
            }
        }


        if (selectedObject == SelectedObjectClass.Fixation)
        {
            OutcomeEnum = Trial_Outcomes.Ignored;
            targetTimer = Mathf.Infinity;
        }
        else if (selectedObject == SelectedObjectClass.Target)
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
                if (selectedTargetIndex == targetIndex) //currentTrial.TargetPositionIndex)
                {
                    if (targetIndex < numberOfTargets - 1)
                    {
                        targetTimer = Mathf.Infinity;
                        targetIndex++;
                        currentTrial.TargetPositionIndex = targetIndex;
                        currentTrial.CuePositionIndex = currentTrial.TargetObjectL[targetIndex].tposition;
                        currentTrial.CueColorIndex = targetIndex;
                        currentTrial.TargetObjectIndex = targetIndex;
                        
                        checkResponseEnter = true;
                    }
                    else
                    {
                        ResponseID = 1; // Go to feedback
                        OutcomeEnum = Trial_Outcomes.GoodTrial;
                        currentTrial.IsCorrect = true;
                    }
                }
                else
                {
                    //Keep looking
                    targetTimer = Mathf.Infinity;
                    
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

        return ResponseID;
    }

    public override void GiveFeedback()
    {
        // For Rest Target, the cursor was set to invisible
        Cursor.visible = true;
        feedbackObj = taskInfo.targetObjects[currentTrial.SelectedObjectIndex];
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
                    UnityEngine.Debug.Log("Progressing without particles" + ex.GetType());
                }
            }
        }

        checkResponseEnter = true;
        SetCursorPos((int)(mp.x), (int)(mp.y));
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
        else if (taskInfo.targetObjects.Contains(go))
        {
            UpdateReactionTime();

            selectedObject = SelectedObjectClass.Target;
            selectedTargetIndex = taskInfo.targetObjects.IndexOf(go);
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
