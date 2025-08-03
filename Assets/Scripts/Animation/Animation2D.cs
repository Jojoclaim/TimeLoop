using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New 2D Animation", menuName = "Animation/Simple 2D Animation", order = 1)]
public class Animation2D : ScriptableObject
{
    public enum AnimationType { OneShot, Loop, PingPong }

    [Serializable]
    public struct Frame
    {
        [Tooltip("The sprite for this animation frame.")]
        public Sprite sprite;

        [Tooltip("The duration (in 'animation frames') for this sprite. Allows fractional values like 1.1, 1.2 for fine control. Defaults to 1 if <= 0.")]
        public float duration;
    }

    [Tooltip("List of frames with their sprites and durations.")]
    public List<Frame> frames = new List<Frame>();

    [Tooltip("Overall animation speed in frames per second (FPS).")]
    public float fps = 10f;

    [Tooltip("The type of animation playback.")]
    public AnimationType animationType = AnimationType.Loop;
}