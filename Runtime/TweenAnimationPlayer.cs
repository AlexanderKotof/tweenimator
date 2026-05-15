using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Adapters;
using LitMotion.Extensions;
using Tweenimator.Runtime.AnimationData;
using Tweenimator.Runtime.Bindings;
using Tweenimator.Runtime.CustomAdapters;
using UnityEngine;
using UnityEngine.UI;
using AnimationState = Tweenimator.Runtime.AnimationData.AnimationState;
using Object = UnityEngine.Object;

namespace Tweenimator.Runtime
{
    public static class TweenAnimationPlayer
    {
        private static readonly string Tag = $"[{nameof(TweenAnimationPlayer)}]";
        private const int DefaultStorageCapacity = 256;

        static TweenAnimationPlayer()
        {
            MotionDispatcher.EnsureStorageCapacity<bool, NoOptions, BoolMotionAdapter>(DefaultStorageCapacity);
            MotionDispatcher.EnsureStorageCapacity<float, NoOptions, FloatMotionAdapter>(DefaultStorageCapacity);
            MotionDispatcher.EnsureStorageCapacity<Vector2, NoOptions, Vector2MotionAdapter>(DefaultStorageCapacity);
            MotionDispatcher.EnsureStorageCapacity<Vector3, NoOptions, Vector3MotionAdapter>(DefaultStorageCapacity);
            MotionDispatcher.EnsureStorageCapacity<Color, NoOptions, ColorMotionAdapter>(DefaultStorageCapacity);
        }

        public static async UniTask PlayAsync(ITweenAnimationClip animationClip, AnimationState state, CancellationToken cancellationToken)
        {
            if (EnsureClipValid(animationClip, state) == false)
            {
#if UNITY_EDITOR
                var clipObj = animationClip as Object;
                Debug.LogError($"Can't play animation clip {clipObj?.name} because no valid targets found! Check your setup, press Validate button.", clipObj);
#endif
                return;
            }

            state.CurrentClip = animationClip;

            do
            {
                ClearStateLists(state);
                FillAnimationMotionsTasks(animationClip, state, cancellationToken);

                state.IsRunning = true;

                await UniTask.WhenAll(state.Tasks)
                    .SuppressCancellationThrow();

                if (cancellationToken.IsCancellationRequested)
                {
                    state.IsRunning = false;
                    return;
                }

                state.IsRunning = animationClip.Loop ? true : false;
                state.IsInLoop = animationClip.Loop;
            }
            while (animationClip.Loop && cancellationToken is { IsCancellationRequested: false });
        }

        public static UniTask PlayAsync(ITweenAnimationClip animationClip, AnimationState state)
        {
            return PlayAsync(animationClip, state, state.CancellationTokenSource.Token);
        }

        private static bool EnsureClipValid(ITweenAnimationClip animationClip, AnimationState state)
        {
            foreach (AnimationTrack<bool> binding in animationClip.BoolTracks)
            {
                if (EnsureValidity(binding) == false)
                {
                    continue;
                }

                return true;
            }
            foreach (AnimationTrack<float> binding in animationClip.FloatTracks)
            {
                if (EnsureValidity(binding) == false)
                {
                    continue;
                }

                return true;
            }
            foreach (AnimationTrack<Vector2> binding in animationClip.Vector2Tracks)
            {
                if (EnsureValidity(binding) == false)
                {
                    continue;
                }

                return true;
            }
            foreach (AnimationTrack<Vector3> binding in animationClip.Vector3Tracks)
            {
                if (EnsureValidity(binding) == false)
                {
                    continue;
                }

                return true;
            }
            foreach (AnimationTrack<Color> binding in animationClip.ColorTracks)
            {
                if (EnsureValidity(binding) == false)
                {
                    continue;
                }

                return true;
            }

            return false;

            bool EnsureValidity(AnimationBinding binding)
            {
                return state.AnimationTargets.TryGetValue(new TargetPathKey(binding), out var target) &&
                       binding.Type == target.GetType();
            }
        }

