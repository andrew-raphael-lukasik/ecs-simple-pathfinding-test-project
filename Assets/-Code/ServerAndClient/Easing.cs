// #region Assembly UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Decompiled with ICSharpCode.Decompiler 8.1.1.7464
// #endregion
// namespace UnityEngine.UIElements.Experimental;

using Unity.Mathematics;

public static class Easing
{

    public static float Step(float t) => (!(t < 0.5f)) ? 1 : 0;
    public static float Linear(float t) => t;
    public static float InSine(float t) => math.sin(math.PIHALF * (t - 1f)) + 1f;
    public static float OutSine(float t) => math.sin(t * math.PIHALF);
    public static float InOutSine(float t) => (math.sin(math.PI * (t - 0.5f)) + 1f) * 0.5f;
    public static float InQuad(float t) => t * t;
    public static float OutQuad(float t) => t * (2f - t);

    public static float InOutQuad(float t)
    {
        t *= 2f;
        if (t < 1f)
            return t * t * 0.5f;

        return -0.5f * ((t - 1f) * (t - 3f) - 1f);
    }

    public static float InCubic(float t) => InPower(t, 3);
    public static float OutCubic(float t) => OutPower(t, 3);
    public static float InOutCubic(float t) => InOutPower(t, 3);
    public static float InPower(float t, int power) => math.pow(t, power);

    public static float OutPower(float t, int power)
    {
        int num = ((power % 2 != 0) ? 1 : (-1));
        return (float)num * (math.pow(t - 1f, power) + (float)num);
    }

    public static float InOutPower(float t, int power)
    {
        t *= 2f;
        if (t < 1f)
            return InPower(t, power) * 0.5f;

        int num = ((power % 2 != 0) ? 1 : (-1));
        return (float)num * 0.5f * (math.pow(t - 2f, power) + (float)(num * 2));
    }

    public static float InBounce(float t) => 1f - OutBounce(1f - t);

    public static float OutBounce(float t)
    {
        if (t < 0.36363637f)
            return 7.5625f * t * t;

        if (t < 0.72727275f)
        {
            float num = (t -= 0.54545456f);
            return 7.5625f * num * t + 0.75f;
        }

        if (t < 0.90909094f)
        {
            float num2 = (t -= 0.8181818f);
            return 7.5625f * num2 * t + 0.9375f;
        }

        float num3 = (t -= 21f / 22f);
        return 7.5625f * num3 * t + 63f / 64f;
    }

    public static float InOutBounce(float t)
    {
        if (t < 0.5f)
            return InBounce(t * 2f) * 0.5f;

        return OutBounce((t - 0.5f) * 2f) * 0.5f + 0.5f;
    }

    public static float InElastic(float t)
    {
        if (t == 0f)
            return 0f;

        if (t == 1f)
            return 1f;

        float num = 0.3f;
        float num2 = num / 4f;
        float num3 = math.pow(2f, 10f * (t -= 1f));
        return 0f - num3 * math.sin((t - num2) * (math.PI * 2f) / num);
    }

    public static float OutElastic(float t)
    {
        if (t == 0f)
            return 0f;

        if (t == 1f)
            return 1f;

        float num = 0.3f;
        float num2 = num / 4f;
        return math.pow(2f, -10f * t) * math.sin((t - num2) * (math.PI * 2f) / num) + 1f;
    }

    public static float InOutElastic(float t)
    {
        if (t < 0.5f)
            return InElastic(t * 2f) * 0.5f;

        return OutElastic((t - 0.5f) * 2f) * 0.5f + 0.5f;
    }

    public static float InBack(float t)
    {
        float num = 1.70158f;
        return t * t * ((num + 1f) * t - num);
    }

    public static float OutBack(float t) => 1f - InBack(1f - t);

    public static float InOutBack(float t)
    {
        if (t < 0.5f)
            return InBack(t * 2f) * 0.5f;

        return OutBack((t - 0.5f) * 2f) * 0.5f + 0.5f;
    }

    public static float InBack(float t, float s) => t * t * ((s + 1f) * t - s);
    public static float OutBack(float t, float s) => 1f - InBack(1f - t, s);

    public static float InOutBack(float t, float s)
    {
        if (t < 0.5f)
            return InBack(t * 2f, s) * 0.5f;

        return OutBack((t - 0.5f) * 2f, s) * 0.5f + 0.5f;
    }

    public static float InCirc(float t) => 0f - (math.sqrt(1f - t * t) - 1f);

    public static float OutCirc(float t)
    {
        t -= 1f;
        return math.sqrt(1f - t * t);
    }

    public static float InOutCirc(float t)
    {
        t *= 2f;
        if (t < 1f)
            return -0.5f * (math.sqrt(1f - t * t) - 1f);

        t -= 2f;
        return 0.5f * (math.sqrt(1f - t * t) + 1f);
    }

}
