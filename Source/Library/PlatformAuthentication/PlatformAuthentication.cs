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
    internal sealed class PlatformAuthentication {
        private readonly NativePlatformService _model;
        private Task<UserIdentity>? _identityTask;
        internal PlatformAuthentication(NativePlatformService model) => _model = model;
        internal bool Available => _model != null;
        internal async Task<global::UserInfo?> GetGameUserInfo(CancellationToken token) {
            token.ThrowIfCancellationRequested();
            if (!Available) return null;
#if BEAT_SABER_1_29_0
            var user = await _model.GetUserInfo().ConfigureAwait(true);
            token.ThrowIfCancellationRequested();
            return user;
#else
            await Task.CompletedTask.ConfigureAwait(true);
            var user = _model.user;
            if (user == null || user.userId == 0) return null;
            var platform = _model.vendor == Vendor.Valve ? global::UserInfo.Platform.Steam
                : _model.vendor == Vendor.Meta ? global::UserInfo.Platform.Oculus
                : _model.vendor == Vendor.Sony ? global::UserInfo.Platform.PS5 : global::UserInfo.Platform.Test;
            return new global::UserInfo(platform, user.userId.ToString(System.Globalization.CultureInfo.InvariantCulture), user.displayName);
#endif
        }
        internal UserIdentity? Identity => _identityTask?.Status == TaskStatus.RanToCompletion ? _identityTask.Result : null;
        internal Task<UserIdentity> GetIdentity() {
            if (!Available) return Task.FromResult(new UserIdentity(AuthenticationPlatform.Unknown, string.Empty, string.Empty));
            if (_identityTask == null || _identityTask.IsFaulted || _identityTask.IsCanceled ||
                (_identityTask.Status == TaskStatus.RanToCompletion &&
                 (string.IsNullOrEmpty(_identityTask.Result.UserId) || string.Equals(_identityTask.Result.UserId, "0", System.StringComparison.Ordinal))))
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
            UserInfo user = await _model.GetUserInfo().ConfigureAwait(true);
            if (user == null) return new UserIdentity(AuthenticationPlatform.Unknown, string.Empty, string.Empty);
            AuthenticationPlatform platform = user.platform == UserInfo.Platform.Steam ? AuthenticationPlatform.Steam
                : user.platform == UserInfo.Platform.Oculus ? AuthenticationPlatform.Meta : AuthenticationPlatform.Unknown;
            return new UserIdentity(platform, user.platformUserId, user.userName);
        }
#else
        private Task<UserIdentity> ReadIdentity() {
            AuthenticationPlatform platform = _model.vendor == Vendor.Valve ? AuthenticationPlatform.Steam
                : _model.vendor == Vendor.Meta ? AuthenticationPlatform.Meta : AuthenticationPlatform.Unknown;
            return Task.FromResult(new UserIdentity(platform, _model.user?.userId.ToString() ?? string.Empty, _model.user?.displayName ?? string.Empty));
        }
#endif
        internal async Task<UserCredentials> GetCredentials() {
            UserIdentity identity = await GetIdentity().ConfigureAwait(true);
#if BEAT_SABER_1_29_0
            string proof = (await _model.GetUserAuthToken().ConfigureAwait(true)).token;
#else
            string proof = await _model.user.GetAccessTokenAsync().ConfigureAwait(true);
#endif
            return new UserCredentials(identity.Platform, identity.UserId, proof);
        }
    }
}
