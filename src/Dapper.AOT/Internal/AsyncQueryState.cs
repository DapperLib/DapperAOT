using System;
using System.Buffers;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper.Internal
{
    internal interface IQueryState
    {
        DbDataReader? Reader { get; }
        DbCommand? Command{ get; set; }

        void Dispose();
        ValueTask DisposeAsync();
    }
    internal class AsyncQueryState : AsyncCommandState, IQueryState
    {
        DbDataReader? IQueryState.Reader => Reader;
        DbCommand? IQueryState.Command
        {
            get => Command;
            set => Command = value;
        }

        public DbDataReader? Reader;
        public int[]? Leased;
        private int fieldCount;

#pragma warning disable CS8774 // Member must have a non-null value when exiting. - validated
        [MemberNotNull(nameof(Reader))]
        public async new Task ExecuteReaderAsync(DbCommand command, CommandBehavior flags, CancellationToken cancellationToken)
            => Reader = await base.ExecuteReaderAsync(command, flags, cancellationToken);
#pragma warning restore CS8774 // Member must have a non-null value when exiting.

        public Span<int> Lease()
        {
            Debug.Assert(Reader is not null);
            fieldCount = Reader!.FieldCount;
            if (Leased is null || Leased.Length < fieldCount)
            {
                // no leased array, or existing lease is not big enough; rent a new array
                if (Leased is not null) ArrayPool<int>.Shared.Return(Leased);
                Leased = ArrayPool<int>.Shared.Rent(fieldCount);
            }
#if NET8_0_OR_GREATER
            return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(Leased), fieldCount);
#else
            return new Span<int>(Leased, 0, fieldCount);
#endif
        }

        public ReadOnlySpan<int> Tokens
        {
            get
            {
                Debug.Assert(Reader is not null && Leased is not null && Leased.Length >= Reader.FieldCount);
#if NET8_0_OR_GREATER
                return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(Leased), fieldCount);
#else
                return new Span<int>(Leased, 0, fieldCount);
#endif
            }
        }

        public void Return()
        {
            if (Leased is not null)
            {
                ArrayPool<int>.Shared.Return(Leased);
                Leased = null;
                fieldCount = 0;
            }
        }

        public override ValueTask DisposeAsync()
        {
            Return();
            if (Reader is not null)
            {
#if NETCOREAPP3_1_OR_GREATER
                var pending = Reader.DisposeAsync();
                if (pending.IsCompletedSuccessfully)
                {
                    pending.GetAwaiter().GetResult(); // ensure observed (for IValueTaskSource)
                }
                else
                {
                    return DisposeAsync(pending);
                }
#else
                Reader.Dispose();
#endif
            }
            return base.DisposeAsync();
        }
        private async ValueTask DisposeAsync(ValueTask pending)
        {
            await pending;
            await base.DisposeAsync();
        }

        public override void Dispose()
        {
            Return();
            Reader?.Dispose();
            base.Dispose();
        }
    }
}

#if !NET6_0_OR_GREATER

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    [Conditional("DEBUG")]
    file sealed class MemberNotNullAttribute : Attribute
    {
        /// <summary>Initializes the attribute with a field or property member.</summary>
        /// <param name="member">
        /// The field or property member that is promised to be not-null.
        /// </param>
        public MemberNotNullAttribute(string member) => Members = new[] { member };

        /// <summary>Initializes the attribute with the list of field and property members.</summary>
        /// <param name="members">
        /// The list of field and property members that are promised to be not-null.
        /// </param>
        public MemberNotNullAttribute(params string[] members) => Members = members;

        /// <summary>Gets field or property member names.</summary>
        public string[] Members { get; }
    }
}

#endif