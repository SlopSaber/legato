#nullable enable

using Legato.Platform.Authentication;
using Legato.Platform.Friends;
using Legato.Platform.Users;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Legato.Platform {
    public class GamePlatformAdapter : IPlatformUserProvider, IPlatformAuthenticationProvider, IPlatformFriendsProvider, IDisposable {
        private readonly IPlatformUserModel _platformUserModel;
#if LEGATO_STEAM_WEB_AUTH
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private Task<string>? _ticketTask;
        private SteamWebTicket? _ticket;
#endif

        public GamePlatformAdapter(IPlatformUserModel platformUserModel) {
            _platformUserModel = platformUserModel;
        }

        public Task<UserInfo> GetUserInfo(CancellationToken _) => _platformUserModel.GetUserInfo();

        public async Task<string> GetAuthToken() {
#if LEGATO_STEAM_WEB_AUTH
            _lifetime.Token.ThrowIfCancellationRequested();
            var user = await _platformUserModel.GetUserInfo().ConfigureAwait(true);
            if (user.platform == UserInfo.Platform.Steam) {
                if (_ticketTask == null || _ticketTask.IsFaulted || _ticketTask.IsCanceled) _ticketTask = RequestSteamTicket();
                return await _ticketTask.ConfigureAwait(true);
            }
#endif
            return (await _platformUserModel.GetUserAuthToken().ConfigureAwait(true)).token;
        }

#if LEGATO_STEAM_WEB_AUTH
        private async Task<string> RequestSteamTicket() {
            var ticket = await SteamWebTicket.Create(string.Empty, TimeSpan.FromSeconds(15), _lifetime.Token).ConfigureAwait(true);
            if (_lifetime.IsCancellationRequested) {
                ticket.Dispose();
                _lifetime.Token.ThrowIfCancellationRequested();
            }
            _ticket = ticket;
            return ticket.Token;
        }
#endif

        public void Dispose() {
#if LEGATO_STEAM_WEB_AUTH
            if (_lifetime.IsCancellationRequested) return;
            _lifetime.Cancel();
            _ticket?.Dispose();
            _ticket = null;
#endif
        }

        public Task<string> GetCrossPlatformAccessToken(CancellationToken _) {
            var completion = new TaskCompletionSource<string>();
            Oculus.Platform.Users.GetAccessToken().OnComplete(message => completion.TrySetResult(message.IsError ? string.Empty : message.Data));
            return completion.Task;
        }

        public Task<IReadOnlyList<string>> GetFriendUserIds() => _platformUserModel.GetUserFriendsUserIds(false);
    }
}
