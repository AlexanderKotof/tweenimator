using System.Collections.Generic;
using Tweenimator.Runtime.AnimationData;
using Tweenimator.Runtime.Helpers;
using UnityEngine;

namespace Tweenimator.Runtime.Components
{
    public interface IAnimationComponent
    {
        List<ITweenAnimationClip> GetAnimationClips();

        SerializableDictionary<TargetPathKey, Object> AnimationTargets { get; }
    }
}
