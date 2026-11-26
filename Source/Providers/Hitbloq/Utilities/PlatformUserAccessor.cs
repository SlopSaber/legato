#if LEGATO_BEFORE_MULTIPLAYER
global using UserInfo = Hitbloq.Utilities.LegacyUserInfo;
#endif
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
#if LEGATO_BEFORE_MULTIPLAYER
using System;
using IPA.Utilities.Async;
#endif
#if HITBLOQ_BS_1_44_0
using OculusStudios.Platform.Core;
#endif

namespace Hitbloq.Utilities
{
#if LEGATO_BEFORE_MULTIPLAYER
	internal sealed class LegacyUserInfo
	{
		internal LegacyUserInfo(string id, PlatformInfo.Platform vendor)
		{
			platformUserId = id;
			platform = vendor;
		}

		internal readonly string platformUserId;
		internal readonly PlatformInfo.Platform platform;
	}
#endif

	internal class PlatformUserAccessor
	{
#if LEGATO_BEFORE_MULTIPLAYER
		private readonly PlatformUserModelSO _platformUserModel;

		public PlatformUserAccessor(PlatformUserModelSO platformUserModel)
		{
			_platformUserModel = platformUserModel;
		}

		public Task<UserInfo?> GetUserInfo(CancellationToken cancellationToken = default) =>
			AwaitRequest<UserInfo>(result => _platformUserModel.GetUserInfo((status, user) =>
				result.TrySetResult(status == PlatformUserModelSO.GetUserInfoResult.OK && !string.IsNullOrEmpty(user?.userId)
					? new UserInfo(user!.userId, _platformUserModel.platformInfo.platform) : null)), cancellationToken);

		public Task<IReadOnlyCollection<string>?> GetUserFriendsUserIds(bool includeSteamFriends) =>
			AwaitRequest<IReadOnlyCollection<string>>(result =>
			{
				if (!includeSteamFriends && _platformUserModel.platformInfo.platform == PlatformInfo.Platform.Steam)
				{
					result.TrySetResult(Array.Empty<string>());
					return null;
				}

				return _platformUserModel.GetUserFriendsUserIds(true, (status, ids) =>
					result.TrySetResult(status == PlatformUserModelSO.GetUserFriendsUserIdsResult.OK ? ids : null));
			}, CancellationToken.None);

		private static async Task<T?> AwaitRequest<T>(Func<TaskCompletionSource<T?>, HMAsyncRequest?> begin, CancellationToken token) where T : class
		{
			token.ThrowIfCancellationRequested();
			var result = new TaskCompletionSource<T?>(TaskCreationOptions.RunContinuationsAsynchronously);
			using var registration = token.Register(() => result.TrySetCanceled());
			var request = await UnityMainThreadTaskScheduler.Factory.StartNew(() => begin(result), token);
			try
			{
				return await result.Task;
			}
			catch (OperationCanceledException) when (request != null)
			{
				await UnityMainThreadTaskScheduler.Factory.StartNew(request.Cancel);
				throw;
			}
		}
#elif HITBLOQ_BS_1_44_0
		private readonly IPlatform _platform;

		public PlatformUserAccessor(IPlatform platform)
		{
			_platform = platform;
		}

		public Task<UserInfo?> GetUserInfo(CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var user = _platform.user;
			if (user == null || user.userId == 0)
			{
				return Task.FromResult<UserInfo?>(null);
			}

			return Task.FromResult<UserInfo?>(new UserInfo(GetPlatform(), user.userId.ToString(), user.displayName));
		}

		public Task<IReadOnlyCollection<string>?> GetUserFriendsUserIds(bool includeSteamFriends)
		{
			return Task.FromResult<IReadOnlyCollection<string>?>(null);
		}

		private UserInfo.Platform GetPlatform()
		{
			return _platform.vendor switch
			{
				Vendor.Valve => UserInfo.Platform.Steam,
				Vendor.Meta => UserInfo.Platform.Oculus,
				Vendor.Sony => UserInfo.Platform.PS5,
				_ => UserInfo.Platform.Test
			};
		}
#elif HITBLOQ_BS_1_29_1
		private readonly IPlatformUserModel _platformUserModel;

		public PlatformUserAccessor(IPlatformUserModel platformUserModel)
		{
			_platformUserModel = platformUserModel;
		}

		public async Task<UserInfo?> GetUserInfo(CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return await _platformUserModel.GetUserInfo();
		}

		public async Task<IReadOnlyCollection<string>?> GetUserFriendsUserIds(bool includeSteamFriends)
		{
			var userIds = await _platformUserModel.GetUserFriendsUserIds(includeSteamFriends);
			return userIds is IReadOnlyCollection<string> collection ? collection : new List<string>(userIds);
		}
#elif HITBLOQ_BS_1_45_0
		public PlatformUserAccessor()
		{
		}

		public async Task<UserInfo?> GetUserInfo(CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var userInfo = await BS_Utils.Gameplay.GetUserInfo.GetUserAsync();
			cancellationToken.ThrowIfCancellationRequested();
			return userInfo;
		}

		public Task<IReadOnlyCollection<string>?> GetUserFriendsUserIds(bool includeSteamFriends)
		{
			return Task.FromResult<IReadOnlyCollection<string>?>(null);
		}

#else
		private readonly IPlatformUserModel _platformUserModel;

		public PlatformUserAccessor(IPlatformUserModel platformUserModel)
		{
			_platformUserModel = platformUserModel;
		}

		public async Task<UserInfo?> GetUserInfo(CancellationToken cancellationToken = default)
		{
			var user = await _platformUserModel.GetUserInfo(cancellationToken);
			if (user == null || string.IsNullOrEmpty(user.platformUserId))
			{
				return null;
			}

			return user;
		}

		public async Task<IReadOnlyCollection<string>?> GetUserFriendsUserIds(bool includeSteamFriends)
		{
			var userIds = await _platformUserModel.GetUserFriendsUserIds(includeSteamFriends);
			return userIds is IReadOnlyCollection<string> collection ? collection : new List<string>(userIds);
		}
#endif
	}
}
