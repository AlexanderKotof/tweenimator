using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using JetBrains.Annotations;
using Tweenimator.Editor.Extensions;
using Tweenimator.Runtime.Bindings;
using Tweenimator.Runtime.AnimationData;
using Tweenimator.Runtime.Components;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using ProcessorAlias =
    System.Func<string,
        System.Collections.Generic.List<UnityEditor.EditorCurveBinding>,
        UnityEngine.AnimationClip,
        Tweenimator.Runtime.Bindings.AnimationBinding>;

namespace Tweenimator.Editor
{
    public static class AnimationToTwinConverter
    {
        private static readonly Dictionary<string, ProcessorAlias> Processors = new()
        {
            { "m_LocalPosition",            ProcessPosition             },
            { "m_LocalRotation",            ProcessQuaternionRotation   },
            { "localEulerAnglesRaw",        ProcessEulerAnglesRotation  },
            { "m_LocalScale",               ProcessScale                },
            { "m_AnchoredPosition",         ProcessAnchoredPosition     },
            { "m_SizeDelta",                ProcessSizeDelta            },
            { "m_IsActive",                 ProcessGameObjectActive     },
            { "m_Enabled",                  ProcessComponentActive      },
            { "m_Color",                    ProcessColor                },
            { "m_PixelsPerUnitMultiplier",  ProcessPixelsPerUnit        },
            { "m_Alpha",                    ProcessAlpha                }
        };

        [MenuItem("Tools/Tweenimator/Convert Selected Animations")]
        public static void ConvertAnimation()
        {
            Object selectedObject = Selection.activeObject;

            if (selectedObject == null)
            {
                Debug.LogError($"Nothing selected! Select game object with Animator, Animation Clip[s] or Animation Controller asset!");
                return;
            }

            AnimationClip[] selectedClips = GetSelectedClips();
            if (selectedClips.Length > 0)
            {
                TryConvertAnimations(selectedClips);
                return;
            }

            if (selectedObject is GameObject gameObject)
            {
                if (gameObject.TryGetComponent<Animator>(out _))
                {
                    AnimationClip[] clips = AnimationUtility.GetAnimationClips(gameObject);
                    TryConvertAnimations(clips);
                    return;
                }

                if (gameObject.TryGetComponent<Animation>(out _))
                {
                    AnimationClip[] clips = AnimationUtility.GetAnimationClips(gameObject);
                    TryConvertAnimations(clips);
                    return;
                }
            }

            if (selectedObject is RuntimeAnimatorController controller)
            {
                TryConvertAnimations(controller.animationClips);
                return;
            }

            if (selectedObject is AnimationClip clip)
            {
                TryConvertAnimations(new[] { clip });
                return;
            }

            Debug.LogError("Cannot process selected object!");
        }

        private static AnimationClip[] GetSelectedClips()
        {
            List<AnimationClip> selectedClips = new List<AnimationClip>();
            Object[] selectedObjects = Selection.objects;
            foreach (Object obj in selectedObjects)
            {
                if (obj is AnimationClip clip)
                {
                    selectedClips.Add(clip);
                }
            }
            return selectedClips.ToArray();
        }

        private static void TryConvertAnimations(AnimationClip[] clips)
        {
            if (clips.Length == 0)
            {
                Debug.LogError("No clips found!");
                return;
            }

            GameObject selectedObject = Selection.activeGameObject;
            TweenAnimationsComponent component = null;

            if (selectedObject != null)
            {
                component = GetOrAddComponent<TweenAnimationsComponent>(selectedObject);
                component.Animations.Clear();
            }

            Object createdAsset = null;

            foreach (AnimationClip clip in clips)
            {
                var tweenAnimationAsset = CreateTweenAnimationAsset(clip, selectedObject);
                createdAsset = tweenAnimationAsset;

                tweenAnimationAsset.Duration = clip.length;
                tweenAnimationAsset.MainTarget = selectedObject?.name ?? string.Empty;
                tweenAnimationAsset.Loop = clip.wrapMode == WrapMode.Loop;

                ProcessClip(clip, tweenAnimationAsset);

                component?.Animations.Add(tweenAnimationAsset);

                EditorUtility.SetDirty(tweenAnimationAsset);
            }

            component?.RegisterAnimationsTargets();

            if (createdAsset != null)
            {
                AssetDatabase.SaveAssets();
                ProjectWindowUtil.ShowCreatedAsset(createdAsset);
            }
        }

