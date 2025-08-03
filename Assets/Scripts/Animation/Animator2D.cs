using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New 2D Animator", menuName = "Animation/Simple 2D Animator", order = 2)]
public class Animator2D : ScriptableObject
{
    public enum ParameterType { Bool, Int, Float, Trigger }

    public enum ConditionType { Equals, GreaterThan, LessThan, NotEquals }

    [Serializable]
    public class Parameter
    {
        public string name;
        public ParameterType type;
        public bool boolValue;
        public int intValue;
        public float floatValue;
    }

    [Tooltip("List of parameters (bools, ints, floats, triggers) used for transitions.")]
    public List<Parameter> parameters = new List<Parameter>();

    [Serializable]
    public class AnimationState
    {
        public string name;
        public Animation2D clip;
        public float speed = 1f;
    }

    [Tooltip("List of animation states.")]
    public List<AnimationState> states = new List<AnimationState>();

    [Tooltip("The entry state name.")]
    public string entryState;

    [Serializable]
    public class Condition
    {
        public string parameterName;
        public ConditionType conditionType;
        public bool boolValue;
        public int intValue;
        public float floatValue;
    }

    [Serializable]
    public class Transition
    {
        public string fromState;
        public string toState;
        public List<Condition> conditions = new List<Condition>();
    }

    [Tooltip("List of transitions between states.")]
    public List<Transition> transitions = new List<Transition>();
}