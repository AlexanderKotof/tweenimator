using System;
using UnityEngine;

namespace Tweenimator.Runtime.Bindings
{
    [Serializable]
    public class AnimationTrack<TValue> : AnimationBinding, IAnimationTrack<TValue>
    {
        [field: SerializeField] public KeyFrame<TValue>[] KeyFrames { get; set; }

        public AnimationTrack(string path, KeyFrame<TValue>[] values, Type targetType,
            AnimationBindingType animationType)
            : base(path, targetType, animationType)
        {
            KeyFrames = values;
        }

        public KeyFrame<TValue>[] GetKeyFrames()
        {
            return KeyFrames;
        }
    }
}
