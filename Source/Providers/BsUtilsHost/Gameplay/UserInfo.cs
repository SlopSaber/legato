using Steamworks;
using System;
using UnityEngine;
using Logger = BS_Utils.Utilities.Logger;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BS_Utils.Utilities;
#if LEGATO_BEFORE_MULTIPLAYER
using IPlatform = global::PlatformUserModelSO;
using UserInfo = global::PlatformUserModelSO.UserInfo;
using UserPlatform = global::PlatformInfo.Platform;
#elif LEGATO_PLATFORM_USER_MODEL
using IPlatform = global::IPlatformUserModel;
using UserPlatform = global::UserInfo.Platform;
#else
using OculusStudios.Platform.Core;
using OculusStudios.Platform.Steam;
using UserPlatform = global::UserInfo.Platform;
#endif

namespace BS_Utils.Gameplay
{
    public static class GetUserInfo
    {
        private static readonly TaskCompletionSource<bool> shouldBeReadyTask = new TaskCompletionSource<bool>();
        private static readonly object getUserLock = new object();

        static string userName = null;
        static string userID = null;
        static UserPlatform platform;
        static Texture2D userAvatar = null;
        private static Task<UserInfo> getUserTask;
        private static bool isReady => shouldBeReadyTask.Task.IsCompleted;
        private static IPlatform _platformUserModel;

        internal static void TriggerReady()
        {
            shouldBeReadyTask.TrySetResult(true);
        }

        public static IPlatform GetPlatformUserModel()
        {
            return _platformUserModel ?? SetPlatformUserModel();
        }

        internal static IPlatform SetPlatformUserModel()
        {
            if (_platformUserModel != null)
                return _platformUserModel;
            try
            {
                // Need to check for null because there's multiple PlatformLeaderboardsModels (at least sometimes), and one has a null IPlatformUserModel with 'vrmode oculus'
                var leaderboardsModel = Legato.Unity.ObjectDiscovery.FindLoaded<PlatformLeaderboardsModel>().LastOrDefault(p => NativePlatform(p) != null);
                if (leaderboardsModel == null)
                {
                    Logger.log.Error("Could not find a 'PlatformLeaderboardsModel', GetUserInfo unavailable.");
                    return null;
                }

                _platformUserModel = NativePlatform(leaderboardsModel);
            }
            catch (Exception ex)
            {
                Logger.log.Error($"Error getting 'IPlatformUserModel', GetUserInfo unavailable: {ex.Message}");
                Logger.log.Debug(ex);
            }
            return _platformUserModel;
        }

        private static IPlatform NativePlatform(PlatformLeaderboardsModel model)
        {
#if LEGATO_PLATFORM_USER_MODEL
            return model._platformUserModel;
#else
            return model._platform;
#endif
        }

        public static async void UpdateUserInfo()
        {
            try
            {
                await GetUserAsync();
            }
            catch (Exception ex)
            {
                Logger.log.Error($"Error retrieving UserInfo: {ex.Message}.");
                Logger.log.Debug(ex);
            }
        }

        /// <summary>
        /// Attempts to retrieve the UserInfo. Returns null if <see cref="IPlatform"/> is unavailable or its identity provider is unsupported.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="IPlatform"/> returns null for the <see cref="UserInfo"/>.</exception>
        public static async Task<UserInfo> GetUserAsync()
        {
            try
            {
                if (!isReady)
                    await shouldBeReadyTask.Task;
                await Task.Delay(200);
                lock (getUserLock)
                {

                    IPlatform platformUserModel = GetPlatformUserModel();
                    if (platformUserModel == null)
                    {
                        Logger.log.Error("IPlatformUserModel not found, cannot update user info.");
                        return null;
                    }
                    if (getUserTask == null || getUserTask.Status == TaskStatus.Faulted)
                        getUserTask = InternalGetUserAsync();
                }
                return await getUserTask;
            }
            catch (Exception ex)
            {
                Logger.log.Error($"Error retrieving UserInfo: {ex.Message}.");
                Logger.log.Debug(ex);
                throw;
            }
        }

