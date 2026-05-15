using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Tweenimator.Runtime.AnimationData;
using Tweenimator.Runtime.Helpers;
using UnityEngine;
using AnimationState = Tweenimator.Runtime.AnimationData.AnimationState;
using Object = UnityEngine.Object;

namespace Tweenimator.Runtime.Components
{
    /// <summary>
    /// Component for play animation in runtime.
    /// </summary>
    public abstract class TweenAnimationComponentBase : MonoBehaviour, IAnimationComponent
    {
        // list of animation targets
        // fills in editor on prefab validation
        [field: SerializeField, HideInInspector]
        public SerializableDictionary<TargetPathKey, Object> AnimationTargets { get; private set; } = new();

        private AnimationState? _animationState;

        private void OnDestroy()
        {
            TryCancelAnimation();
        }

        public abstract List<ITweenAnimationClip> GetAnimationClips();

        protected UniTask PlayAnimationAsync(ITweenAnimationClip animationClip)
        {
            if (_animationState == null)
            {
                _animationState = new AnimationState(AnimationTargets);
            }
            else
            {
                _animationState.Cancel();
                _animationState.ClearState();
            }

            return TweenAnimationPlayer.PlayAsync(animationClip, _animationState);
        }

        public void TryCancelAnimation()
        {
            _animationState?.Cancel();
        }

        public void ResetState()
        {
            _animationState?.Reset();
        }
    }
}
