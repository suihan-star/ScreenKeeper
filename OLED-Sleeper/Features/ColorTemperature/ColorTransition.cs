namespace OLED_Sleeper.Features.ColorTemperature
{
    /// <summary>A short catch-up uses monotonic milliseconds and bounded steps, independently of wall-clock scheduling.</summary>
    public sealed record ColorTransition(ColorValue From, ColorValue Target, long StartedMilliseconds)
    {
        public const int DurationMilliseconds = 10_000;
        public const int StepMilliseconds = 2_000;

        public double Progress(long nowMilliseconds) => Math.Clamp((nowMilliseconds - StartedMilliseconds) / StepMilliseconds / 5d, 0, 1);

        public ColorValue Frame(long nowMilliseconds)
        {
            double progress = Progress(nowMilliseconds);
            var frame = From.Copy();
            for (int i = 0; i < frame.Channels.Length; i++)
                frame.Channels[i] = (ushort)Math.Round(From.Channels[i] + (Target.Channels[i] - From.Channels[i]) * progress);
            return frame;
        }
    }
}
