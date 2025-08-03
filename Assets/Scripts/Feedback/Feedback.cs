using EZCameraShake;
using UnityEngine;

public abstract class Feedback
{
    public abstract void Start(GameObject target);
    public abstract bool Update(float deltaTime);
    public virtual void Complete() { }
}

public class ColorFlashFeedback : Feedback
{
    private Color flashColor;
    private float duration;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float timer;

    public ColorFlashFeedback(Color flashColor, float duration)
    {
        this.flashColor = flashColor;
        this.duration = duration;
    }

    public override void Start(GameObject target)
    {
        spriteRenderer = target.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        timer = 0f;
    }

    public override bool Update(float deltaTime)
    {
        if (spriteRenderer == null) return true;

        timer += deltaTime;
        float t = timer / duration;
        if (t < 0.5f)
        {
            // Lerp to flash color
            spriteRenderer.color = Color.Lerp(originalColor, flashColor, t * 2f);
        }
        else
        {
            // Lerp back to original
            spriteRenderer.color = Color.Lerp(flashColor, originalColor, (t - 0.5f) * 2f);
        }
        return timer >= duration;
    }

    public override void Complete()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
}

public class CameraShakeFeedback : Feedback
{
    private float magnitude;
    private float roughness;
    private float fadeIn;
    private float fadeOut;

    public CameraShakeFeedback(float magnitude, float roughness, float fadeIn, float fadeOut)
    {
        this.magnitude = magnitude;
        this.roughness = roughness;
        this.fadeIn = fadeIn;
        this.fadeOut = fadeOut;
    }

    public override void Start(GameObject target)
    {
        CameraShaker.Instance.ShakeOnce(magnitude, roughness, fadeIn, fadeOut);
    }

    public override bool Update(float deltaTime)
    {
        return true; // Completes immediately as shake is handled by CameraShaker
    }
}

public class ScalePunchFeedback : Feedback
{
    private float punchAmount;
    private float duration;
    private Vector3 originalScale;
    private float timer;
    private GameObject target;

    public ScalePunchFeedback(float punchAmount, float duration)
    {
        this.punchAmount = punchAmount;
        this.duration = duration;
    }

    public override void Start(GameObject target)
    {
        this.target = target;
        originalScale = target.transform.localScale;
        timer = 0f;
    }

    public override bool Update(float deltaTime)
    {
        timer += deltaTime;
        float t = timer / duration;
        if (t < 0.5f)
        {
            // Scale up
            float scaleFactor = Mathf.Lerp(1f, punchAmount, t * 2f);
            target.transform.localScale = originalScale * scaleFactor;
        }
        else
        {
            // Scale back
            float scaleFactor = Mathf.Lerp(punchAmount, 1f, (t - 0.5f) * 2f);
            target.transform.localScale = originalScale * scaleFactor;
        }
        return timer >= duration;
    }

    public override void Complete()
    {
        target.transform.localScale = originalScale;
    }
}