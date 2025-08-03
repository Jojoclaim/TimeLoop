#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(Animation2D))]
public class Animation2DEditor : Editor
{
    private ReorderableList list;
    private const float MinDuration = 0.1f;
    private const float MaxDuration = 10f;

    private void OnEnable()
    {
        list = new ReorderableList(serializedObject, serializedObject.FindProperty("frames"), true, true, true, true);

        list.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Frames");
        };

        list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;
            float previewSize = rect.height - 4;
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, previewSize, previewSize), element.FindPropertyRelative("sprite"), GUIContent.none);
            var durationProp = element.FindPropertyRelative("duration");
            EditorGUI.Slider(new Rect(rect.x + previewSize + 10, rect.y, rect.width - previewSize - 10, EditorGUIUtility.singleLineHeight), durationProp, MinDuration, MaxDuration, new GUIContent("Duration"));
        };

        list.elementHeight = 70f; // Increased height for better sprite preview visibility
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        list.DoLayoutList();

        if (GUILayout.Button("Add Selected Sprites"))
        {
            var selectedSprites = Selection.GetFiltered<Sprite>(SelectionMode.Assets);
            var framesProp = serializedObject.FindProperty("frames");
            int insertIndex = framesProp.arraySize;

            foreach (var sprite in selectedSprites)
            {
                framesProp.InsertArrayElementAtIndex(insertIndex);
                var newElement = framesProp.GetArrayElementAtIndex(insertIndex);
                newElement.FindPropertyRelative("sprite").objectReferenceValue = sprite;
                newElement.FindPropertyRelative("duration").floatValue = 1f;
                insertIndex++;
            }
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("fps"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("animationType"));

        serializedObject.ApplyModifiedProperties();
    }
}
#endif