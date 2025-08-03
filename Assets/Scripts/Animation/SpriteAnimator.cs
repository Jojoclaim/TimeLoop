using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator2D animator;

    private Dictionary<string, Animator2D.Parameter> runtimeParameters = new Dictionary<string, Animator2D.Parameter>();
    private string currentStateName;
    private Animator2D.AnimationState currentState;
    private Animation2D currentClip;
    private float currentSpeed;
    private int currentFrameIndex = 0;
    private float frameTimer = 0f;
    private bool forward = true;
    private bool isClipPlaying = true;

    private void OnValidate()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        if (animator == null) return;

        foreach (var p in animator?.parameters ?? Enumerable.Empty<Animator2D.Parameter>())
        {
            var copy = new Animator2D.Parameter
            {
                name = p.name,
                type = p.type,
                boolValue = p.boolValue,
                intValue = p.intValue,
                floatValue = p.floatValue
            };
            runtimeParameters.Add(p.name, copy);
        }

        currentStateName = animator.entryState;
        SetCurrentState();
    }

    private void Update()
    {
        if (animator == null)
        {
            return;
        }

        if (currentClip == null || currentClip.frames == null || currentClip.frames.Count == 0 || !isClipPlaying)
        {
            CheckTransitions();
            return;
        }

        float currentFrameTime = GetFrameTime(currentFrameIndex);
        frameTimer += Time.deltaTime;

        while (frameTimer >= currentFrameTime)
        {
            frameTimer -= currentFrameTime;
            AdvanceFrame();
            if (!isClipPlaying) break;
            currentFrameTime = GetFrameTime(currentFrameIndex);
        }

        CheckTransitions();
    }

    private void SetCurrentState()
    {
        currentState = animator?.states?.FirstOrDefault(s => s.name == currentStateName);
        if (currentState == null) return;

        currentClip = currentState?.clip;
        currentSpeed = currentState.speed;
        ResetClip();
    }

    private void ResetClip()
    {
        currentFrameIndex = 0;
        frameTimer = 0f;
        forward = true;
        isClipPlaying = true;
        if (currentClip != null && currentClip.frames != null && currentClip.frames.Count > 0 && spriteRenderer != null)
        {
            spriteRenderer.sprite = currentClip.frames[0].sprite;
        }
    }

    private float GetFrameTime(int index)
    {
        if (currentClip == null || currentClip.frames == null || index < 0 || index >= currentClip.frames.Count)
        {
            return 0f;
        }
        float hold = currentClip.frames[index].duration;
        if (hold <= 0f) hold = 1f;
        return hold / (currentClip.fps * currentSpeed);
    }

    private void AdvanceFrame()
    {
        if (currentClip == null || currentClip.frames == null || spriteRenderer == null)
        {
            isClipPlaying = false;
            return;
        }

        var type = currentClip.animationType;
        if (type == Animation2D.AnimationType.OneShot)
        {
            currentFrameIndex++;
            if (currentFrameIndex >= currentClip.frames.Count)
            {
                currentFrameIndex = currentClip.frames.Count - 1;
                isClipPlaying = false;
                return;
            }
        }
        else if (type == Animation2D.AnimationType.Loop)
        {
            currentFrameIndex = (currentFrameIndex + 1) % currentClip.frames.Count;
        }
        else if (type == Animation2D.AnimationType.PingPong)
        {
            if (forward)
            {
                currentFrameIndex++;
                if (currentFrameIndex >= currentClip.frames.Count - 1)
                    forward = false;
            }
            else
            {
                currentFrameIndex--;
                if (currentFrameIndex <= 0)
                    forward = true;
            }
            currentFrameIndex = Mathf.Clamp(currentFrameIndex, 0, currentClip.frames.Count - 1);
        }

        spriteRenderer.sprite = currentClip.frames[currentFrameIndex].sprite;
    }

    private void CheckTransitions()
    {
        foreach (var trans in animator?.transitions ?? Enumerable.Empty<Animator2D.Transition>())
        {
            if (trans.fromState != currentStateName) continue;

            bool allConditionsMet = true;
            List<Animator2D.Parameter> triggersToConsume = new List<Animator2D.Parameter>();

            if (trans.conditions != null)
            {
                foreach (var cond in trans.conditions)
                {
                    if (!runtimeParameters.TryGetValue(cond.parameterName, out var param))
                    {
                        allConditionsMet = false;
                        break;
                    }

                    bool met = false;
                    switch (param.type)
                    {
                        case Animator2D.ParameterType.Bool:
                            bool bval = param.boolValue;
                            if (cond.conditionType == Animator2D.ConditionType.Equals)
                                met = bval == cond.boolValue;
                            else if (cond.conditionType == Animator2D.ConditionType.NotEquals)
                                met = bval != cond.boolValue;
                            break;
                        case Animator2D.ParameterType.Trigger:
                            bool tval = param.boolValue;
                            if (cond.conditionType == Animator2D.ConditionType.Equals && cond.boolValue)
                            {
                                met = tval;
                                if (met) triggersToConsume.Add(param);
                            }
                            break;
                        case Animator2D.ParameterType.Int:
                            int ival = param.intValue;
                            if (cond.conditionType == Animator2D.ConditionType.Equals)
                                met = ival == cond.intValue;
                            else if (cond.conditionType == Animator2D.ConditionType.GreaterThan)
                                met = ival > cond.intValue;
                            else if (cond.conditionType == Animator2D.ConditionType.LessThan)
                                met = ival < cond.intValue;
                            else if (cond.conditionType == Animator2D.ConditionType.NotEquals)
                                met = ival != cond.intValue;
                            break;
                        case Animator2D.ParameterType.Float:
                            float fval = param.floatValue;
                            if (cond.conditionType == Animator2D.ConditionType.Equals)
                                met = Mathf.Approximately(fval, cond.floatValue);
                            else if (cond.conditionType == Animator2D.ConditionType.GreaterThan)
                                met = fval > cond.floatValue;
                            else if (cond.conditionType == Animator2D.ConditionType.LessThan)
                                met = fval < cond.floatValue;
                            else if (cond.conditionType == Animator2D.ConditionType.NotEquals)
                                met = !Mathf.Approximately(fval, cond.floatValue);
                            break;
                    }
                    if (!met)
                    {
                        allConditionsMet = false;
                        break;
                    }
                }
            }

            if (allConditionsMet)
            {
                foreach (var trig in triggersToConsume)
                {
                    trig.boolValue = false;
                }
                currentStateName = trans.toState;
                SetCurrentState();
                break;
            }
        }
    }

    public void SetBool(string name, bool value)
    {
        if (runtimeParameters.TryGetValue(name, out var p) && p.type == Animator2D.ParameterType.Bool)
            p.boolValue = value;
    }

    public void SetInt(string name, int value)
    {
        if (runtimeParameters.TryGetValue(name, out var p) && p.type == Animator2D.ParameterType.Int)
            p.intValue = value;
    }

    public void SetFloat(string name, float value)
    {
        if (runtimeParameters.TryGetValue(name, out var p) && p.type == Animator2D.ParameterType.Float)
            p.floatValue = value;
    }

    public void SetTrigger(string name)
    {
        if (runtimeParameters.TryGetValue(name, out var p) && p.type == Animator2D.ParameterType.Trigger)
            p.boolValue = true;
    }
}