namespace OLED_Sleeper.Infrastructure
{
    /// <summary>
    /// Runtime identifiers are independent of the upstream project and namespace names.
    /// Keep the installer identifiers in sync when changing them.
    /// </summary>
    public static class ProductIdentity
    {
        public const string Name = "ScreenKeeper";
        public const string DisplayName = "多屏管家 ScreenKeeper";
        public const string SettingsTitle = DisplayName + " · 设置";
        public const string MutexName = "ScreenKeeper-Mutex";
        public const string ShowWindowEventName = "ScreenKeeper-ShowWindow";
    }
}
