#nullable enable
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\NonFactoryMethod.input.cs", 14, 99)]
        internal static dynamic QueryFirst0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, HasParameters, SingleRow, Text, AtLeastOne, BindResultsByName, KnownParameters
            // takes parameter: <anonymous type: int productId>
            // parameter map: productId
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).QueryFirst(param, global::Dapper.RowFactory.Inbuilt.Dynamic);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\NonFactoryMethod.input.cs", 17, 92)]
        internal static global::UsageLinker.Product QueryFirst1(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, HasParameters, SingleRow, Text, AtLeastOne, BindResultsByName, KnownParameters
            // takes parameter: <anonymous type: int productId>
            // parameter map: productId
            // returns data: global::UsageLinker.Product
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).QueryFirst(param, RowFactory0.Instance);

        }

        private class CommonCommandFactory<T> : global::Dapper.CommandFactory<T>
        {
            public override global::System.Data.Common.DbCommand GetCommand(global::System.Data.Common.DbConnection connection, string sql, global::System.Data.CommandType commandType, T args)
            {
                var cmd = base.GetCommand(connection, sql, commandType, args);
                // apply special per-provider command initialization logic for OracleCommand
                if (cmd is global::Oracle.ManagedDataAccess.Client.OracleCommand cmd0)
                {
                    cmd0.BindByName = true;
                    cmd0.InitialLONGFetchSize = -1;

                }
                return cmd;
            }

        }

        private static readonly CommonCommandFactory<object?> DefaultCommandFactory = new();

        private sealed class RowFactory0 : global::Dapper.RowFactory<global::UsageLinker.Product>
        {
            internal static readonly RowFactory0 Instance = new();
            private RowFactory0() {}
            public override object? Tokenize(global::System.Data.Common.DbDataReader reader, global::System.Span<int> tokens, int columnOffset)
            {
                for (int i = 0; i < tokens.Length; i++)
                {
                    int token = -1;
                    var name = reader.GetName(columnOffset);
                    var type = reader.GetFieldType(columnOffset);
                    switch (NormalizedHash(name))
                    {
                        case 2521315361U when NormalizedEquals(name, "productid"):
                            token = type == typeof(int) ? 0 : 25; // two tokens for right-typed and type-flexible
                            break;
                        case 2369371622U when NormalizedEquals(name, "name"):
                            token = type == typeof(string) ? 1 : 26;
                            break;
                        case 1133313085U when NormalizedEquals(name, "productnumber"):
                            token = type == typeof(string) ? 2 : 27;
                            break;
                        case 1711670609U when NormalizedEquals(name, "makeflag"):
                            token = type == typeof(bool) ? 3 : 28;
                            break;
                        case 1559940253U when NormalizedEquals(name, "finishedgoodsflag"):
                            token = type == typeof(bool) ? 4 : 29;
                            break;
                        case 1031692888U when NormalizedEquals(name, "color"):
                            token = type == typeof(string) ? 5 : 30;
                            break;
                        case 3319250123U when NormalizedEquals(name, "safetystocklevel"):
                            token = type == typeof(short) ? 6 : 31;
                            break;
                        case 1418663774U when NormalizedEquals(name, "reorderpoint"):
                            token = type == typeof(short) ? 7 : 32;
                            break;
                        case 3014405261U when NormalizedEquals(name, "standardcost"):
                            token = type == typeof(decimal) ? 8 : 33;
                            break;
                        case 2811106182U when NormalizedEquals(name, "listprice"):
                            token = type == typeof(decimal) ? 9 : 34;
                            break;
                        case 597743964U when NormalizedEquals(name, "size"):
                            token = type == typeof(string) ? 10 : 35;
                            break;
                        case 2980199683U when NormalizedEquals(name, "sizeunitmeasurecode"):
                            token = type == typeof(string) ? 11 : 36;
                            break;
                        case 3982800768U when NormalizedEquals(name, "weightunitmeasurecode"):
                            token = type == typeof(string) ? 12 : 37;
                            break;
                        case 1352703673U when NormalizedEquals(name, "weight"):
                            token = type == typeof(decimal) ? 13 : 38;
                            break;
                        case 3356678568U when NormalizedEquals(name, "daystomanufacture"):
                            token = type == typeof(int) ? 14 : 39;
                            break;
                        case 2315215726U when NormalizedEquals(name, "productline"):
                            token = type == typeof(string) ? 15 : 40;
                            break;
                        case 2872970239U when NormalizedEquals(name, "class"):
                            token = type == typeof(string) ? 16 : 41;
                            break;
                        case 2888859350U when NormalizedEquals(name, "style"):
                            token = type == typeof(string) ? 17 : 42;
                            break;
                        case 4252224555U when NormalizedEquals(name, "productsubcategoryid"):
                            token = type == typeof(int) ? 18 : 43;
                            break;
                        case 4100243804U when NormalizedEquals(name, "productmodelid"):
                            token = type == typeof(int) ? 19 : 44;
                            break;
                        case 3344591163U when NormalizedEquals(name, "sellstartdate"):
                            token = type == typeof(global::System.DateTime) ? 20 : 45;
                            break;
                        case 1349254478U when NormalizedEquals(name, "sellenddate"):
                            token = type == typeof(global::System.DateTime) ? 21 : 46;
                            break;
                        case 3790794106U when NormalizedEquals(name, "discontinueddate"):
                            token = type == typeof(global::System.DateTime) ? 22 : 47;
                            break;
                        case 2427359870U when NormalizedEquals(name, "rowguid"):
                            token = type == typeof(global::System.Guid) ? 23 : 48;
                            break;
                        case 1611473142U when NormalizedEquals(name, "modifieddate"):
                            token = type == typeof(global::System.DateTime) ? 24 : 49;
                            break;

                    }
                    tokens[i] = token;
                    columnOffset++;

                }
                return null;
            }
            public override global::UsageLinker.Product Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::UsageLinker.Product result = new();
                foreach (var token in tokens)
                {
                    switch (token)
                    {
                        case 0:
                            result.ProductID = reader.GetInt32(columnOffset);
                            break;
                        case 25:
                            result.ProductID = GetValue<int>(reader, columnOffset);
                            break;
                        case 1:
                            result.Name = reader.GetString(columnOffset);
                            break;
                        case 26:
                            result.Name = GetValue<string>(reader, columnOffset);
                            break;
                        case 2:
                            result.ProductNumber = reader.GetString(columnOffset);
                            break;
                        case 27:
                            result.ProductNumber = GetValue<string>(reader, columnOffset);
                            break;
                        case 3:
                            result.MakeFlag = reader.GetBoolean(columnOffset);
                            break;
                        case 28:
                            result.MakeFlag = GetValue<bool>(reader, columnOffset);
                            break;
                        case 4:
                            result.FinishedGoodsFlag = reader.GetBoolean(columnOffset);
                            break;
                        case 29:
                            result.FinishedGoodsFlag = GetValue<bool>(reader, columnOffset);
                            break;
                        case 5:
                            result.Color = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;
                        case 30:
                            result.Color = reader.IsDBNull(columnOffset) ? (string?)null : GetValue<string>(reader, columnOffset);
                            break;
                        case 6:
                            result.SafetyStockLevel = reader.GetInt16(columnOffset);
                            break;
                        case 31:
                            result.SafetyStockLevel = GetValue<short>(reader, columnOffset);
                            break;
                        case 7:
                            result.ReorderPoint = reader.GetInt16(columnOffset);
                            break;
                        case 32:
                            result.ReorderPoint = GetValue<short>(reader, columnOffset);
                            break;
                        case 8:
                            result.StandardCost = reader.GetDecimal(columnOffset);
                            break;
                        case 33:
                            result.StandardCost = GetValue<decimal>(reader, columnOffset);
                            break;
                        case 9:
                            result.ListPrice = reader.GetDecimal(columnOffset);
                            break;
                        case 34:
                            result.ListPrice = GetValue<decimal>(reader, columnOffset);
                            break;
                        case 10:
                            result.Size = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;
                        case 35:
                            result.Size = reader.IsDBNull(columnOffset) ? (string?)null : GetValue<string>(reader, columnOffset);
                            break;
                        case 11:
                            result.SizeUnitMeasureCode = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;
                        case 36:
                            result.SizeUnitMeasureCode = reader.IsDBNull(columnOffset) ? (string?)null : GetValue<string>(reader, columnOffset);
                            break;
                        case 12:
                            result.WeightUnitMeasureCode = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;
                        case 37:
                            result.WeightUnitMeasureCode = reader.IsDBNull(columnOffset) ? (string?)null : GetValue<string>(reader, columnOffset);
                            break;
                        case 13:
                            result.Weight = reader.IsDBNull(columnOffset) ? (decimal?)null : reader.GetDecimal(columnOffset);
                            break;
                        case 38:
                            result.Weight = reader.IsDBNull(columnOffset) ? (decimal?)null : GetValue<decimal>(reader, columnOffset);
                            break;
                        case 14:
                            result.DaysToManufacture = reader.GetInt32(columnOffset);
                            break;
                        case 39:
                            result.DaysToManufacture = GetValue<int>(reader, columnOffset);
                            break;
                        case 15:
                            result.ProductLine = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;
                        case 40:
                            result.ProductLine = reader.IsDBNull(columnOffset) ? (string?)null : GetValue<string>(reader, columnOffset);
                            break;
                        case 16:
                            result.Class = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;
                        case 41:
                            result.Class = reader.IsDBNull(columnOffset) ? (string?)null : GetValue<string>(reader, columnOffset);
                            break;
                        case 17:
                            result.Style = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;
                        case 42:
                            result.Style = reader.IsDBNull(columnOffset) ? (string?)null : GetValue<string>(reader, columnOffset);
                            break;
                        case 18:
                            result.ProductSubcategoryID = reader.IsDBNull(columnOffset) ? (int?)null : reader.GetInt32(columnOffset);
                            break;
                        case 43:
                            result.ProductSubcategoryID = reader.IsDBNull(columnOffset) ? (int?)null : GetValue<int>(reader, columnOffset);
                            break;
                        case 19:
                            result.ProductModelID = reader.IsDBNull(columnOffset) ? (int?)null : reader.GetInt32(columnOffset);
                            break;
                        case 44:
                            result.ProductModelID = reader.IsDBNull(columnOffset) ? (int?)null : GetValue<int>(reader, columnOffset);
                            break;
                        case 20:
                            result.SellStartDate = reader.GetDateTime(columnOffset);
                            break;
                        case 45:
                            result.SellStartDate = GetValue<global::System.DateTime>(reader, columnOffset);
                            break;
                        case 21:
                            result.SellEndDate = reader.IsDBNull(columnOffset) ? (global::System.DateTime?)null : reader.GetDateTime(columnOffset);
                            break;
                        case 46:
                            result.SellEndDate = reader.IsDBNull(columnOffset) ? (global::System.DateTime?)null : GetValue<global::System.DateTime>(reader, columnOffset);
                            break;
                        case 22:
                            result.DiscontinuedDate = reader.IsDBNull(columnOffset) ? (global::System.DateTime?)null : reader.GetDateTime(columnOffset);
                            break;
                        case 47:
                            result.DiscontinuedDate = reader.IsDBNull(columnOffset) ? (global::System.DateTime?)null : GetValue<global::System.DateTime>(reader, columnOffset);
                            break;
                        case 23:
                            result.RowGuid = reader.GetGuid(columnOffset);
                            break;
                        case 48:
                            result.RowGuid = GetValue<global::System.Guid>(reader, columnOffset);
                            break;
                        case 24:
                            result.ModifiedDate = reader.GetDateTime(columnOffset);
                            break;
                        case 49:
                            result.ModifiedDate = GetValue<global::System.DateTime>(reader, columnOffset);
                            break;

                    }
                    columnOffset++;

                }
                return result;

            }

        }

        private sealed class CommandFactory0 : CommonCommandFactory<object?> // <anonymous type: int productId>
        {
            internal static readonly CommandFactory0 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { productId = default(int) }); // expected shape
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "productId";
                p.DbType = global::System.Data.DbType.Int32;
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(typed.productId);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { productId = default(int) }); // expected shape
                var ps = cmd.Parameters;
                ps[0].Value = AsValue(typed.productId);

            }
            public override bool CanPrepare => true;

        }


    }
}
namespace System.Runtime.CompilerServices
{
    // this type is needed by the compiler to implement interceptors - it doesn't need to
    // come from the runtime itself, though

    [global::System.Diagnostics.Conditional("DEBUG")] // not needed post-build, so: evaporate
    [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]
    sealed file class InterceptsLocationAttribute : global::System.Attribute
    {
        public InterceptsLocationAttribute(string path, int lineNumber, int columnNumber)
        {
            _ = path;
            _ = lineNumber;
            _ = columnNumber;
        }
    }
}