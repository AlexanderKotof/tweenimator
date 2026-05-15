using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Tweenimator.Runtime.AnimationData;
using Tweenimator.Runtime.Bindings;
using Tweenimator.Runtime.Components;
using UnityEditor;
using UnityEngine;

namespace Tweenimator.Editor.Extensions
{
    public static class AnimationRegistrationExtension
    {
        public static void RegisterAnimationsTargets(this IAnimationComponent animationSubcomponent)
        {
            if (animationSubcomponent is not MonoBehaviour animationMonoComponent)
            {
                Debug.LogErrorFormat("{0} is not a MonoBehaviour", animationSubcomponent.GetType());
                return;
            }

            animationSubcomponent.AnimationTargets.Clear();

            foreach (var clip in animationSubcomponent.GetAnimationClips())
            {
                animationMonoComponent.RegisterBindingTargets(clip,
                    animationSubcomponent.AnimationTargets);
            }

            EditorUtility.SetDirty(animationMonoComponent);
        }

        //ToDo: This function can be used for register animations in runtime
        // but uses expensive calls
        public static void RegisterBindingTargets(this MonoBehaviour component, ITweenAnimationClip? clip, IDictionary<TargetPathKey, Object> targetsMap)
        {
            if (clip == null)
            {
                return;
            }

            Transform? mainTarget;

            if (string.IsNullOrEmpty(clip.MainTarget) || clip.MainTarget.Equals(component.name))
            {
                mainTarget = component.transform;
            }
            else if (TryFindMainTarget(component.transform, clip.MainTarget, out mainTarget) == false)
            {
                Object? clipObj = clip as Object;
                Debug.LogError($"Can not find main target {clip.MainTarget} of animation clip {clipObj?.name}!",
                    component.gameObject);
                return;
            }

            foreach (var binding in GetAllBindings(clip))
            {
                if (TryFindTarget(binding, mainTarget, out var target) == false)
                {
                    Object? clipObj = clip as Object;
                    Debug.LogError($"Can not find animation target {binding.Type} by path {binding.Path} of animation clip {clipObj?.name}!",
                        component.gameObject);
                    continue;
                }

                var targetPath = new TargetPathKey(binding);
                targetsMap.TryAdd(targetPath, target);
            }
        }

        private static IEnumerable<AnimationBinding> GetAllBindings(ITweenAnimationClip clip)
        {
            return clip.BoolTracks
                .Concat<AnimationBinding>(clip.FloatTracks)
                .Concat(clip.Vector2Tracks)
                .Concat(clip.Vector3Tracks)
                .Concat(clip.ColorTracks);
        }

        private static bool TryFindTarget(AnimationBinding binding, Transform mainTarget, [NotNullWhen(true)] out Object? target)
        {
            target = null;
            Transform targetTransform = mainTarget.Find(binding.Path);
            if (targetTransform == null)
            {
                return false;
            }

            if (binding.Type == typeof(GameObject))
            {
                target = targetTransform.gameObject;
                return true;
            }

            if (targetTransform.TryGetComponent(binding.Type, out var component))
            {
                target = component;
                return true;
            }

            return false;
        }

        private static bool TryFindMainTarget(Transform thisTransform, string name, [NotNullWhen(true)] out Transform? target)
        {
            var childTransforms = thisTransform.GetComponentsInChildren<Transform>(true);

            target = childTransforms.FirstOrDefault(childTransform => childTransform.name.Equals(name));

            return target != null;
        }
    }
}
