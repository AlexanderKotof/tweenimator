using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Tweenimator.Runtime.AnimationData;
using UnityEngine;

namespace Tweenimator.Runtime.Components
{
    /// <summary>
    /// Component for play animation in runtime.
    /// Supports animations preview in editor
    /// </summary>
    public class TweenAnimationsComponent : TweenAnimationComponentBase
    {
        [SerializeField]
        private bool _playOnEnable;
        [SerializeField]
        private List<TweenAnimationClip> _animationsData = new();
        public List<TweenAnimationClip> Animations => _animationsData;

        private void OnEnable()
        {
            if (_playOnEnable)
            {
                PlayAnimation();
            }
        }

        public void PlayAnimation(int index = 0)
        {
            if (_animationsData.Count <= index || index < 0)
            {
                Debug.LogError($"Invalid animation index: {index}");
                return;
            }

            PlayAnimationAsync(_animationsData[index]).Forget();
        }

        public override List<ITweenAnimationClip> GetAnimationClips()
        {
            return _animationsData.Cast<ITweenAnimationClip>().ToList();
        }
    }
}
