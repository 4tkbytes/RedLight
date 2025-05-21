namespace RedLight.Utils
{
    public static class RLMath
    {
        public static float DegreesToRadians(float degrees)
        {
            return degrees * (MathF.PI / 180.0f);
        }
        
        public static float RadiansToDegrees(float radians)
        {
            return radians * (180.0f / MathF.PI);
        }
    }
}