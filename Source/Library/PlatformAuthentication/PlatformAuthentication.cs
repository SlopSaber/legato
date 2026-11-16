#nullable enable
#if BEAT_SABER_1_29_0
global using NativePlatformService = global::IPlatformUserModel;
#else
global using NativePlatformService = global::OculusStudios.Platform.Core.IPlatform;
#endif
using System.Threading.Tasks;
using IPA.Utilities;
#if !BEAT_SABER_1_29_0
using OculusStudios.Platform.Core;
#endif

namespace Legato.Platform {
    internal enum AuthenticationPlatform { Unknown, Steam, Meta }

    internal sealed class UserIdentity {
        internal readonly AuthenticationPlatform Platform;
        internal readonly string UserId;
        internal UserIdentity(AuthenticationPlatform platform, string userId) { Platform = platform; UserId = userId; }
    }

    internal sealed class UserCredentials {
        internal readonly AuthenticationPlatform Platform;
        internal readonly string UserId, Proof;
        internal UserCredentials(AuthenticationPlatform platform, string userId, string proof) {
            Platform = platform; UserId = userId; Proof = proof;
        }
    }

    internal sealed class PlatformAuthentication {
        private readonly NativePlatformService _model;
        private Task<UserIdentity>? _identityTask;
        internal PlatformAuthentication(NativePlatformService model) => _model = model;
        internal UserIdentity? Identity => _identityTask?.Status == TaskStatus.RanToCompletion ? _identityTask.Result : null;
        internal Task<UserIdentity> GetIdentity() => _identityTask ??= ReadIdentity();

        internal static PlatformAuthentication FromLeaderboards(PlatformLeaderboardsModel model) => new PlatformAuthentication(
#if BEAT_SABER_1_29_0
            model.GetField<NativePlatformService, PlatformLeaderboardsModel>("_platformUserModel"));
#else
            model.GetField<NativePlatformService, PlatformLeaderboardsModel>("_platform"));
#endif
        private async Task<UserIdentity> ReadIdentity() {
#if BEAT_SABER_1_29_0
            UserInfo user = await _model.GetUserInfo();
            AuthenticationPlatform platform = user.platform == UserInfo.Platform.Steam ? AuthenticationPlatform.Steam
                : user.platform == UserInfo.Platform.Oculus ? AuthenticationPlatform.Meta : AuthenticationPlatform.Unknown;
            return new UserIdentity(platform, user.platformUserId);
#else
            await Task.CompletedTask;
            AuthenticationPlatform platform = _model.vendor == Vendor.Valve ? AuthenticationPlatform.Steam
                : _model.vendor == Vendor.Meta ? AuthenticationPlatform.Meta : AuthenticationPlatform.Unknown;
            return new UserIdentity(platform, _model.user?.userId.ToString() ?? string.Empty);
#endif
        }
        internal async Task<UserCredentials> GetCredentials() {
            UserIdentity identity = await GetIdentity();
#if BEAT_SABER_1_29_0
            string proof = (await _model.GetUserAuthToken()).token;
#else
            string proof = await _model.user.GetAccessTokenAsync();
#endif
            return new UserCredentials(identity.Platform, identity.UserId, proof);
        }
    }
}
