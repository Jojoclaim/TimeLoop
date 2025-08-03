// This file includes the main component and its custom editor.
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using System;
#endif

public class FeedbackHandler : MonoBehaviour
{
    [SerializeField] private FeedbackProfile profile;

    private List<Feedback> activeFeedbacks = new List<Feedback>();

    public void Play()
    {
        if (profile == null) return;
        foreach (var effect in profile.effects)
        {
            if (effect.active)
            {
                Feedback feedback = effect.CreateFeedback();
                feedback.Start(gameObject);
                activeFeedbacks.Add(feedback);
            }
        }
    }

    private void Update()
    {
        for (int i = activeFeedbacks.Count - 1; i >= 0; i--)
        {
            if (activeFeedbacks[i].Update(Time.deltaTime))
            {
                activeFeedbacks[i].Complete();
                activeFeedbacks.RemoveAt(i);
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(FeedbackHandler))]
public class FeedbackHandlerEditor : Editor
{
    private SerializedProperty profileProp;
    private ReorderableList effectList;
    private SerializedObject profileSerializedObject;
    private Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

    private void OnEnable()
    {
        profileProp = serializedObject.FindProperty("profile");
        UpdateEffectList();
    }

    private void UpdateEffectList()
    {
        if (profileProp.objectReferenceValue != null)
        {
            profileSerializedObject = new SerializedObject(profileProp.objectReferenceValue);
            var effects = profileSerializedObject.FindProperty("effects");

            effectList = new ReorderableList(profileSerializedObject, effects, true, true, true, true);
            effectList.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "Feedback Effects");

            effectList.drawElementCallback = DrawElementCallback;
            effectList.elementHeightCallback = ElementHeightCallback;
            effectList.onAddDropdownCallback = OnAddDropdownCallback;

            effectList.onRemoveCallback = (ReorderableList l) =>
            {
                ReorderableList.defaultBehaviours.DoRemoveButton(l);
            };
        }
        else
        {
            effectList = null;
            profileSerializedObject = null;
        }
    }

    private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
    {
        var element = effectList.serializedProperty.GetArrayElementAtIndex(index);
        if (element.managedReferenceValue == null) return;

        string id = element.managedReferenceId.ToString();
        if (!foldouts.ContainsKey(id)) foldouts[id] = false;

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float lineSpacing = 2f;
        float indent = 15f;

        // First line: Active checkbox with label
        Rect checkRect = new Rect(rect.x + 5, rect.y + 2, rect.width - 10, lineHeight);
        SerializedProperty activeProp = element.FindPropertyRelative("active");
        string typeName = element.managedReferenceValue.GetType().Name.Replace("Effect", "");
        activeProp.boolValue = EditorGUI.ToggleLeft(checkRect, typeName, activeProp.boolValue);

        // Second line: Foldout
        Rect foldRect = new Rect(rect.x + indent, rect.y + lineHeight + lineSpacing, rect.width - indent - 5, lineHeight);
        foldouts[id] = EditorGUI.Foldout(foldRect, foldouts[id], "Settings", true);

        if (foldouts[id])
        {
            // Draw each property individually, skipping "active"
            EditorGUI.indentLevel++;

            float yPos = rect.y + (lineHeight * 2) + (lineSpacing * 2) + 3;
            SerializedProperty prop = element.Copy();
            SerializedProperty endProp = prop.GetEndProperty();

            bool enterChildren = true;
            while (prop.NextVisible(enterChildren) && !SerializedProperty.EqualContents(prop, endProp))
            {
                if (prop.name != "active") // Skip the active property we already drew
                {
                    float height = EditorGUI.GetPropertyHeight(prop, true);
                    Rect propertyRect = new Rect(rect.x + indent + 10, yPos, rect.width - indent - 15, height);
                    EditorGUI.PropertyField(propertyRect, prop, true);
                    yPos += height + 2;
                }
                enterChildren = false;
            }

            EditorGUI.indentLevel--;
        }
    }

    private float ElementHeightCallback(int index)
    {
        var element = effectList.serializedProperty.GetArrayElementAtIndex(index);
        if (element.managedReferenceValue == null) return EditorGUIUtility.singleLineHeight;

        string id = element.managedReferenceId.ToString();

        // Base height now includes two lines (checkbox + foldout)
        float baseHeight = (EditorGUIUtility.singleLineHeight * 2) + 6;

        if (foldouts.ContainsKey(id) && foldouts[id])
        {
            // Calculate height by iterating through properties
            float additionalHeight = 5; // Initial padding

            SerializedProperty prop = element.Copy();
            SerializedProperty endProp = prop.GetEndProperty();

            bool enterChildren = true;
            while (prop.NextVisible(enterChildren) && !SerializedProperty.EqualContents(prop, endProp))
            {
                if (prop.name != "active") // Skip the active property
                {
                    additionalHeight += EditorGUI.GetPropertyHeight(prop, true) + 2;
                }
                enterChildren = false;
            }

            return baseHeight + additionalHeight + 3; // Extra padding at the end
        }

        return baseHeight;
    }

    private void OnAddDropdownCallback(Rect buttonRect, ReorderableList l)
    {
        var menu = new GenericMenu();
        AddEffectMenuItem(menu, typeof(ColorFlashEffect), "Color Flash");
        AddEffectMenuItem(menu, typeof(CameraShakeEffect), "Camera Shake");
        AddEffectMenuItem(menu, typeof(ScalePunchEffect), "Scale Punch");
        menu.ShowAsContext();
    }

    private void AddEffectMenuItem(GenericMenu menu, Type type, string name)
    {
        bool exists = false;
        for (int i = 0; i < effectList.serializedProperty.arraySize; i++)
        {
            var element = effectList.serializedProperty.GetArrayElementAtIndex(i);
            if (element.managedReferenceValue?.GetType() == type)
            {
                exists = true;
                break;
            }
        }
        if (exists)
        {
            menu.AddDisabledItem(new GUIContent(name));
        }
        else
        {
            menu.AddItem(new GUIContent(name), false, AddEffect, type);
        }
    }

    private void AddEffect(object userData)
    {
        Type type = (Type)userData;
        var index = effectList.serializedProperty.arraySize;
        effectList.serializedProperty.arraySize++;
        var element = effectList.serializedProperty.GetArrayElementAtIndex(index);
        element.managedReferenceValue = Activator.CreateInstance(type);
        profileSerializedObject.ApplyModifiedProperties();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(profileProp);
        if (EditorGUI.EndChangeCheck())
        {
            UpdateEffectList();
        }

        if (profileProp.objectReferenceValue == null)
        {
            if (GUILayout.Button("New Profile"))
            {
                CreateNewProfile();
            }
        }
        else
        {
            if (effectList != null)
            {
                profileSerializedObject.Update();
                effectList.DoLayoutList();
                profileSerializedObject.ApplyModifiedProperties();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void CreateNewProfile()
    {
        FeedbackProfile profile = ScriptableObject.CreateInstance<FeedbackProfile>();
        profile.name = "New Feedback Profile";
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/NewFeedbackProfile.asset");
        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();
        profileProp.objectReferenceValue = profile;
        serializedObject.ApplyModifiedProperties();
        UpdateEffectList();
    }
}
#endif