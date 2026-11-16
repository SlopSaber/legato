using System.Threading.Tasks;
using IPA.Utilities;
#if !BEAT_SABER_1_29_0
using OculusStudios.Platform.Core;
#endif

namespace Legato.Platform {
    internal enum AuthenticationPlatform { Unknown, Steam, Meta }

    internal sealed class UserCredentials {
        internal readonly AuthenticationPlatform Platform;
        internal readonly string UserId, Proof;
        internal UserCredentials(AuthenticationPlatform platform, string userId, string proof) {
            Platform = platform; UserId = userId; Proof = proof;
        }
    }

    internal sealed class PlatformAuthentication {
#if BEAT_SABER_1_29_0
        private readonly IPlatformUserModel _model;
        private PlatformAuthentication(PlatformLeaderboardsModel model) =>
            _model = model.GetField<IPlatformUserModel, PlatformLeaderboardsModel>("_platformUserModel");
#else
        private readonly IPlatform _model;
        private PlatformAuthentication(PlatformLeaderboardsModel model) =>
            _model = model.GetField<IPlatform, PlatformLeaderboardsModel>("_platform");
#endif
        internal static PlatformAuthentication FromLeaderboards(PlatformLeaderboardsModel model) => new PlatformAuthentication(model);

        internal async Task<UserCredentials> GetCredentials() {
#if BEAT_SABER_1_29_0
            UserInfo user = await _model.GetUserInfo();
            var auth = await _model.GetUserAuthToken();
            AuthenticationPlatform platform = user.platform == UserInfo.Platform.Steam ? AuthenticationPlatform.Steam
                : user.platform == UserInfo.Platform.Oculus ? AuthenticationPlatform.Meta : AuthenticationPlatform.Unknown;
            return new UserCredentials(platform, user.platformUserId, auth.token);
#else
            string proof = await _model.user.GetAccessTokenAsync();
            AuthenticationPlatform platform = _model.vendor == Vendor.Valve ? AuthenticationPlatform.Steam
                : _model.vendor == Vendor.Meta ? AuthenticationPlatform.Meta : AuthenticationPlatform.Unknown;
            return new UserCredentials(platform, _model.user.userId.ToString(), proof);
#endif
        }
    }
}
