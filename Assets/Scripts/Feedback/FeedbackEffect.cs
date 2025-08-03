using UnityEngine;

[System.Serializable]
public abstract class FeedbackEffect
{
    public bool active = true;
    public abstract Feedback CreateFeedback();
}

[System.Serializable]
public class ColorFlashEffect : FeedbackEffect
{
    public Color flashColor = Color.red;
    public float duration = 0.5f;

    public override Feedback CreateFeedback()
    {
        return new ColorFlashFeedback(flashColor, duration);
    }
}

[System.Serializable]
public class CameraShakeEffect : FeedbackEffect
{
    public float magnitude = 2f;
    public float roughness = 10f;
    public float fadeIn = 0.1f;
    public float fadeOut = 1f;

    public override Feedback CreateFeedback()
    {
        return new CameraShakeFeedback(magnitude, roughness, fadeIn, fadeOut);
    }
}

[System.Serializable]
public class ScalePunchEffect : FeedbackEffect
{
    public float punchAmount = 1.2f;
    public float duration = 0.3f;

    public override Feedback CreateFeedback()
    {
        return new ScalePunchFeedback(punchAmount, duration);
    }
}