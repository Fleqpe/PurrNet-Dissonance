using System.Collections.Generic;

public class ByteArrayPool
{
    private static readonly Dictionary<int, Queue<byte[]>> Pools = new();
    private static readonly object Lock = new();

    public static byte[] Rent(int size)
    {
        lock (Lock)
        {
            if (!Pools.TryGetValue(size, out var pool))
            {
                pool = new Queue<byte[]>();
                Pools[size] = pool;
            }

            return pool.Count > 0 ? pool.Dequeue() : new byte[size];
        }
    }

    public static void Return(byte[] array)
    {
        if (array == null) return;
        
        lock (Lock)
        {
            if (!Pools.TryGetValue(array.Length, out var pool))
            {
                pool = new Queue<byte[]>();
                Pools[array.Length] = pool;
            }

            pool.Enqueue(array);
        }
    }
}