        private static TweenAnimationClip CreateTweenAnimationAsset(AnimationClip clip, [CanBeNull] GameObject selectedObject)
        {
            string clipName = clip.name;
            string assetName = selectedObject == null ?
                $"{clipName}.asset" :
                $"{selectedObject.name}_{clipName}.asset";

            TweenAnimationClip tweenAnimationAsset = ScriptableObject.CreateInstance<TweenAnimationClip>();

            string prefabPath = AssetDatabase.GetAssetPath(clip);
            prefabPath = Path.GetDirectoryName(prefabPath) + "/" + assetName;

            Debug.Log($"Created asset at path: {prefabPath}");

            AssetDatabase.CreateAsset(tweenAnimationAsset, prefabPath);

            return tweenAnimationAsset;
        }

        private static TComponent GetOrAddComponent<TComponent>(GameObject gameObject)
            where TComponent : Component
        {
            if (gameObject.TryGetComponent(out TComponent component))
            {
                return component;
            }

            return gameObject.AddComponent<TComponent>();
        }

        private static void ProcessClip(AnimationClip clip, ITweenAnimationClip animationClip)
        {
            Dictionary<string, List<EditorCurveBinding>> bindingsMap = new Dictionary<string, List<EditorCurveBinding>>();
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);

            foreach (EditorCurveBinding binding in bindings)
            {
                string id = GetBindingId(binding);
                if (!bindingsMap.ContainsKey(id))
                {
                    bindingsMap.Add(id, new List<EditorCurveBinding>() { binding } );
                }
                else
                {
                    bindingsMap[id].Add(binding);
                }
                Debug.Log($"Binding found {(string.IsNullOrEmpty(binding.path) ? "this" : "this/" + binding.path)} " +
                          $"[{binding.type.Name}] : {binding.propertyName}");
            }

            ValidateBindings(bindingsMap);

            ProcessBindingsMap(clip, bindingsMap, animationClip);
        }

        private static void ValidateBindings(Dictionary<string, List<EditorCurveBinding>> bindingsMap)
        {
            // Check that we have all required bindings
            foreach (var pair in bindingsMap)
            {
                switch (pair.Key)
                {
                    // 3 properties required
                    case "m_LocalPosition":
                    case "m_LocalScale":
                    case "localEulerAnglesRaw":
                        EnsureBindingsCount(pair.Key, pair.Value, 3);
                        break;

                    // 2 properties required
                    case "m_AnchoredPosition":
                    case "m_SizeDelta":
                        EnsureBindingsCount(pair.Key, pair.Value, 2);
                        break;

                    // 4 properties required
                    case "m_LocalRotation":
                    case "m_Color":
                        EnsureBindingsCount(pair.Key, pair.Value, 4);
                        break;

                    // 1 property - validation not needed
                    case "m_IsActive":
                    case "m_Enabled":
                    case "m_PixelsPerUnitMultiplier":
                    case "m_Alpha":
                        break;

                    default:
                        Debug.LogWarning($"No validation for {pair.Key}!");
                        break;
                }
            }
            return;

            void EnsureBindingsCount(string bindingName, List<EditorCurveBinding> bindings, int requiredCount)
            {
                if (bindings.Count != requiredCount)
                {
                    Debug.LogError($"Binding {bindingName} has {bindings.Count} which does not match the required number of bindings!");
                }
            }
        }

