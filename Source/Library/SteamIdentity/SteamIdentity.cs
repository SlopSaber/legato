namespace Legato.Platform {
    internal static class SteamIdentity {
        internal static string UserId() => Steamworks.SteamUser.GetSteamID().m_SteamID.ToString();
    }
}
