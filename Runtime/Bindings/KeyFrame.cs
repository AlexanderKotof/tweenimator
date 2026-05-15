using System;
using LitMotion;
using UnityEngine;

namespace Tweenimator.Runtime.Bindings
{
    [Serializable]
    public class KeyFrame<TValue>
    {
        [field: SerializeField, Range(0, 1)] public float Time { get; private set; }
        [field: SerializeField] public TValue Value { get; private set; }
        [field: SerializeField] public Ease Ease { get; private set; }

        public KeyFrame(float time, TValue value)
        {
            Time = time;
            Value = value;
            Ease = Ease.Linear;
        }
    }
}
