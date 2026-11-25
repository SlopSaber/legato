using System;

namespace CustomJSONData.CustomBeatmap
{
    public class CustomObstacleData : ObstacleData, ICustomData, IVersionable
    {
        public CustomObstacleData(
            float time,
#if LEGATO_BEATMAP_OBJECT_BEAT
            float startBeat,
            float endBeat,
#endif
#if LEGATO_BEATMAP_OBJECT_ROTATION
            int rotation,
#endif
            int lineIndex,
            NoteLineLayer lineLayer,
            float duration,
            int width,
            int height,
            CustomData customData,
            Version version)
            : base(
                time,
#if LEGATO_BEATMAP_OBJECT_BEAT
                startBeat,
                endBeat,
#endif
#if LEGATO_BEATMAP_OBJECT_ROTATION
                rotation,
#endif
                lineIndex,
                lineLayer,
                duration,
                width,
                height)
        {
            this.customData = customData;
            this.version = version;
        }

        public CustomData customData { get; }

        public Version version { get; }

        public override BeatmapDataItem GetCopy()
        {
            return new CustomObstacleData(
                time,
#if LEGATO_BEATMAP_OBJECT_BEAT
                beat,
                endBeat,
#endif
#if LEGATO_BEATMAP_OBJECT_ROTATION
                rotation,
#endif
                lineIndex,
                lineLayer,
                duration,
                width,
                height,
                customData.Copy(),
                version);
        }
    }
}
