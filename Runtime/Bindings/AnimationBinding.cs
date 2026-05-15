using System;
using Tweenimator.Runtime.Attributes;
using UnityEngine;

namespace Tweenimator.Runtime.Bindings
{
    [Serializable]
    public class AnimationBinding : ISerializationCallbackReceiver
    {
        public string Path => _path;
        public Type Type { get; private set; }
        public AnimationBindingType BindingType => _bindingType;

        [SerializeField]
        private string _path = default!;
        [SerializeField, InspectorReadOnly]
        private string _type = default!;
        [SerializeField, InspectorReadOnly]
        private AnimationBindingType _bindingType;

        public AnimationBinding(string path, Type type, AnimationBindingType bindingType)
        {
            _path = path;
            _bindingType = bindingType;
            Type = type;
        }

        // Called before serialization to convert Type to string
        public virtual void OnBeforeSerialize()
        {
            _type = Type?.AssemblyQualifiedName ?? string.Empty;
        }

        // Called after deserialization to convert string back to Type
        public virtual void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(_type))
            {
                Type = Type.GetType(_type);
                if (Type == null)
                {
                    Debug.LogWarning($"Failed to get Type from string: {_type}");
                }
            }
        }
    }
}
