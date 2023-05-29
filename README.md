# NCCCognitiveTasks
Unity Tasks for NCC trial

#Setup
* Clone the repository `https://github.com/SachsLab/PDSaccadeTasks.git` 
* Clone the unityvr_r-and-d and copy content under Assets\unityvr_r-and-d
*	Clone the hmd-eyes `https://github.com/pupil-labs/hmd-eyes` and copy content under existing Assets\Plugin\hmd-eyes
*	Clone the additional tasks from this repository and add them under Assets\Scripts\Tasks (overwrite the files when asked)
*	Overwrite the Assets\Scenes\CO_MGS_WAM.unity scene with the new scene with the new tasks
*	Open Unity `2019.2.0f1` or newer and load the project
*	Within Unity, if using tobii, you will need to search for tobii in the asset store and import it
*	Within Unity, in the Project tab, open Assets\Scenes\CO_MGS_WAM, you can then play the games
  *	Select the Task from drop down menu
  *	Select Input method (not all tested)
  *	Reset Camera
  *	Begin Experiment

# Cognitive Tasks Descriptions
*	Wisconsin Card Sorting 
  * Possible rules: Color, Shape and number of objects - the rule changes every 3 to 7 trials randomly
  * Based on the Cue target (middle object), choose the target (1 of 4) that follows the rule
* NBack 
  * Typical n-back game set to 2 back, with 1 to 4 possible numbers
* Visual Spatial Attention 
  * Cue indicates the trial attentional space, if and once it moves saccade to it. If a distractor move, the correct response is to keep fixating
* Decision Making Task 
  * Very basic task, simply choose your favorite between 4 colored spheres
* Delayed Saccade Task
  * Delayed saccade to four possible target locations (testing FEF activity and movement preparation)
* BCI Window Cursor Control
  * This task is designed to move a cursor between targets within a grid of 2, 4 or 16 possible target locations. 
  * If one target long sequence is selected, the target location can be the origin meaning this is a rest position. 
  * The task works in 2 modes, 1) fully automated where the cursor is automatically moved from one position to the others adding noise to make it more natural, or 2) using a decoded velocity ( x, y ) received from an lsl stream to control the mouse cursor in the BCI control mode.
