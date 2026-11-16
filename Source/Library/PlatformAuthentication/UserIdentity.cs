namespace Legato.Platform {
    internal sealed class UserIdentity {
        internal readonly AuthenticationPlatform Platform;
        internal readonly string UserId;
        internal readonly string DisplayName;
        internal UserIdentity(AuthenticationPlatform platform, string userId, string displayName = "") { Platform = platform; UserId = userId; DisplayName = displayName; }
    }
}
