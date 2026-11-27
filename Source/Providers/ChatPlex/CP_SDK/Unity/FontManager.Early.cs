#if LEGATO_EARLY_STANDARD_MAPS
using System;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace CP_SDK.Unity
{
    /// <summary>
    /// TextMeshPro font provider for games whose TextMeshPro cannot create dynamic font assets
    /// </summary>
    public static class FontManager
    {
        private const string GameFontName = "Teko-Medium SDF";

        private static Func<TMP_FontAsset, TMP_FontAsset> m_TMPFontAssetSetup;
        private static TMP_FontAsset m_MainFont;

        public static Task      SystemFontLoadTask  { get; private set; } = Task.CompletedTask;
        public static bool      IsInitialized       { get; private set; } = true;
        public static string    MainFontName        => GameFontName;
        public static string    ChatFontName        => GameFontName;

        internal static void Setup(Func<TMP_FontAsset, TMP_FontAsset> p_TMPFontAssetSetup)
            => m_TMPFontAssetSetup = p_TMPFontAssetSetup;

        internal static Task Init() => Task.CompletedTask;

        internal static void ApplyPlatformFixes()
        {
        }

        /// <summary>
        /// Get main font, a copy of the game's font with its own material
        /// </summary>
        public static TMP_FontAsset GetMainFont()
        {
            if (m_MainFont)
                return m_MainFont;

            var l_GameFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(x => x && x.name == GameFontName)
                          ?? Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(x => x);
            if (!l_GameFont)
                return null;

            m_MainFont = UnityEngine.Object.Instantiate(l_GameFont);
            m_MainFont.name = l_GameFont.name + " Clone";
            if (l_GameFont.material)
                m_MainFont.material = new Material(l_GameFont.material) { name = l_GameFont.material.name + " Clone" };

            if (m_TMPFontAssetSetup != null)
                m_MainFont = m_TMPFontAssetSetup(m_MainFont);

            return m_MainFont;
        }
        /// <summary>
        /// Get chat font
        /// </summary>
        public static TMP_FontAsset GetChatFont() => GetMainFont();

        /// <summary>
        /// Find a loaded font asset by family name
        /// </summary>
        public static bool TryGetTMPFontAssetByFamily(string p_Family, out TMP_FontAsset p_Font, string p_SubFamily = null, bool p_FallbackIfNoSubfamily = false, bool p_SetupOsFallbacks = true)
        {
            p_Font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                .FirstOrDefault(x => x && !string.IsNullOrEmpty(p_Family) && x.name.StartsWith(p_Family, StringComparison.OrdinalIgnoreCase));
            return p_Font;
        }
        /// <summary>
        /// Find a loaded font asset by full name
        /// </summary>
        public static bool TryGetTMPFontAssetByFullName(string p_FullName, out TMP_FontAsset p_Font, bool p_SetupOsFallbacks = true)
            => TryGetTMPFontAssetByFamily(p_FullName, out p_Font);
        /// <summary>
        /// Font files cannot be converted by this TextMeshPro version
        /// </summary>
        public static bool AddFontFile(string p_Path, out string p_FamilyName)
        {
            p_FamilyName = string.Empty;
            return false;
        }
    }
}
#endif