        private static async Task<UserInfo> InternalGetUserAsync()
        {
#if LEGATO_BEFORE_MULTIPLAYER
            UserInfo userInfo = await new Legato.Platform.PlatformAuthentication(_platformUserModel).GetGameUserInfo(CancellationToken.None);
#elif BEAT_SABER_1_29_0
            UserInfo userInfo = await _platformUserModel.GetUserInfo();
#elif LEGATO_PLATFORM_USER_MODEL
            UserInfo userInfo = await _platformUserModel.GetUserInfo(CancellationToken.None);
#else
            // taken from NetworkPlayerModel<T>.InitAuthenticationTokenProvider()
            var platformKey = _platformUserModel.key;
            UserInfo.Platform? identityPlatform = platformKey switch
            {
                "steam" => UserInfo.Platform.Steam,
                "oculus" => UserInfo.Platform.Oculus,
                "oculus-mock" => UserInfo.Platform.Oculus,
                "mock" => UserInfo.Platform.Test,
                _ => (UserInfo.Platform?)null,
            };
            if (!identityPlatform.HasValue)
            {
                Logger.log.Warn($"Identity provider '{platformKey ?? "<null>"}' is unsupported; user info is unavailable.");
                return null;
            }

            UserInfo userInfo = new UserInfo(identityPlatform.Value, _platformUserModel.user.userId.ToString(), _platformUserModel.user.displayName);

#endif

            if (userInfo != null)
            {
                userName = userInfo.userName;
#if LEGATO_BEFORE_MULTIPLAYER
                userID = userInfo.userId;
                platform = _platformUserModel.platformInfo.platform;
#else
                userID = userInfo.platformUserId;
                platform = userInfo.platform;
#endif
                Logger.log.Debug($"UserInfo found: {userID}: {userName} on {platform}");
                if (platform == UserPlatform.Steam)
                    GetSteamAvatar();
                else if (platform == UserPlatform.Oculus)
                    userAvatar = UIUtilities.LoadTextureFromResources("BS_Utils.Resources.oculus.png");
            }
            else
                throw new InvalidOperationException("UserInfo is null.");
            return userInfo;
        }

        private static void GetSteamAvatar()
        {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            if (SteamManager.Initialized)
#elif LEGATO_PLATFORM_USER_MODEL
            if (_platformUserModel is SteamPlatformUserModel steamModel && steamModel._platformInit.IsInitialized)
#else
            var steamPlatformUserModel = (SteamPlatform)_platformUserModel;
            if (steamPlatformUserModel.isInitialized)
#endif
            {
                var steamUser = SteamUser.GetSteamID();
                userAvatar = GetAvatar(steamUser);
            }
        }
        private static Texture2D GetAvatar(CSteamID steamUser)
        {
            int avatarInt = SteamFriends.GetLargeFriendAvatar(steamUser);
            bool success = SteamUtils.GetImageSize(avatarInt, out uint imageWidth, out uint imageHeight);

            if (success && imageWidth > 0 && imageHeight > 0)
            {
                byte[] Image = new byte[imageWidth * imageHeight * 4];
                Texture2D returnTexture = new Texture2D((int)imageWidth, (int)imageHeight, TextureFormat.RGBA32, false, true);
                success = SteamUtils.GetImageRGBA(avatarInt, Image, (int)(imageWidth * imageHeight * 4));

                if (success)
                {
                    returnTexture.LoadRawTextureData(Image);
                    returnTexture.Apply();
                }

                return returnTexture;
            }
            else
            {
                Debug.LogError("Couldn't get avatar.");
                return new Texture2D(0, 0);
            }
        }

        [Obsolete("This is only valid after GetUserAsync() returns non-null. Use 'GetUserAsync()' instead.")]
        public static UserPlatform GetPlatformInfo()
        {
            return platform;
        }

        [Obsolete("This will return null until BS_Utils has finished retrieving the UserInfo. Use 'GetUserAsync()' instead.")]
        public static string GetUserName()
        {
            return userName;
        }

        [Obsolete("This will return null until BS_Utils has finished retrieving the UserInfo. Use 'GetUserAsync()' instead.")]
        public static string GetUserID()
        {
            return userID;
        }

        public static Texture2D GetUserAvatar()
        {
            return userAvatar;
        }
    }
}
