using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Misc_Utilities;

public class PFCWTaskInfo : BaseTaskInfo
{
    public List<GameObject> targetWalls = new List<GameObject>();

    public List<Modifiers> myModifiers = new List<Modifiers>();
    public List<ConditionTypes> myConditionTypes = new List<ConditionTypes>();
    public List<ResponseTypes> myResponseTypes = new List<ResponseTypes>();
}
