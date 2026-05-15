using System;
using Tweenimator.Runtime.Bindings;
using UnityEngine;

namespace Tweenimator.Runtime.AnimationData
{
    [Serializable]
    public struct TargetPathKey : IEquatable<TargetPathKey>
    {
        [SerializeField] private string _path;
        [SerializeField] private string _type;

        public TargetPathKey(string path, Type type)
        {
            _path = path;
            _type = type.FullName;
        }

        public TargetPathKey(AnimationBinding binding)
        {
            _path = binding.Path;
            _type = binding.Type.FullName;
        }

        public bool Equals(TargetPathKey other)
        {
            return _path == other._path && _type == other._type;
        }

        public override bool Equals(object obj)
        {
            return obj is TargetPathKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_path, _type);
        }
    }
}