        private static void ProcessBindingsMap(AnimationClip clip,
            Dictionary<string, List<EditorCurveBinding>> bindingsMap,
            ITweenAnimationClip animationClip)
        {
            foreach (List<EditorCurveBinding> bindings in bindingsMap.Values)
            {
                string propertyName = GetPropertyFirstName(bindings[0]);

                if (ProcessBindings(clip, bindings, propertyName, out AnimationBinding binding) == false)
                {
                    continue;
                }

                Debug.Log($"Successfully processed binding {propertyName}");
                AddBindingToAsset(binding, animationClip);
            }
        }

        private static bool ProcessBindings(AnimationClip clip,
            List<EditorCurveBinding> bindings,
            string propertyName,
            [NotNullWhen(true)] out AnimationBinding convertedBinding)
        {
            string path = bindings[0].path;
            convertedBinding = null;

            if (!Processors.TryGetValue(propertyName, out var processor))
            {
                Debug.LogError($"Can not process binding: {propertyName} - not supported");
                return false;
            }

            convertedBinding = processor(path, bindings, clip);
            return convertedBinding != null;
        }

        private static void AddBindingToAsset(AnimationBinding binding, ITweenAnimationClip animationClip)
        {
            switch (binding)
            {
                case AnimationTrack<bool> boolTrack:
                    animationClip.BoolTracks.Add(boolTrack);
                    break;
                case AnimationTrack<float> floatTrack:
                    animationClip.FloatTracks.Add(floatTrack);
                    break;
                case AnimationTrack<Vector2> vector2Track:
                    animationClip.Vector2Tracks.Add(vector2Track);
                    break;
                case AnimationTrack<Vector3> vector3Track:
                    animationClip.Vector3Tracks.Add(vector3Track);
                    break;
                case AnimationTrack<Color> colorTrack:
                    animationClip.ColorTracks.Add(colorTrack);
                    break;
            }
        }

        private static AnimationBinding ProcessAnchoredPosition(string path, List<EditorCurveBinding> bindings, AnimationClip clip)
        {
            Dictionary<float, Vector2> keyMap = CreateKeyMapFromBindings<Vector2>(bindings, clip, UpdateVector2Value);
            return new AnimationTrack<Vector2>(path, CreateSortedKeyframesArray(keyMap), typeof(RectTransform), AnimationBindingType.AnchoredPosition);
        }
        private static AnimationBinding ProcessSizeDelta(string path, List<EditorCurveBinding> bindings, AnimationClip clip)
        {
            Dictionary<float, Vector2> keyMap = CreateKeyMapFromBindings<Vector2>(bindings, clip, UpdateVector2Value);
            return new AnimationTrack<Vector2>(path, CreateSortedKeyframesArray(keyMap), typeof(RectTransform), AnimationBindingType.SizeDelta);
        }
        private static AnimationBinding ProcessQuaternionRotation(string path, List<EditorCurveBinding> bindings, AnimationClip clip)
        {
            Type targetType = bindings[0].type;
            Dictionary<float, Vector4> keyMap = CreateKeyMapFromBindings<Vector4>(bindings, clip, UpdateVector4Value);

            // Rotation is quaternion, so we need to cast it to euler angles
            Dictionary<float, Vector3> eulerAnglesRotations = ConvertQuaternionsToEulerAngles(keyMap);
            return new AnimationTrack<Vector3>(path, CreateSortedKeyframesArray(eulerAnglesRotations), targetType, AnimationBindingType.LocalRotation);
        }
        private static AnimationBinding ProcessEulerAnglesRotation(string path, List<EditorCurveBinding> bindings, AnimationClip clip)
        {
            Type targetType = bindings[0].type;
            Dictionary<float, Vector3> keyMap = CreateKeyMapFromBindings<Vector3>(bindings, clip, UpdateVector3Value);
            return new AnimationTrack<Vector3>(path, CreateSortedKeyframesArray(keyMap), targetType, AnimationBindingType.LocalRotation);
        }
        private static Dictionary<float, Vector3> ConvertQuaternionsToEulerAngles(Dictionary<float, Vector4> keyMap)
        {
            Dictionary<float, Vector3> eulerAngles = new Dictionary<float, Vector3>();

            foreach (var pair in keyMap)
            {
                var quaternion = new Quaternion(pair.Value.x, pair.Value.y, pair.Value.z, pair.Value.w);
                eulerAngles.Add(pair.Key, quaternion.eulerAngles);
            }

            return eulerAngles;
        }

