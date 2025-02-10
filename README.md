# ArrayPoolCollection

A low-allocation collection library using pooled arrays

![](ArrayPoolCollection.png)

## Basic Usage

```cs
// By using the `using` statement, it will be automatically returned to the pool.
using var dict = new ArrayPoolDictionary<int, string>();

// Can be used in the same way as a Dictionary
dict.Add(123, "Alice");
Console.WriteLine(dict[123]);   // "Alice"
```

## Install

ArrayPoolCollection can be installed from NuGet `AndanteSoft.ArrayPoolCollection`.

```
dotnet add package AndanteSoft.ArrayPoolCollection
```

ArrayPoolCollection requires .NET Standard 2.1 or .NET 9.
(There are performance benefits to using .NET 9 where possible.)

If you want to use it with [MemoryPack](https://github.com/Cysharp/MemoryPack), install `AndanteSoft.ArrayPoolCollection.MemoryPack` instead.

### Unity

Supported version: 2021.2 or later. (API Compatibility Level: .NET Standard 2.1)

My test environment is 6000.1.0b1.

Use [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) to install.

## Features

It provides the following collections, which are mostly compatible with the default collections:

* `ArrayPoolWrapper<T>` : [`T[]`](https://learn.microsoft.com/en-us/dotnet/api/system.array?view=net-9.0)
* `ArrayPoolList<T>` : [`List<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1?view=net-9.0)
* `ArrayPoolDictionary<TKey, TValue>` : [`Dictionary<TKey, TValue>`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2?view=net-9.0)
* `ArrayPoolHashSet<T>` : [`HashSet<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1?view=net-9.0)
* `ArrayPoolStack<T>` : [`Stack<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.stack-1?view=net-9.0)
* `ArrayPoolQueue<T>` : [`Queue<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.queue-1?view=net-9.0)
* `ArrayPoolPriorityQueue<TElement, TPriority>` : [`PriorityQueue<TElement, TPriority>`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.priorityqueue-2?view=net-9.0)

They use `ArrayPool<T>` for their internal arrays, and by calling `Dispose()` when destroyed, they do not generate GC garbage.
These implement almost all APIs available as of .NET 9.

> Therefore, it can also be used to utilize `PriorityQueue`, which was not available in the current Unity environment.

Example of Dictionary usage:

```cs
using var dict = new ArrayPoolDictionary<string, string>(new Utf8BytesComparer())
{
    { "Alice", "Supernova" }
};

// You can use GetAlternateLookup in Unity.
// Unfortunately, it is not a `allows ref struct` constraint, so
// `ReadOnlySpan<byte>` cannot be taken as an argument.
Debug.Log(dict["Alice"]);   // "Supernova"
Debug.Log(dict.GetAlternateLookup<byte[]>()[Encoding.UTF8.GetBytes("Alice")]);  // "Supernova"

private class Utf8BytesComparer : IEqualityComparer<string>, IAlternateEqualityComparer<byte[], string>
{
    public string Create(byte[] alternate)
    {
        return Encoding.UTF8.GetString(alternate);
    }

    public bool Equals(byte[] alternate, string other)
    {
        return Encoding.UTF8.GetBytes(other).SequenceEqual(alternate);
    }

    public bool Equals(string x, string y)
    {
        return x.Equals(y);
    }

    public int GetHashCode(byte[] alternate)
    {
        return Encoding.UTF8.GetString(alternate).GetHashCode();
    }

    public int GetHashCode(string obj)
    {
        return obj.GetHashCode();
    }
}
```

In addition, it implements the following alternative collections, although they have some differences in specifications:

* `ArrayPoolBits` : [`BitArray`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.bitarray?view=net-9.0)

> Reason: It's an old collection and the design is old.
> For example, `And` and `Xor` returns `void` ​​to indicate that they are mutable.
> Also, `BitArray` only implements `ICollection` (non-generic!), while `ArrayPoolBits` implements `IList<bool>`.

Also implement an object pool:

* `ObjectPool`

By utilizing this and reusing instances of `ArrayPool***`, GC garbage can be completely eliminated.
This is also available in the pool of GameObjects in Unity.

```cs
// Get a dictionary from the pool
var dict = ObjectPool<ArrayPoolDictionary<int, string>>.Shared.Rent();

// The contents of the rented dictionary are undefined, so clear it.
dict.Clear();

// Then use it just like a normal Dictionary.
dict.Add(123, "Alice");
dict.Add(456, "Barbara");
dict.Add(789, "Charlotte");

Debug.Log($"{dict[123]}");      // "Alice"

// Return it when you're done using it
ObjectPool<ArrayPoolDictionary<int, string>>.Shared.Return(dict);
```

Example of pooling `GameObject` in Unity:

```cs
var root = new GameObject("root");

// Create GameObjectPool
using var gameObjectPool = new ObjectPool<GameObject>(PooledObjectCallback<GameObject>.Create(
    // OnInstantiate - create prefab
    () =>
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.SetParent(root.transform, false);
        return go;
    },
    // OnRent - set active
    go => go.SetActive(true),
    // OnReturn - set inactive
    go => go.SetActive(false),
    // OnDestroy - destroy
    go => Destroy(go)
));

// Generate a specified number of pieces in advance
gameObjectPool.Prewarm(16);

for (int i = 0; i < 64; i++)
{
    // Rent & Return
    var go = gameObjectPool.Rent();
    go.transform.position = UnityEngine.Random.insideUnitSphere * 10;
    UniTask.Delay(1000).ContinueWith(() => gameObjectPool.Return(go)).Forget();

    await UniTask.Delay(50);
}

await UniTask.Delay(1000);
```

### With MemoryPack
Collections can be serialized using [MemoryPack](https://github.com/Cysharp/MemoryPack).

```cs
// Call this once at the beginning
ArrayPoolCollectionRegisterer.Register();

using var list = new ArrayPoolList<string>()
{
    "Alice", "Barbara", "Charlotte",
};

// Serialize the list
byte[] bytes = MemoryPackSerializer.Serialize(list);

// Deserialize the list
var deserialized = MemoryPackSerializer.Deserialize<ArrayPoolList<string>>(bytes);

// "Alice, Barbara, Charlotte"
Debug.Log(string.Join(", ", deserialized));
```

Notes:

* `Comparer` of Dictionary/HashSet is not usually serializable, so it is not saved. If you want to save it, you need to overwrite it when serializing.

```cs
using var ignoreCase = new ArrayPoolDictionary<string, int>(StringComparer.OrdinalIgnoreCase)
{
    { "Alice", 16 },
};

byte[] bytes = MemoryPackSerializer.Serialize(ignoreCase);

// ---

var deserialized = new ArrayPoolDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
MemoryPackSerializer.Deserialize(bytes, ref deserialized);

Debug.Log($"{deserialized["alice"]}");      // 16
```

## Fork

### Build

```
dotnet build
```

### Run tests

```
dotnet test
```

### Run benchmarks

```
dotnet run -c Release --project ArrayPoolCollection.Benchmarks
```

### Publish

```
dotnet pack
```

## License

[MIT License](LICENSE)
