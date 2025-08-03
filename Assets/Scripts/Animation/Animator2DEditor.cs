#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(Animator2D))]
public class Animator2DEditor : Editor
{
    private ReorderableList parametersList;
    private ReorderableList statesList;
    private ReorderableList transitionsList;

    private void OnEnable()
    {
        parametersList = new ReorderableList(serializedObject, serializedObject.FindProperty("parameters"), true, true, true, true);
        parametersList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Parameters");
        parametersList.drawElementCallback = DrawParameterElement;
        parametersList.elementHeight = EditorGUIUtility.singleLineHeight * 2 + 4;

        statesList = new ReorderableList(serializedObject, serializedObject.FindProperty("states"), true, true, true, true);
        statesList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "States");
        statesList.drawElementCallback = DrawStateElement;
        statesList.elementHeight = EditorGUIUtility.singleLineHeight * 3 + 4;

        transitionsList = new ReorderableList(serializedObject, serializedObject.FindProperty("transitions"), true, true, true, true);
        transitionsList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Transitions");
        transitionsList.drawElementCallback = DrawTransitionElement;
        transitionsList.elementHeightCallback = index =>
        {
            var element = transitionsList.serializedProperty.GetArrayElementAtIndex(index);
            var conditionsProp = element.FindPropertyRelative("conditions");
            int numConditions = conditionsProp.arraySize;
            float single = EditorGUIUtility.singleLineHeight;
            return (4 + numConditions) * single + 10 + numConditions * 2;
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        parametersList.DoLayoutList();
        statesList.DoLayoutList();

        SerializedProperty entryStateProp = serializedObject.FindProperty("entryState");
        string[] stateNames = GetStateNames();
        int selectedIndex = System.Array.IndexOf(stateNames, entryStateProp.stringValue);
        int newIndex = EditorGUILayout.Popup("Entry State", selectedIndex, stateNames);
        if (newIndex >= 0 && newIndex < stateNames.Length)
        {
            entryStateProp.stringValue = stateNames[newIndex];
        }

        transitionsList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawParameterElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        var element = parametersList.serializedProperty.GetArrayElementAtIndex(index);
        rect.y += 2;

        EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width * 0.4f, EditorGUIUtility.singleLineHeight),
            element.FindPropertyRelative("name"), GUIContent.none);

        var typeProp = element.FindPropertyRelative("type");
        Animator2D.ParameterType selectedType = (Animator2D.ParameterType)EditorGUI.EnumPopup(new Rect(rect.x + rect.width * 0.4f + 5, rect.y, rect.width * 0.6f - 5, EditorGUIUtility.singleLineHeight),
            (Animator2D.ParameterType)typeProp.enumValueIndex);
        typeProp.enumValueIndex = (int)selectedType;

        rect.y += EditorGUIUtility.singleLineHeight + 2;

        var ptype = (Animator2D.ParameterType)typeProp.enumValueIndex;
        if (ptype == Animator2D.ParameterType.Bool)
        {
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("boolValue"), new GUIContent("Default Value"));
        }
        else if (ptype == Animator2D.ParameterType.Int)
        {
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("intValue"), new GUIContent("Default Value"));
        }
        else if (ptype == Animator2D.ParameterType.Float)
        {
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("floatValue"), new GUIContent("Default Value"));
        }
        // Trigger has no default value field
    }

    private void DrawStateElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        var element = statesList.serializedProperty.GetArrayElementAtIndex(index);
        rect.y += 2;

        EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
            element.FindPropertyRelative("name"), new GUIContent("State Name"));

        rect.y += EditorGUIUtility.singleLineHeight + 2;

        EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
            element.FindPropertyRelative("clip"), new GUIContent("Animation Clip"));

        rect.y += EditorGUIUtility.singleLineHeight + 2;

        EditorGUI.Slider(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
            element.FindPropertyRelative("speed"), 0.1f, 5f, new GUIContent("Speed"));
    }

    private void DrawTransitionElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        var element = transitionsList.serializedProperty.GetArrayElementAtIndex(index);
        rect.y += 2;

        string[] stateNames = GetStateNames();

        // From State
        var fromProp = element.FindPropertyRelative("fromState");
        int fromIndex = System.Array.IndexOf(stateNames, fromProp.stringValue);
        fromIndex = EditorGUI.Popup(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
            "From", fromIndex, stateNames);
        if (fromIndex >= 0 && fromIndex < stateNames.Length) fromProp.stringValue = stateNames[fromIndex];

        rect.y += EditorGUIUtility.singleLineHeight + 2;

        // To State
        var toProp = element.FindPropertyRelative("toState");
        int toIndex = System.Array.IndexOf(stateNames, toProp.stringValue);
        toIndex = EditorGUI.Popup(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
            "To", toIndex, stateNames);
        if (toIndex >= 0 && toIndex < stateNames.Length) toProp.stringValue = stateNames[toIndex];

        rect.y += EditorGUIUtility.singleLineHeight + 2;

        // Wait for Completion checkbox
        EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
            element.FindPropertyRelative("waitForCompletion"), new GUIContent("Wait for Animation End"));

        rect.y += EditorGUIUtility.singleLineHeight + 4;

        // Conditions
        var conditionsProp = element.FindPropertyRelative("conditions");
        string[] paramNames = GetParamNames();

        List<int> toRemove = new List<int>();

        const float buttonWidth = 20f;
        const float space = 5f;
        float fieldWidth = rect.width - buttonWidth - (space * 3);
        float width1 = fieldWidth * 0.4f;
        float width2 = fieldWidth * 0.25f;
        float width3 = fieldWidth * 0.35f;
        float remainingWidth = width2 + space + width3;

        for (int i = 0; i < conditionsProp.arraySize; i++)
        {
            var condElement = conditionsProp.GetArrayElementAtIndex(i);
            var paramNameProp = condElement.FindPropertyRelative("parameterName");
            var condTypeProp = condElement.FindPropertyRelative("conditionType");
            var boolValProp = condElement.FindPropertyRelative("boolValue");
            var intValProp = condElement.FindPropertyRelative("intValue");
            var floatValProp = condElement.FindPropertyRelative("floatValue");

            // Parameter popup
            int paramIndex = System.Array.IndexOf(paramNames, paramNameProp.stringValue);
            paramIndex = EditorGUI.Popup(new Rect(rect.x, rect.y, width1, EditorGUIUtility.singleLineHeight),
                paramIndex, paramNames);
            if (paramIndex >= 0 && paramIndex < paramNames.Length) paramNameProp.stringValue = paramNames[paramIndex];

            var selectedParam = ((Animator2D)target).parameters.FirstOrDefault(p => p.name == paramNameProp.stringValue);

            if (selectedParam != null)
            {
                var ptype = selectedParam.type;
                Rect remainingRect = new Rect(rect.x + width1 + space, rect.y, remainingWidth, EditorGUIUtility.singleLineHeight);

                if (ptype == Animator2D.ParameterType.Trigger)
                {
                    EditorGUI.LabelField(remainingRect, "is set");
                    condTypeProp.enumValueIndex = (int)Animator2D.ConditionType.Equals;
                    boolValProp.boolValue = true;
                }
                else if (ptype == Animator2D.ParameterType.Bool)
                {
                    string[] opts = { "is true", "is false" };
                    int sel = boolValProp.boolValue ? 0 : 1;
                    sel = EditorGUI.Popup(remainingRect, sel, opts);
                    condTypeProp.enumValueIndex = (int)Animator2D.ConditionType.Equals;
                    boolValProp.boolValue = (sel == 0);
                }
                else
                {
                    Rect condRect = new Rect(rect.x + width1 + space, rect.y, width2, EditorGUIUtility.singleLineHeight);
                    Rect valRect = new Rect(rect.x + width1 + space + width2 + space, rect.y, width3, EditorGUIUtility.singleLineHeight);

                    Animator2D.ConditionType selectedCond = (Animator2D.ConditionType)EditorGUI.EnumPopup(condRect, (Animator2D.ConditionType)condTypeProp.enumValueIndex);
                    condTypeProp.enumValueIndex = (int)selectedCond;

                    if (ptype == Animator2D.ParameterType.Int)
                    {
                        intValProp.intValue = EditorGUI.IntField(valRect, intValProp.intValue);
                    }
                    else if (ptype == Animator2D.ParameterType.Float)
                    {
                        floatValProp.floatValue = EditorGUI.FloatField(valRect, floatValProp.floatValue);
                    }
                }
            }

            // Remove button
            if (GUI.Button(new Rect(rect.x + rect.width - buttonWidth, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight), "-"))
            {
                toRemove.Add(i);
            }

            rect.y += EditorGUIUtility.singleLineHeight + 2;
        }

        // Remove conditions after drawing to avoid index issues
        foreach (var r in toRemove.OrderByDescending(x => x))
        {
            conditionsProp.DeleteArrayElementAtIndex(r);
        }

        // Add Condition button
        if (GUI.Button(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), "Add Condition"))
        {
            conditionsProp.InsertArrayElementAtIndex(conditionsProp.arraySize);
            var newCond = conditionsProp.GetArrayElementAtIndex(conditionsProp.arraySize - 1);
            newCond.FindPropertyRelative("parameterName").stringValue = "";
            newCond.FindPropertyRelative("conditionType").enumValueIndex = 0;
            newCond.FindPropertyRelative("boolValue").boolValue = false;
            newCond.FindPropertyRelative("intValue").intValue = 0;
            newCond.FindPropertyRelative("floatValue").floatValue = 0f;
        }
    }

    private string[] GetStateNames()
    {
        return ((Animator2D)target).states.Select(s => s.name).ToArray();
    }

    private string[] GetParamNames()
    {
        return ((Animator2D)target).parameters.Select(p => p.name).ToArray();
    }
}
#endif