namespace RedLight.Lighting;

public struct Attenuation
{
    public float Range;
    public float Constant = 1.0f;
    public float Linear;
    public float Quadratic;

    public Attenuation(float range, float linear, float quadratic, float constant = 1.0f)
    {
        Range = range;
        Constant = constant;
        Linear = linear;
        Quadratic = quadratic;
    }

    public float Calculate(out float attenuation, out float luminosity)
    {
        attenuation = Constant + Linear * Range + Quadratic * float.Pow(Range, 2);
        luminosity = 1 / attenuation;
        return luminosity;
    }

    public readonly struct DefaultValues
    {
        public static readonly Attenuation Range3250 = new Attenuation(3250f, 0.0014f, 0.000007f);
        public static readonly Attenuation Range600 = new Attenuation(600f, 0.007f, 0.0002f);
        public static readonly Attenuation Range325 = new Attenuation(325f, 0.014f, 0.0007f);
        public static readonly Attenuation Range200 = new Attenuation(200f, 0.022f, 0.0019f);
        public static readonly Attenuation Range160 = new Attenuation(160f, 0.027f, 0.0028f);
        public static readonly Attenuation Range100 = new Attenuation(100f, 0.045f, 0.0075f);
        public static readonly Attenuation Range65 = new Attenuation(65f, 0.07f, 0.017f);
        public static readonly Attenuation Range50 = new Attenuation(50f, 0.09f, 0.032f);
        public static readonly Attenuation Range32 = new Attenuation(32f, 0.14f, 0.07f);
        public static readonly Attenuation Range13 = new Attenuation(13f, 0.35f, 0.44f);
    }

}