        private static void FillAnimationMotionsTasks(ITweenAnimationClip animationClip,
            AnimationState state,
            CancellationToken cancellationToken)
        {
            foreach (AnimationTrack<bool> binding in animationClip.BoolTracks)
            {
                UniTask task = ProcessBoolBinding(binding, state, animationClip, cancellationToken);
                state.Tasks.Add(task);
            }
            foreach (AnimationTrack<float> binding in animationClip.FloatTracks)
            {
                UniTask task = ProcessFloatBinding(binding, state, animationClip, cancellationToken);
                state.Tasks.Add(task);
            }
            foreach (AnimationTrack<Vector2> binding in animationClip.Vector2Tracks)
            {
                UniTask task = ProcessVector2Binding(binding, state, animationClip, cancellationToken);
                state.Tasks.Add(task);
            }
            foreach (AnimationTrack<Vector3> binding in animationClip.Vector3Tracks)
            {
                UniTask task = ProcessVector3Binding(binding, state, animationClip, cancellationToken);
                state.Tasks.Add(task);
            }
            foreach (AnimationTrack<Color> binding in animationClip.ColorTracks)
            {
                UniTask task = ProcessColorBinding(binding,  state, animationClip, cancellationToken);
                state.Tasks.Add(task);
            }
        }

        private static void ClearStateLists(AnimationState state)
        {
            state.Tasks.Clear();
            state.Motions.Clear();
        }

        private static UniTask ProcessBoolBinding(AnimationTrack<bool> binding,
            AnimationState state, ITweenAnimationClip animationClip, CancellationToken cancellationToken)
        {
            switch (binding.BindingType)
            {
                case AnimationBindingType.ComponentActivity:
                    return CreateComponentActivityMotion(binding, state, animationClip, cancellationToken);
                case AnimationBindingType.GameObjectActivity:
                    return CreateGameObjectActivityMotion(binding, state, animationClip, cancellationToken);

                default:
#if UNITY_EDITOR
                    Debug.LogError($"{Tag} Not supported binding type {binding.BindingType} for Bool binding.");
#endif
                    break;
            }

            return UniTask.CompletedTask;
        }
        private static UniTask ProcessFloatBinding(AnimationTrack<float> binding,
            AnimationState state, ITweenAnimationClip animationClip, CancellationToken cancellationToken)
        {
            switch (binding.BindingType)
            {
                case AnimationBindingType.CanvasGroupAlpha:
                    return CreateCanvasGroupAlphaMotion(binding, state, animationClip, cancellationToken);

                case AnimationBindingType.PixelsPerUnit:
                    return CreatePixelsPerUnitMotion(binding, state, animationClip, cancellationToken);

                default:
#if UNITY_EDITOR
                    Debug.LogError($"{Tag} Not supported binding type {binding.BindingType} for Float binding.");
#endif
                    break;
            }

            return UniTask.CompletedTask;
        }

        private static UniTask ProcessVector3Binding(AnimationTrack<Vector3> binding,
            AnimationState state, ITweenAnimationClip animationClip, CancellationToken cancellationToken)
        {
            switch (binding.BindingType)
            {
                case AnimationBindingType.LocalPosition:
                    return CreatePositionMotion(binding, state, animationClip, cancellationToken);
                case AnimationBindingType.LocalRotation:
                    return CreateRotationMotion(binding, state, animationClip, cancellationToken);
                case AnimationBindingType.LocalScale:
                    return CreateScaleMotion(binding, state, animationClip, cancellationToken);

                default:
#if UNITY_EDITOR
                    Debug.LogError($"{Tag} Not supported binding type {binding.BindingType} for Vector3 binding.");
#endif
                    break;
            }
            return UniTask.CompletedTask;
        }
        private static UniTask ProcessVector2Binding(AnimationTrack<Vector2> binding,
            AnimationState state, ITweenAnimationClip animationClip, CancellationToken cancellationToken)
        {
            switch (binding.BindingType)
            {
                case AnimationBindingType.SizeDelta:
                    return CreateSizeDeltaMotion(binding, state, animationClip, cancellationToken);
                case AnimationBindingType.AnchoredPosition:
                    return CreateAnchoredPositionMotion(binding, state, animationClip, cancellationToken);

                default:
#if UNITY_EDITOR
                    Debug.LogError($"{Tag} Not supported binding type {binding.BindingType} for Vector2 binding.");
#endif
                    break;
            }
            return UniTask.CompletedTask;
        }
        private static UniTask ProcessColorBinding(AnimationTrack<Color> binding,
            AnimationState state,ITweenAnimationClip animationClip, CancellationToken cancellationToken)
        {
            switch (binding.BindingType)
            {
                case AnimationBindingType.GraphicColor:
                    return CreateGraphicColorMotion(binding, state, animationClip, cancellationToken);

                default:
#if UNITY_EDITOR
                    Debug.LogError($"{Tag} Not supported binding type {binding.BindingType} for Color binding.");
#endif
                    break;
            }
            return UniTask.CompletedTask;
        }

