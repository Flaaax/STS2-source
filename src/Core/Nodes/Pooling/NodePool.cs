using System;
using System.Collections.Generic;
using Godot;

namespace MegaCrit.Sts2.Core.Nodes.Pooling;

public class NodePool
{
	private static Dictionary<Type, INodePool> _pools = new Dictionary<Type, INodePool>();

	public static NodePool<T> Init<T>(string scenePath, int prewarmCount) where T : Node, IPoolable
	{
		Type typeFromHandle = typeof(T);
		if (_pools.TryGetValue(typeFromHandle, out INodePool _))
		{
			throw new InvalidOperationException($"Tried to init NodePool for type {typeof(T)} but it's already initialized!");
		}
		NodePool<T> nodePool = new NodePool<T>(scenePath, prewarmCount);
		_pools[typeFromHandle] = nodePool;
		return nodePool;
	}

	public static IPoolable Get(Type type)
	{
		if (!_pools.TryGetValue(type, out INodePool value))
		{
			throw new InvalidOperationException($"Tried to get pool for type {type} before it was initialized!");
		}
		return value.Get();
	}

	public static void Free(IPoolable poolable)
	{
		Type type = poolable.GetType();
		if (!_pools.TryGetValue(type, out INodePool value))
		{
			throw new InvalidOperationException($"Tried to get pool for type {type} before it was initialized!");
		}
		value.Free(poolable);
	}

	public static T Get<T>() where T : Node, IPoolable
	{
		return (T)Get(typeof(T));
	}

	public static void Free<T>(T obj) where T : Node, IPoolable
	{
		Free((IPoolable)obj);
	}
}
