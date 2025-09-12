using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver, IEnumerable<KeyValuePair<TKey, TValue>>
{
    [Serializable]
    public struct Entry
    {
        public TKey Key;
        public TValue Value;
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<TKey, TValue> dict = new();

    public int Count => dict.Count;

    public void OnBeforeSerialize()
    {
    }

    public void OnAfterDeserialize()
    {
        dict.Clear();
        foreach (var e in entries)
        {
            if (e.Key != null && !dict.ContainsKey(e.Key))
            {
                dict[e.Key] = e.Value;
            }
        }
    }

    public TValue this[TKey key]
    {
        get => dict[key];
        set
        {
            dict[key] = value;
            SyncFromDict();
        }
    }

    public void Add(TKey key, TValue value)
    {
        dict[key] = value;
        SyncFromDict();
    }

    public bool ContainsKey(TKey key) => dict.ContainsKey(key);

    public bool Remove(TKey key)
    {
        var result = dict.Remove(key);
        if (result) SyncFromDict();
        return result;
    }

    public bool TryGetValue(TKey key, out TValue value) => dict.TryGetValue(key, out value);

    public void Clear()
    {
        dict.Clear();
        entries.Clear();
    }

    public Dictionary<TKey, TValue>.KeyCollection Keys => dict.Keys;
    public Dictionary<TKey, TValue>.ValueCollection Values => dict.Values;

    private void SyncFromDict()
    {
        entries.Clear();
        foreach (var kv in dict)
        {
            entries.Add(new Entry { Key = kv.Key, Value = kv.Value });
        }
    }

    public KeyValuePair<TKey, TValue> ElementAt(int index)
    {
        if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
        if (entries.Count != dict.Count) SyncFromDict();
        var e = entries[index];
        return new KeyValuePair<TKey, TValue>(e.Key, e.Value);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        if (entries.Count != dict.Count) SyncFromDict();
        foreach (var e in entries)
            yield return new KeyValuePair<TKey, TValue>(e.Key, e.Value);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
