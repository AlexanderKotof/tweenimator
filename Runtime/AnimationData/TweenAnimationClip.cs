using System.Collections.Generic;
using Tweenimator.Runtime.Bindings;
using UnityEngine;

namespace Tweenimator.Runtime.AnimationData
{
    public class TweenAnimationClip : ScriptableObject, ITweenAnimationClip
    {
        [field: SerializeField, Tooltip("Name of animated object root.")]
        public string MainTarget { get; set; } = default!;

        [field: SerializeField, Header("Clips")] public List<AnimationTrack<bool>> BoolTracks { get; set; } = new();
        [field: SerializeField] public List<AnimationTrack<float>> FloatTracks { get; set; } = new();
        [field: SerializeField] public List<AnimationTrack<Vector2>> Vector2Tracks { get; set; } = new();
        [field: SerializeField] public List<AnimationTrack<Vector3>> Vector3Tracks { get; set; } = new();
        [field: SerializeField] public List<AnimationTrack<Color>> ColorTracks { get; set; } = new();

        [field: SerializeField, Min(0f), Space] public float StartDelay { get; set; }
        [field: SerializeField, Min(0f)] public float Duration { get; set; }
        [field: SerializeField] public bool Loop { get; set; }

        [field: SerializeField] public AnimationResetOption OnCancel { get; set; }
    }
}
