#nullable enable
#if BEAT_SABER_1_29_0
global using NativePlatformService = global::IPlatformUserModel;
#else
global using NativePlatformService = global::OculusStudios.Platform.Core.IPlatform;
#endif
using System.Threading.Tasks;
using System.Threading;
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
        internal Task<UserIdentity> GetIdentity() {
            if (_identityTask == null || _identityTask.IsFaulted || _identityTask.IsCanceled ||
                (_identityTask.Status == TaskStatus.RanToCompletion &&
                 (string.IsNullOrEmpty(_identityTask.Result.UserId) || _identityTask.Result.UserId == "0")))
                _identityTask = ReadIdentity();
            return _identityTask;
        }

        internal async Task<UserIdentity> GetIdentity(CancellationToken token) {
            token.ThrowIfCancellationRequested();
            Task<UserIdentity> identity = GetIdentity();
            if (!identity.IsCompleted && token.CanBeCanceled) {
                var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                using (token.Register(() => cancelled.TrySetResult(true)))
                    await Task.WhenAny(identity, cancelled.Task).ConfigureAwait(false);
            }
            token.ThrowIfCancellationRequested();
            return await identity.ConfigureAwait(false);
        }

        internal static PlatformAuthentication FromLeaderboards(PlatformLeaderboardsModel model) => new PlatformAuthentication(
#if BEAT_SABER_1_29_0
            model.GetField<NativePlatformService, PlatformLeaderboardsModel>("_platformUserModel"));
#else
            model.GetField<NativePlatformService, PlatformLeaderboardsModel>("_platform"));
#endif
#if BEAT_SABER_1_29_0
        private async Task<UserIdentity> ReadIdentity() {
            UserInfo user = await _model.GetUserInfo();
            AuthenticationPlatform platform = user.platform == UserInfo.Platform.Steam ? AuthenticationPlatform.Steam
                : user.platform == UserInfo.Platform.Oculus ? AuthenticationPlatform.Meta : AuthenticationPlatform.Unknown;
            return new UserIdentity(platform, user.platformUserId);
        }
#else
        private Task<UserIdentity> ReadIdentity() {
            AuthenticationPlatform platform = _model.vendor == Vendor.Valve ? AuthenticationPlatform.Steam
                : _model.vendor == Vendor.Meta ? AuthenticationPlatform.Meta : AuthenticationPlatform.Unknown;
            return Task.FromResult(new UserIdentity(platform, _model.user?.userId.ToString() ?? string.Empty));
        }
#endif
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
