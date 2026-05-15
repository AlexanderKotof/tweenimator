using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

#pragma warning disable CS0693 // Type parameter has the same name as the type parameter from outer type

namespace Tweenimator.Runtime.Helpers
{
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>, ISerializationCallbackReceiver, IDictionary<TKey, TValue>
    {
        [Serializable]
        public struct InnerDataWrapper<TKey, TValue>
        {
            [field: SerializeField] public TKey Key { get; private set; }
            [field: SerializeField] public TValue Value { get; private set; }

            public InnerDataWrapper(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }

        public TValue this[TKey key]
        {
            get => InnerDictionary[key];
            set => InnerDictionary[key] = value;
        }

        public int Count => InnerDictionary.Count;
        public bool IsReadOnly { get; } = false;
        public IEnumerable<TKey> Keys => InnerDictionary.Keys;

        ICollection<TValue> IDictionary<TKey, TValue>.Values => InnerDictionary.Values;

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => InnerDictionary.Keys;

        public IEnumerable<TValue> Values => InnerDictionary.Values;

        public Dictionary<TKey, TValue> InnerDictionary { get; } = new ();

        [SerializeField]
        private List<InnerDataWrapper<TKey, TValue>> _values = new();

        public bool TryAdd(TKey key, TValue value)
        {
            return InnerDictionary.TryAdd(key, value);
        }

        public void Add(TKey key, TValue value)
        {
            InnerDictionary.Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return InnerDictionary.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            return InnerDictionary.Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return InnerDictionary.Remove(item.Key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return InnerDictionary.TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            InnerDictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return InnerDictionary.ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public void OnBeforeSerialize()
        {
            _values.Clear();
            foreach (var pair in InnerDictionary)
            {
                _values.Add(new InnerDataWrapper<TKey, TValue>(pair.Key, pair.Value));
            }
        }

        public void OnAfterDeserialize()
        {
            foreach (var keyPair in _values)
            {
                InnerDictionary.TryAdd(keyPair.Key, keyPair.Value);
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return InnerDictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