        private static UniTask CreatePositionMotion(AnimationTrack<Vector3> track,
            AnimationState state, ITweenAnimationClip animationClip, CancellationToken cancellationToken)
        {
            return CreateMotion<AnimationTrack<Vector3>, Transform, Vector3, NoOptions, Vector3MotionAdapter>(track,
                state,
                animationClip,
                cancellationToken,
                (motionBuilder, target) => motionBuilder.BindToLocalPosition(target),
                (target, value) => target.localPosition = value);
        }
        private static UniTask CreateRotationMotion(AnimationTrack<Vector3> track,
            AnimationState state, ITweenAnimationClip animationClip, CancellationToken cancellationToken)
        {
            return CreateMotion<AnimationTrack<Vector3>, Transform, Vector3, NoOptions, Vector3MotionAdapter>(track,
                state,
                animationClip,
                cancellationToken,
                (motionBuilder, target) => motionBuilder.BindToLocalEulerAngles(target),
                (target, value) => target.localEulerAngles = value);
        }
        private static UniTask CreateScaleMotion(AnimationTrack<Vector3> track,
            AnimationState state,ITweenAnimationClip animationClip, CancellationToken cancellationToken)
        {
            return CreateMotion<AnimationTrack<Vector3>, Transform, Vector3, NoOptions, Vector3MotionAdapter>(track,
                state,
                animationClip,
                cancellationToken,
                (motionBuilder, target) => motionBuilder.BindToLocalScale(target),
                (target, value) => target.localScale = value);
        }
        private static UniTask CreateSizeDeltaMotion(AnimationTrack<Vector2> track,
            AnimationState state, ITweenAnimationClip animationClip, CancellationToken cancellationToken)
        {
            return CreateMotion<AnimationTrack<Vector2>, RectTransform, Vector2, NoOptions, Vector2MotionAdapter>(track,
                state,
                animationClip,
                cancellationToken,
                (motionBuilder, target) => motionBuilder.BindToSizeDelta(target),
                (target, value) => target.sizeDelta = value);
        }
        private static UniTask CreateAnchoredPositionMotion(AnimationTrack<Vector2> track,
            AnimationState state, ITweenAnimationClip animationClip, CancellationToken cancellationToken)
        {
            return CreateMotion<AnimationTrack<Vector2>, RectTransform, Vector2, NoOptions, Vector2MotionAdapter>(track,
                state,
                animationClip,
                cancellationToken,
                (motionBuilder, target) => motionBuilder.BindToAnchoredPosition(target),
                (target, value) => target.anchoredPosition = value);
        }
        private static UniTask CreateComponentActivityMotion(AnimationTrack<bool> track,
            AnimationState state, ITweenAnimationClip animationClip, CancellationToken cancellationToken)
        {
            return CreateMotion<AnimationTrack<bool>, Behaviour, bool, NoOptions, BoolMotionAdapter>(track,
                state,
                animationClip,
                cancellationToken,
                (motionBuilder, target) =>
                {
                    return motionBuilder.Bind(target, static (x, target) =>
                    {
                        target.enabled = x;
                    });
                },
                (target, value) => target.enabled = value);
        }
        private static UniTask CreateGameObjectActivityMotion(AnimationTrack<bool> track,
            AnimationState state, ITweenAnimationClip animationClip, CancellationToken cancellationToken)
        {
            return CreateMotion<AnimationTrack<bool>, GameObject, bool, NoOptions, BoolMotionAdapter>(track,
                state,
                animationClip,
                cancellationToken,
                (motionBuilder, target) =>
                {
                    return motionBuilder.Bind(target, static (value, target) =>
                    {
                        target.SetActive(value);
                    });
                },
                (target, value) => target.SetActive(value));
        }
        private static UniTask CreateGraphicColorMotion(AnimationTrack<Color> track,
            AnimationState state, ITweenAnimationClip animationClip, CancellationToken cancellationToken)
        {
            return CreateMotion<AnimationTrack<Color>, Graphic, Color, NoOptions, ColorMotionAdapter>(track,
                state,
                animationClip,
                cancellationToken,
                (motionBuilder, target) => motionBuilder.BindToColor(target),
                (target, value ) => target.color = value);
        }
        private static UniTask CreateCanvasGroupAlphaMotion(AnimationTrack<float> track,
            AnimationState state, ITweenAnimationClip animationClip, CancellationToken cancellationToken)
        {
            return CreateMotion<AnimationTrack<float>, CanvasGroup, float, NoOptions, FloatMotionAdapter>(track,
                state,
                animationClip,
                cancellationToken,
                (motionBuilder, target) => motionBuilder.BindToAlpha(target),
                (target, value) => target.alpha = value);
        }
        private static UniTask CreatePixelsPerUnitMotion(AnimationTrack<float> track,
            AnimationState state, ITweenAnimationClip animationClip, CancellationToken cancellationToken)
        {
            return CreateMotion<AnimationTrack<float>, Image, float, NoOptions, FloatMotionAdapter>(track,
                state,
                animationClip,
                cancellationToken,
                (motionBuilder, target) => motionBuilder.Bind(target, static (x, target) =>
                {
                    target.pixelsPerUnitMultiplier = x;
                }),
                (target, value) => target.pixelsPerUnitMultiplier = value);
        }