        private static AnimationBinding ProcessPosition(string path, List<EditorCurveBinding> bindings, AnimationClip clip)
        {
            Type targetType = bindings[0].type;
            Dictionary<float, Vector3> keyMap = CreateKeyMapFromBindings<Vector3>(bindings, clip, UpdateVector3Value);
            return new AnimationTrack<Vector3>(path, CreateSortedKeyframesArray(keyMap), targetType, AnimationBindingType.LocalPosition);
        }
        private static AnimationBinding ProcessScale(string path, List<EditorCurveBinding> bindings, AnimationClip clip)
        {
            Type targetType = bindings[0].type;
            Dictionary<float, Vector3> keyMap = CreateKeyMapFromBindings<Vector3>(bindings, clip, UpdateVector3Value);
            return new AnimationTrack<Vector3>(path, CreateSortedKeyframesArray(keyMap), targetType, AnimationBindingType.LocalScale);
        }
        private static AnimationBinding ProcessGameObjectActive(string path, List<EditorCurveBinding> bindings, AnimationClip clip)
        {
            Dictionary<float, bool> keyMap = CreateKeyMapFromBindings<bool>(bindings, clip, UpdateBoolValue);
            return new AnimationTrack<bool>(path, CreateSortedKeyframesArray(keyMap), typeof(GameObject), AnimationBindingType.GameObjectActivity);

            bool UpdateBoolValue(EditorCurveBinding _, bool __,  Keyframe keyframe)
            {
                return Math.Abs(keyframe.value - 1f) < 0.1f;
            }
        }
        private static AnimationBinding ProcessComponentActive(string path, List<EditorCurveBinding> bindings, AnimationClip clip)
        {
            Type targetType = bindings[0].type;
            Dictionary<float, bool> keyMap = CreateKeyMapFromBindings<bool>(bindings, clip, UpdateBoolValue);
            return new AnimationTrack<bool>(path, CreateSortedKeyframesArray(keyMap), targetType, AnimationBindingType.ComponentActivity);

            bool UpdateBoolValue(EditorCurveBinding _, bool __,  Keyframe keyframe)
            {
                return Math.Abs(keyframe.value - 1f) < 0.1f;
            }
        }
        private static AnimationBinding ProcessColor(string path, List<EditorCurveBinding> bindings, AnimationClip clip)
        {
            Type targetType = bindings[0].type;
            Dictionary<float, Color> keyMap = CreateKeyMapFromBindings(bindings, clip, UpdateColorValue, Color.white);
            return new AnimationTrack<Color>(path, CreateSortedKeyframesArray(keyMap), targetType, AnimationBindingType.GraphicColor);
        }
        private static AnimationBinding ProcessPixelsPerUnit(string path, List<EditorCurveBinding> bindings, AnimationClip clip)
        {
            Type targetType = bindings[0].type;
            Dictionary<float, float> keyMap = CreateKeyMapFromBindings<float>(bindings, clip, (_, _, keyframe) => keyframe.value);
            return new AnimationTrack<float>(path, CreateSortedKeyframesArray(keyMap), targetType, AnimationBindingType.PixelsPerUnit);
        }
        private static AnimationBinding ProcessAlpha(string path, List<EditorCurveBinding> bindings, AnimationClip clip)
        {
            Type targetType = bindings[0].type;
            if (targetType != typeof(CanvasGroup))
            {
                Debug.LogWarning("Unsupported animation target type: " + targetType);
                return null;
            }

            Dictionary<float, float> keyMap = CreateKeyMapFromBindings<float>(bindings, clip, (_, _, keyframe) => keyframe.value);
            return new AnimationTrack<float>(path, CreateSortedKeyframesArray(keyMap), targetType, AnimationBindingType.CanvasGroupAlpha);
        }

