namespace Legato.Platform {
    internal sealed class UserCredentials {
        internal readonly AuthenticationPlatform Platform;
        internal readonly string UserId, Proof;
        internal UserCredentials(AuthenticationPlatform platform, string userId, string proof) {
            Platform = platform; UserId = userId; Proof = proof;
        }
    }
}