        private static async UniTask CreateMotion<TAnimationTrack, TTarget, TValue, TOptions, TAdapter>(
            TAnimationTrack track,
            AnimationState state,
            ITweenAnimationClip animationClip,
            CancellationToken cancellationToken,
            Func<MotionBuilder<TValue, TOptions, TAdapter>, TTarget, MotionHandle> bindHandler,
            Action<TTarget, TValue> resetHandler
        )
            where TTarget : Object
            where TAnimationTrack : AnimationBinding, IAnimationTrack<TValue>
            where TValue : unmanaged
            where TOptions : unmanaged, IMotionOptions
            where TAdapter : unmanaged, IMotionAdapter<TValue, TOptions>
        {
            if (!TryGetAnimationTarget<TAnimationTrack, TTarget>(track, state, out var bindingTarget))
                return;


            KeyFrame<TValue>[] keyFrames = track.GetKeyFrames();
            if (keyFrames.Length == 1)
            {
                KeyFrame<TValue> keyFrame = keyFrames[0];
                float startDelay = animationClip.StartDelay + animationClip.Duration * keyFrame.Time;

                MotionBuilder<TValue, TOptions, TAdapter> motion = CreateMotionFromKeys<TValue, TOptions, TAdapter>(
                    keyFrame, null, startDelay, animationClip.Duration * (1 - keyFrame.Time));
                MotionHandle handle = bindHandler.Invoke(motion, bindingTarget);

                state.Motions.Add(handle);

                await handle;
                return;
            }

            for (int index = 0; index < keyFrames.Length - 1; index++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                KeyFrame<TValue> from = keyFrames[index];
                KeyFrame<TValue> to = keyFrames[index + 1];

                float startDelay = index == 0 ? animationClip.StartDelay + animationClip.Duration * from.Time : 0;
                float duration = animationClip.Duration * (to.Time - from.Time);

                MotionBuilder<TValue, TOptions, TAdapter> motion = CreateMotionFromKeys<TValue, TOptions, TAdapter>(from, to, startDelay, duration);
                MotionHandle handle = bindHandler.Invoke(motion, bindingTarget);

                state.Motions.Add(handle);

                await handle;
            }
        }

