namespace Dapper
{
    internal sealed class AotGridReader : global::Dapper.SqlMapper.GridReader
    {
        public AotGridReader(global::System.Data.IDbCommand command, global::System.Data.Common.DbDataReader reader,
            global::System.Action<object?>? onCompleted = null, object? state = null,
            global::System.Threading.CancellationToken cancellationToken = default)
            : base(command, reader, null!, onCompleted, state, false, cancellationToken) { }

        internal new int ResultIndex => base.ResultIndex;

        internal new global::System.Data.Common.DbDataReader Reader => base.Reader;

        internal new void MarkNextResult() => base.MarkNextResult();

        internal new void MarkConsumed() => base.MarkConsumed();

        internal new global::System.Threading.CancellationToken CancellationToken => base.CancellationToken;
    }
}
