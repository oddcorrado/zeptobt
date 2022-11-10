using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ZeptoBtAction", menuName = "ScriptableObjects/ZeptoBtAction", order = 1)]
public class ZeptpBtNameToAction : ScriptableObject
{
    [System.Serializable]
    public class NameToAction
    {
        public string name;
        public MonoBehaviour action;
    }

    public NameToAction[] nameToActions;
}
