using System.Collections.Generic;
using Tweenimator.Runtime.Bindings;
using UnityEngine;

namespace Tweenimator.Runtime.AnimationData
{
    public interface ITweenAnimationClip
    {
        string MainTarget { get; }

        List<AnimationTrack<bool>> BoolTracks { get; }
        List<AnimationTrack<float>> FloatTracks { get; }
        List<AnimationTrack<Vector2>> Vector2Tracks { get; }
        List<AnimationTrack<Vector3>> Vector3Tracks { get; }
        List<AnimationTrack<Color>> ColorTracks { get; }

        float StartDelay { get; }
        float Duration { get; }
        bool Loop { get; }

        AnimationResetOption OnCancel { get; }
    }
}
