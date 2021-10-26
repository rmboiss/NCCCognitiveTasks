using UnityEngine;

public class CursorTasksGUIController : BaseGUIController
{
    // Defines the list of available tasks. 
    public enum TaskTypes { SelectTask, CenterOutTask, WhackAMole, MemoryGuidedSaccade,
        WisconsinCardSorting, NBackTask, VisualSpatialAttention, DecisionMakingTask, DelayedSaccadeTask }

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
    }

    // Experiment Selection   
    public override void PopulateExperimentSelection()
    {
        MainMenu.TaskSelection.AddOptions(GetEnumSpacedList<TaskTypes>());
    }
    protected override void ExperimentSelectionHandler(int selection)
    {
        string typeName;
        switch (selection)
        {
            case 0:
                typeName = "";
                break; // do nothing
            case 1: // Center Out
                typeName = "COExperimentController";
                break;
            case 2: // Whack A Mole
                typeName = "WAMExperimentController";
                break;
            //case 3: //Joeyo Playback
            //    typeName = "JPExperimentController";
            //    break;
            case 3: // Memmory guided saccade
                typeName = "MGSExperimentController";
                break;
            case 4: // PFC Wisconsin card sorting task
                typeName = "PFCWExperimentController";
                break;
            case 5: // Working Memory NBack task
                typeName = "NBackExperimentController";
                break;
            case 6: // Visual Spatial Attention task
                typeName = "VSAExperimentController";
                break;
            case 7: // Decision Making task
                typeName = "DMTExperimentController";
                break;
            case 8: // Delayed Saccade task
                typeName = "DSTExperimentController";
                break;
            default:
                typeName = "";
                break;
        }

        // find all experiment controllers
        // need to deactivate all exp before activating new task
        BaseExperimentController newTask = null;
        foreach (object obj in Resources.FindObjectsOfTypeAll(typeof(BaseExperimentController)))
        {
            ((BaseExperimentController)obj).gameObject.SetActive(false);

            if (obj.GetType().FullName == typeName)
                newTask = ((BaseExperimentController)obj);

        }
        newTask.gameObject.SetActive(true);

        // Need to update the hitbox and tolerance values from the newly selected
        // task info
        MainMenu.HitBox.text = newTask.taskInfo.HitBox.ToString();
        MainMenu.Tolerance.text = newTask.taskInfo.awayTolerance.ToString();
        MainMenu.ResponseTime.text = newTask.taskInfo.responseTime.ToString();

        // Make sure the values update
        MainMenu.HitBox.onEndEdit.Invoke(newTask.taskInfo.HitBox.ToString());
        MainMenu.Tolerance.onEndEdit.Invoke(newTask.taskInfo.awayTolerance.ToString());
        MainMenu.ResponseTime.onEndEdit.Invoke(newTask.taskInfo.responseTime.ToString());

        // Update remote recording controller
        remoteRC.currentExperiment = ((TaskTypes)selection).ToString();

        // Now that we have an experiment controller instance
        // we can activate all the buttons requiring it. 
        MainMenu.InputSelection.interactable = true;
        ControlMenu.BeginExperiment.interactable = true;
        ControlMenu.ResetCamera.interactable = true;

    }

    // Button press
    // Update file Info functions
    public override void UpdateRCFileInfo()
    {
        // Set the values for the Remote Recording Controller
        remoteRC.patientID = MainMenu.PatientID.text;
        remoteRC.sessionID = MainMenu.SessionID.text;
        remoteRC.NSPFileComment = MainMenu.FileComment.text;
        remoteRC.currentExperiment = ((TaskTypes)MainMenu.TaskSelection.value).ToString();
    }

}