        private static bool TryGetAnimationTarget<TAnimationTrack, TTarget>(
            TAnimationTrack track, AnimationState state, out TTarget bindingTarget)
            where TTarget : Object
            where TAnimationTrack : AnimationBinding
        {
            bindingTarget = null;
            if (state.AnimationTargets.TryGetValue(new TargetPathKey(track), out Object target) == false)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"{Tag} Can't play track! Target not found for binding {track.BindingType}!");
#endif
                return false;
            }

            if (target is not TTarget)
            {
#if UNITY_EDITOR
                Debug.LogError($"{Tag} Target of track {track.BindingType} is not typeof {typeof(TTarget)}!", target);
#endif
                return false;
            }

            bindingTarget = (TTarget)target;
            return true;
        }

        private static void SetupResetCallbacks<TAnimationTrack, TTarget, TValue>(
            ITweenAnimationClip data,
            TAnimationTrack track,
            AnimationState state,
            Action<TTarget, TValue> resetAction)
            where TAnimationTrack : AnimationBinding, IAnimationTrack<TValue>
            where TTarget : Object
        {
            if (!TryGetAnimationTarget<TAnimationTrack, TTarget>(track, state, out var bindingTarget))
            {
                return;
            }

            SetupResetValuesCallbacks(data, track, state, bindingTarget, resetAction);
        }

        private static void SetupResetValuesCallbacks<TAnimationTrack, TTarget, TValue>(ITweenAnimationClip data,
            TAnimationTrack track,
            AnimationState state,
            TTarget target,
            Action<TTarget, TValue> resetAction)
            where TAnimationTrack : AnimationBinding, IAnimationTrack<TValue>
            where TTarget : Object
        {
            if (track.KeyFrames.Length == 0)
            {
                return;
            }

            if (data.OnCancel != AnimationResetOption.None)
            {
                KeyFrame<TValue> keyframe = data.OnCancel == AnimationResetOption.ResetToStart
                    ? track.KeyFrames[0]
                    : track.KeyFrames[^1];

                state.AddResetCallback(() => resetAction?.Invoke(target, keyframe.Value));
            }
        }

        private static MotionBuilder<TValue, TOptions, TAdapter> CreateMotionFromKeys<TValue, TOptions, TAdapter>(
            KeyFrame<TValue> from,
            KeyFrame<TValue>? to,
            float startDelay,
            float duration
        )
            where TValue : unmanaged
            where TOptions : unmanaged, IMotionOptions
            where TAdapter : unmanaged, IMotionAdapter<TValue, TOptions>
        {
            return CreateMotionFromValues<TValue, TOptions, TAdapter>(from.Value,
                to?.Value ?? from.Value,
                startDelay,
                duration,
                from.Ease);
        }

        private static MotionBuilder<TValue, TOptions, TAdapter> CreateMotionFromValues<TValue, TOptions, TAdapter>(
            TValue fromValue,
            TValue toValue,
            float startDelay,
            float duration,
            Ease ease
        )
            where TValue : unmanaged
            where TOptions : unmanaged, IMotionOptions
            where TAdapter : unmanaged, IMotionAdapter<TValue, TOptions>
        {
            MotionBuilder<TValue, TOptions, TAdapter> motionBuilder =
                LMotion.Create<TValue, TOptions, TAdapter>(fromValue, toValue, duration)
                    .WithEase(ease);

            return startDelay > 0 ? motionBuilder.WithDelay(startDelay) : motionBuilder;
        }
    }
}
