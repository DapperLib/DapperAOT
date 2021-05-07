using System;
using System.Buffers;
using System.ComponentModel;
#if NETCOREAPP3_1_OR_GREATER
using System.Collections.Immutable;
#endif

namespace Dapper.Internal
{
    /// <summary>
    /// This type is not intended for public consumption. Please just don't, thanks.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This type is not intended for public consumption. Please just don't, thanks.")]
    public struct Collector<T> : IDisposable
    {
#pragma warning disable CS1591
        private int _count, _capacity;
        private T[]? _oversized;

        public int Count => _count;

        public ref T this[int index] => ref _oversized![index]; // may fail, that's fine

        public void Add(T value)
        {
            if (_count == _capacity) Grow();
            _oversized![_count++] = value;
        }

        private void Grow()
        {
            if (_capacity == 0)
            {
                _oversized = ArrayPool<T>.Shared.Rent(8);
            }
            else
            {
                var newBuffer = ArrayPool<T>.Shared.Rent(checked(_capacity * 2));
                Span.CopyTo(newBuffer);
                ReleaseArray();
                _oversized = newBuffer;
            }
            _capacity = _oversized.Length;
        }

        private void ReleaseArray()
        {
            if (_capacity != 0)
            {
#if NETCOREAPP3_1_OR_GREATER // if we can reliably detect we *don't* have refs, we can skip this
                if (System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
#endif
                    Span.Clear();
#if NETCOREAPP3_1_OR_GREATER
                }
#endif
                ArrayPool<T>.Shared.Return(_oversized!, false);
                _oversized = null;
                _capacity = 0;
            }
        }

        public void Dispose()
        {
            _count = 0;
            ReleaseArray();
        }

        public Span<T> Span
            => _count == 0 ? default : new Span<T>(_oversized, 0, _count);

        public ArraySegment<T> Segment => _count == 0 ? default : new ArraySegment<T>(_oversized!, 0, _count);

        public T[] GetBuffer() => _oversized ?? Array.Empty<T>();

#if NETCOREAPP3_1_OR_GREATER
        public T[] ToArray() => Span.ToArray();

        public ImmutableArray<T> ToImmutableArray()
            => _count == 0 ? ImmutableArray<T>.Empty : ImmutableArray.Create<T>(_oversized!, 0, _count);

        public ImmutableList<T> ToImmutableList()
        {
            switch (_count)
            {
                case 0:
                    return ImmutableList<T>.Empty;
                case 1:
                    return ImmutableList.Create<T>(_oversized![0]);
                default:
                    var builder = ImmutableList<T>.Empty.ToBuilder();
                    foreach (var item in Span)
                    {
                        builder.Add(item);
                    }
                    return builder.ToImmutable();
            }
        }

#endif

#pragma warning restore CS1591
    }
}
