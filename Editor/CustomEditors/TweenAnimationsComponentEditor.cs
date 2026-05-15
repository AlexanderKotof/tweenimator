using Tweenimator.Editor.Extensions;
using Tweenimator.Runtime.Components;
using UnityEditor;
using UnityEngine;

namespace Tweenimator.Runtime.Editor
{
    [CustomEditor(typeof(TweenAnimationsComponent))]
    public class TweenAnimationsComponentEditor : UnityEditor.Editor
    {
        private TweenAnimationsComponent? _target;

        private void OnEnable()
        {
            _target = target as TweenAnimationsComponent;
            ValidateComponent();
        }

        public override void OnInspectorGUI()
        {
            //DrawUsageWarning();

            EditorGUI.BeginChangeCheck();

            base.OnInspectorGUI();

            if (EditorGUI.EndChangeCheck())
            {
                _target?.TryCancelAnimation();
                ValidateComponent();
            }

            DrawControls();

            GUILayout.Space(10);

            if (GUILayout.Button("Validate"))
            {
                ValidateComponent();
            }
        }

        private void DrawControls()
        {
            if (_target == null)
            {
                return;
            }

            var clips = _target.GetAnimationClips();

            GUILayout.Space(10);

            if (clips.Count == 0)
            {
                EditorGUILayout.HelpBox("Add animations to list to play them", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Play animation:");
            EditorGUILayout.BeginHorizontal();

            for (var i = 0; i < clips.Count; i++)
            {
                if (clips[i] == null)
                {
                    continue;
                }

                if (GUILayout.Button((i + 1).ToString(), EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    _target.PlayAnimation(i);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Stop animation"))
            {
                _target.TryCancelAnimation();
            }
        }

        private static void DrawUsageWarning()
        {
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("This component is for testing animations, not for production use!\n" +
                                    "Please remove it once the animation is tested.", MessageType.Error);
            GUILayout.Space(10);
        }

        private void ValidateComponent()
        {
            _target?.RegisterAnimationsTargets();
        }
    }
}
