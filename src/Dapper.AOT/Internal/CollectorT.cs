//using System;
//using System.Buffers;
//using System.Collections.Generic;
//using System.ComponentModel;

//namespace Dapper.Internal;

///// <summary>
///// This type is not intended for public consumption. Please just don't, thanks.
///// </summary>
//[Browsable(false)]
//[EditorBrowsable(EditorBrowsableState.Never)]
//[Obsolete(InternalUtilities.ObsoleteWarning)]
//internal struct Collector<T> : IDisposable
//{
//    private int _count, _capacity;
//    private T[]? _oversized;

//    public int Count => _count;

//    // public ref T this[int index] => ref _oversized![index]; // may fail, that's fine

//    public void Add(T value)
//    {
//        if (_count == _capacity) Grow();
//        _oversized![_count++] = value;
//    }

//    private void Grow()
//    {
//        if (_capacity == 0)
//        {
//            _oversized = ArrayPool<T>.Shared.Rent(8);
//        }
//        else
//        {
//            var newBuffer = ArrayPool<T>.Shared.Rent(checked(_capacity * 2));
//            Span.CopyTo(newBuffer);
//            ReleaseArray();
//            _oversized = newBuffer;
//        }
//        _capacity = _oversized.Length;
//    }

//    private void ReleaseArray()
//    {
//        if (_capacity != 0)
//        {
//#if NETCOREAPP3_1_OR_GREATER // if we can reliably detect we *don't* have refs, we can skip this
//            if (System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<T>())
//            {
//#endif
//                Span.Clear();
//#if NETCOREAPP3_1_OR_GREATER
//            }
//#endif
//            ArrayPool<T>.Shared.Return(_oversized!, false);
//            _oversized = null;
//            _capacity = 0;
//        }
//    }

//    public void Dispose()
//    {
//        _count = 0;
//        ReleaseArray();
//    }

//    public Span<T> Span
//        => _count == 0 ? default : new Span<T>(_oversized, 0, _count);

//    // public ArraySegment<T> Segment => _count == 0 ? default : new ArraySegment<T>(_oversized!, 0, _count);

//    // public T[] GetBuffer() => _oversized ?? Array.Empty<T>();

//    public List<T> ToList()
//    {
//        var list = new List<T>(_count);
//        foreach (var item in Span)
//        {
//            list.Add(item);
//        }
//        return list;
//    }
//}