        private static Dictionary<float, TValue> CreateKeyMapFromBindings<TValue>(List<EditorCurveBinding> bindings,
            AnimationClip clip, Func<EditorCurveBinding, TValue, Keyframe, TValue> updateValueFunc, TValue defaultValue = default!)
        {
            Dictionary<float, TValue> keyMap = new Dictionary<float, TValue>();
            foreach (EditorCurveBinding binding in bindings)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);

                foreach (Keyframe keyFrame in curve.keys)
                {
                    float normalizedTime = keyFrame.time / clip.length;
                    if (keyMap.TryGetValue(normalizedTime, out TValue position))
                    {
                        position = updateValueFunc(binding, position, keyFrame);
                        keyMap[normalizedTime] = position;
                    }
                    else
                    {
                        position = updateValueFunc(binding, defaultValue, keyFrame);
                        keyMap.Add(normalizedTime, position);
                    }
                }
            }

            return keyMap;
        }
        private static KeyFrame<TValue>[] CreateSortedKeyframesArray<TValue>(Dictionary<float, TValue> keyMap)
        {
            List<KeyFrame<TValue>> keyframes = new List<KeyFrame<TValue>>();

            foreach (KeyValuePair<float, TValue> key in keyMap)
            {
                keyframes.Add(new KeyFrame<TValue>(key.Key, key.Value));
            }

            keyframes.Sort((x, y) => x.Time.CompareTo(y.Time));
            return keyframes.ToArray();
        }

        private static Vector3 UpdateVector3Value(EditorCurveBinding binding, Vector3 value, Keyframe keyFrame)
        {
            switch (GetPropertySecondName(binding))
            {
                case "x":
                    value.x = keyFrame.value;
                    break;
                case "y":
                    value.y = keyFrame.value;
                    break;
                case "z":
                    value.z = keyFrame.value;
                    break;
            }

            return value;
        }
        private static Vector4 UpdateVector4Value(EditorCurveBinding binding, Vector4 value, Keyframe keyFrame)
        {
            switch (GetPropertySecondName(binding))
            {
                case "x":
                    value.x = keyFrame.value;
                    break;
                case "y":
                    value.y = keyFrame.value;
                    break;
                case "z":
                    value.z = keyFrame.value;
                    break;
                case "w":
                    value.w = keyFrame.value;
                    break;
            }

            return value;
        }
        private static Vector2 UpdateVector2Value(EditorCurveBinding binding, Vector2 value, Keyframe keyFrame)
        {
            switch (GetPropertySecondName(binding))
            {
                case "x":
                    value.x = keyFrame.value;
                    break;
                case "y":
                    value.y = keyFrame.value;
                    break;
            }

            return value;
        }

        private static Color UpdateColorValue(EditorCurveBinding binding, Color value, Keyframe keyFrame)
        {
            switch (GetPropertySecondName(binding))
            {
                case "r":
                    value.r = keyFrame.value;
                    break;
                case "g":
                    value.g = keyFrame.value;
                    break;
                case "b":
                    value.b = keyFrame.value;
                    break;
                case "a":
                    value.a = keyFrame.value;
                    break;
            }

            return value;
        }

        private static string GetBindingId(EditorCurveBinding binding)
        {
            return $"{binding.path}/{GetPropertyFirstName(binding)}";
        }

        private static string GetPropertyFirstName(EditorCurveBinding binding)
        {
            return binding.propertyName.Split('.')[0];
        }

        private static string GetPropertySecondName(EditorCurveBinding binding)
        {
            return binding.propertyName.Split('.')[1];
        }
    }
}
