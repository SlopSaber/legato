using CP_SDK;
using CP_SDK.Unity;
using CP_SDK_WebSocketSharp;
using System;
using System.Threading;
using UnityEngine;

namespace CP_SDK_BS.Game
{
    /// <summary>
    /// UserPlatform helper
    /// </summary>
    public static class UserPlatform
    {
        /// <summary>
        /// User ID cache
        /// </summary>
        private static string m_UserID = null;
        /// <summary>
        /// User name cache
        /// </summary>
        private static string m_UserName = null;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Get User ID
        /// </summary>
        /// <returns></returns>
        public static string GetUserID()
        {
            if (m_UserID != null)
                return m_UserID;

            if (MTMainThreadInvoker.IsMainThread())
                FetchPlatformInfos();
            else
            {
                var isReady = false;
                MTMainThreadInvoker.Enqueue(() =>
                {
                    FetchPlatformInfos();
                    isReady = true;
                });

                while (!isReady)
                    Thread.Sleep(TimeSpan.FromMilliseconds(1));
            }

            return m_UserID;
        }
        /// <summary>
        /// Get User Name
        /// </summary>
        /// <returns></returns>
        public static string GetUserName()
        {
            if (m_UserName != null)
                return m_UserName;

            if (MTMainThreadInvoker.IsMainThread())
                FetchPlatformInfos();
            else
            {
                var isReady = false;
                MTMainThreadInvoker.Enqueue(() =>
                {
                    FetchPlatformInfos();
                    isReady = true;
                });

                while (!isReady)
                    Thread.Sleep(TimeSpan.FromMilliseconds(1));
            }

            return m_UserName;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Find platform informations
        /// </summary>
        private static void FetchPlatformInfos()
        {
            try
            {
                foreach (var model in Resources.FindObjectsOfTypeAll<PlatformLeaderboardsModel>())
                {
                    var identity = Legato.Platform.PlatformAuthentication.FromLeaderboards(model).GetIdentity();
                    if (identity.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
                        SetIdentity(identity.Result);
                    else
                        _ = CompleteIdentity(identity);
                    if (m_UserID != null) return;
                }
            }
            catch (System.Exception l_Exception)
            {
                CP_SDK.ChatPlexSDK.Logger.Error("[CP_SDK_BS.Game][UserPlatform] Unable to find user platform informations");
                CP_SDK.ChatPlexSDK.Logger.Error(l_Exception);
            }
        }
        private static void SetIdentity(Legato.Platform.UserIdentity identity)
        {
            if (string.IsNullOrEmpty(identity.UserId) || identity.UserId == "0") return;
            m_UserID = identity.UserId;
            m_UserName = identity.DisplayName;
        }

        private static async System.Threading.Tasks.Task CompleteIdentity(System.Threading.Tasks.Task<Legato.Platform.UserIdentity> pending)
        {
            try { SetIdentity(await pending); }
            catch (Exception error) { CP_SDK.ChatPlexSDK.Logger.Error(error); }
        }
    }
}
