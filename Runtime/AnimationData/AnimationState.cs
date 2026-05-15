using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tweenimator.Runtime.AnimationData
{
    /// <summary>
    /// Represents state of animation
    /// </summary>
    public class AnimationState
    {
        public readonly List<UniTask> Tasks = new List<UniTask>();
        public readonly List<MotionHandle> Motions = new List<MotionHandle>();

        public readonly List<Action> ResetAnimationCallbacks = new List<Action>();

        public readonly IReadOnlyDictionary<TargetPathKey, Object> AnimationTargets;

        public bool IsInLoop;

        public CancellationTokenSource CancellationTokenSource { get; private set; }
        public ITweenAnimationClip? CurrentClip { get; set; }
        public bool IsRunning { get; set; }

        public AnimationState(IReadOnlyDictionary<TargetPathKey, Object> animationTargets)
        {
            AnimationTargets = animationTargets;
            CancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Cancel animation
        /// </summary>
        public void Cancel()
        {
            CancellationTokenSource.Cancel();

            foreach (var motion in Motions)
            {
                motion.TryCancel();
            }

            if (CurrentClip != null &&
                CurrentClip.OnCancel != AnimationResetOption.None)
            {
                Reset();
            }

            ClearState();
        }

        /// <summary>
        /// Reset affected objects to initial state
        /// </summary>
        public void Reset()
        {
            if (ResetAnimationCallbacks.Count == 0)
            {
                // Debug.Log("Reset callbacks is empty. Reset option in animation clip should be set.");
                return;
            }

            foreach (var callback in ResetAnimationCallbacks)
            {
                callback?.Invoke();
            }

            ResetAnimationCallbacks.Clear();
        }

        /// <summary>
        /// Clearing previous played animation data
        /// </summary>
        public void ClearState()
        {
            Tasks.Clear();
            Motions.Clear();
            ResetAnimationCallbacks.Clear();

            IsInLoop = false;

            CancellationTokenSource = new CancellationTokenSource();
        }

        public void AddResetCallback(Action action) => ResetAnimationCallbacks.Add(action);
    }
}
