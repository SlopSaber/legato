using System.Collections.Generic;
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
using System.Collections.Concurrent;
#endif

namespace SongCore
{
    public partial class Loader
    {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        internal static readonly IDictionary<string, NativeLoadedSaveData> NativeSaveStore = new ConcurrentDictionary<string, NativeLoadedSaveData>();
#else
        internal static IDictionary<string, NativeLoadedSaveData> NativeSaveStore => CustomLevelLoader._loadedBeatmapSaveData;
#endif
    }
}
