#nullable enable
#if LEGATO_EARLY_STANDARD_MAPS
global using NativePlatformService = Legato.Platform.EarlyPlatformService;
using GameUserInfo = Legato.Platform.UserIdentity;
#elif LEGATO_BEFORE_MULTIPLAYER
global using NativePlatformService = global::PlatformUserModelSO;
using GameUserInfo = global::PlatformUserModelSO.UserInfo;
#elif LEGATO_PLATFORM_USER_MODEL
global using NativePlatformService = global::IPlatformUserModel;
using GameUserInfo = global::UserInfo;
#else
global using NativePlatformService = global::OculusStudios.Platform.Core.IPlatform;
using GameUserInfo = global::UserInfo;
#endif
using System.Threading.Tasks;
using System.Threading;
using IPA.Utilities;
#if LEGATO_EARLY_STANDARD_MAPS
using Steamworks;
#elif !LEGATO_PLATFORM_USER_MODEL
using OculusStudios.Platform.Core;
#endif

namespace Legato.Platform {
#if LEGATO_EARLY_STANDARD_MAPS
    internal sealed class EarlyPlatformService {
    }
#endif

    internal sealed class PlatformAuthentication {
        private readonly NativePlatformService _model;
        private Task<UserIdentity>? _identityTask;
        internal PlatformAuthentication(NativePlatformService model) => _model = model;
        internal bool Available => _model != null;
        internal async Task<GameUserInfo?> GetGameUserInfo(CancellationToken token) {
            token.ThrowIfCancellationRequested();
            if (!Available) return null;
#if LEGATO_EARLY_STANDARD_MAPS
            UserIdentity identity = await GetIdentity(token).ConfigureAwait(true);
            return string.IsNullOrEmpty(identity.UserId) || identity.UserId == "0" ? null : identity;
#elif LEGATO_BEFORE_MULTIPLAYER
            var completion = new TaskCompletionSource<GameUserInfo?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var request = _model.GetUserInfo((result, user) =>
                completion.TrySetResult(result == PlatformUserModelSO.GetUserInfoResult.OK ? user : null));
            using var registration = token.Register(() => completion.TrySetCanceled());
            try { return await completion.Task.ConfigureAwait(true); }
            catch (System.OperationCanceledException) when (token.IsCancellationRequested) {
                request?.Cancel();
                throw;
            }
#elif BEAT_SABER_1_29_0
            var user = await _model.GetUserInfo().ConfigureAwait(true);
            token.ThrowIfCancellationRequested();
            return user;
#elif LEGATO_PLATFORM_USER_MODEL
            return await _model.GetUserInfo(token).ConfigureAwait(true);
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

#if LEGATO_EARLY_STANDARD_MAPS
        internal static PlatformAuthentication FromLeaderboards(PlatformLeaderboardsModel model) => new PlatformAuthentication(new EarlyPlatformService());
#else
        internal static PlatformAuthentication FromLeaderboards(PlatformLeaderboardsModel model) => new PlatformAuthentication(
#if LEGATO_PLATFORM_USER_MODEL
            model.GetField<NativePlatformService, PlatformLeaderboardsModel>("_platformUserModel"));
#else
            model.GetField<NativePlatformService, PlatformLeaderboardsModel>("_platform"));
#endif
#endif
#if LEGATO_EARLY_STANDARD_MAPS
        private Task<UserIdentity> ReadIdentity() {
            string userId = SteamUser.GetSteamID().m_SteamID.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return Task.FromResult(new UserIdentity(AuthenticationPlatform.Steam, userId, SteamFriends.GetPersonaName() ?? string.Empty));
        }
#elif LEGATO_BEFORE_MULTIPLAYER
        private async Task<UserIdentity> ReadIdentity() {
            GameUserInfo? user = await GetGameUserInfo(CancellationToken.None).ConfigureAwait(true);
            if (user == null) return new UserIdentity(AuthenticationPlatform.Unknown, string.Empty, string.Empty);
            AuthenticationPlatform platform = _model.platformInfo.platform switch {
                PlatformInfo.Platform.Steam => AuthenticationPlatform.Steam,
                PlatformInfo.Platform.Oculus => AuthenticationPlatform.Meta,
                _ => AuthenticationPlatform.Unknown
            };
            return new UserIdentity(platform, user.userId, user.userName);
        }
#elif LEGATO_PLATFORM_USER_MODEL
        private async Task<UserIdentity> ReadIdentity() {
            UserInfo? user = await GetGameUserInfo(CancellationToken.None).ConfigureAwait(true);
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
#if LEGATO_BEFORE_MULTIPLAYER || LEGATO_EARLY_STANDARD_MAPS
            string proof = await GetLegacyToken(CancellationToken.None).ConfigureAwait(true);
#elif LEGATO_PLATFORM_USER_MODEL
            string proof = (await _model.GetUserAuthToken().ConfigureAwait(true)).token;
#else
            string proof = await _model.user.GetAccessTokenAsync().ConfigureAwait(true);
#endif
            return new UserCredentials(identity.Platform, identity.UserId, proof);
        }
#if LEGATO_EARLY_STANDARD_MAPS
        internal async Task<string> GetLegacyToken(CancellationToken token) {
            token.ThrowIfCancellationRequested();
            var buffer = new byte[1024];
            var ticket = HAuthTicket.Invalid;
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var callback = Callback<GetAuthSessionTicketResponse_t>.Create(response => {
                if (response.m_hAuthTicket == ticket) completion.TrySetResult(response.m_eResult == EResult.k_EResultOK);
            });
            try {
                ticket = SteamUser.GetAuthSessionTicket(buffer, buffer.Length, out uint length);
                if (ticket == HAuthTicket.Invalid || length == 0)
                    throw new System.InvalidOperationException("The game platform did not return an authentication token.");
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                var completed = await Task.WhenAny(completion.Task, Task.Delay(System.TimeSpan.FromSeconds(10), timeout.Token)).ConfigureAwait(true);
                token.ThrowIfCancellationRequested();
                timeout.Cancel();
                if (completed != completion.Task || !completion.Task.Result)
                    throw new System.InvalidOperationException("The game platform did not return an authentication token.");
                return System.BitConverter.ToString(buffer, 0, (int)length).Replace("-", string.Empty);
            } finally {
                callback.Unregister();
            }
        }
#elif LEGATO_BEFORE_MULTIPLAYER
        internal async Task<string> GetLegacyToken(CancellationToken token) {
            token.ThrowIfCancellationRequested();
            var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var request = _model.GetUserAuthToken((result, info) => {
                if (result == PlatformUserModelSO.GetUserAuthTokenResult.OK && info != null && !string.IsNullOrEmpty(info.platformToken))
                    completion.TrySetResult(info.platformToken);
                else
                    completion.TrySetException(new System.InvalidOperationException("The game platform did not return an authentication token."));
            });
            using var registration = token.Register(() => completion.TrySetCanceled());
            try {
                string proof = await completion.Task.ConfigureAwait(true);
                token.ThrowIfCancellationRequested();
                return proof;
            } catch (System.OperationCanceledException) when (token.IsCancellationRequested) {
                request?.Cancel();
                throw;
            }
        }
#endif
    }
}
