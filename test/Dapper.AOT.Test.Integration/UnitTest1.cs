using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Dapper.AOT.Test.Integration.Executables.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Data.SqlClient;

namespace Dapper.AOT.Test.Integration;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        string connection = "data source=DMKOR_PC;initial catalog=dapper-experiments;trusted_connection=true;TrustServerCertificate=True";
        IDbConnection dbConnection = new SqlConnection(connection);
        // var items = dbConnection.Query<Poco>("select * from Product");
        // var res = items.FirstOrDefault()?.Name;

        var userCode = Parse("UserCode.cs", $$"""
                                              namespace ProgramNamespace
                                              {
                                                  using System;
                                                  using System.Linq;
                                                  using System.Data;
                                                  using Dapper;
                                                  using Dapper.AOT.Test.Integration.Executables.Models;
                                              
                                                  public static class Program
                                                  {
                                                      public static DbStringPoco RunCode(IDbConnection dbConnection)
                                                      {
                                                          var results = dbConnection.Query<DbStringPoco>("select * from table");
                                                          return results.First();
                                                      }
                                                  }
                                              }
                                              """);

        var generatedCode = Parse("AnotherFile.cs", $$"""
                                                       namespace Dapper.AOT
                                                       {
                                                          file static class D
                                                          {
                                                              [System.Runtime.CompilerServices.InterceptsLocation(path: "UserCode.cs", lineNumber: 13, columnNumber: 40)]
                                                              internal static global::System.Collections.Generic.IEnumerable<global::Dapper.AOT.Test.Integration.Executables.Models.DbStringPoco> Query0(
                                                                    this global::System.Data.IDbConnection cnn,
                                                                    string sql,
                                                                    object? param = null,
                                                                    global::System.Data.IDbTransaction? transaction = null, bool buffered = true, int? commandTimeout = null, global::System.Data.CommandType? commandType = null)
                                                              {
                                                                  return new global::System.Collections.Generic.List<global::Dapper.AOT.Test.Integration.Executables.Models.DbStringPoco>()  
                                                                  {
                                                                      new global::Dapper.AOT.Test.Integration.Executables.Models.DbStringPoco() { Name = "something" }
                                                                  };
                                                              }
                                                          }  
                                                          
                                                       }
                                                      """);

        var interceptorsLocationAttributeCode = Parse("InterceptorsLocationAttribute.cs", $$"""
                  namespace System.Runtime.CompilerServices
                  {
                      [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
                      public sealed class InterceptsLocationAttribute(string path, int lineNumber, int columnNumber) : Attribute
                      {
                      }
                  }                                        
              """);

        var compilation = CSharpCompilation.Create(
            assemblyName: "Test.dll",
            syntaxTrees: [interceptorsLocationAttributeCode, userCode, generatedCode],
            references: [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location), // System.Runtime ?
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location), // System.Runtime ?
                
                MetadataReference.CreateFromFile(typeof(Dapper.SqlMapper).Assembly.Location), // Dapper
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location), // System.Linq
                
                MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location), // System.Data.Common
                
                MetadataReference.CreateFromFile(typeof(Dapper.AOT.Test.Integration.Executables.UserCode.DbStringUsage).Assembly.Location), // Dapper.AOT.Test.Integration.Executables 
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));


        var assembly = Compile(compilation);
        var type = assembly.GetTypes().Single(t => t.FullName == "ProgramNamespace.Program");
        var mainMethod = type.GetMethod("RunCode", BindingFlags.Public | BindingFlags.Static);
        var result = mainMethod!.Invoke(obj: null, [ dbConnection ]);

        Assert.NotNull(result);
        if (result is not DbStringPoco typedResult)
        {
            Assert.Fail($"invocation result is expected to be of type {typeof(DbStringPoco)}");
            return;
        }
        
        Assert.True(typedResult.Name.Equals("something", StringComparison.InvariantCultureIgnoreCase));
    }
    
    Assembly Compile(Compilation compilation)
    {
        using var peStream = new MemoryStream();
        using var pdbstream = new MemoryStream();
        var dbg = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? DebugInformationFormat.Pdb : DebugInformationFormat.PortablePdb;
        
        var emitResult = compilation.Emit(peStream, pdbstream, null, null, null, new EmitOptions(false, dbg));
        if (!emitResult.Success)
        {
            TryThrowErrors(emitResult.Diagnostics);
        }

        peStream.Position = pdbstream.Position = 0;
        return Assembly.Load(peStream.ToArray(), pdbstream.ToArray());
    }

    void TryThrowErrors(IEnumerable<Diagnostic> items)
    {
        var errors = new List<string>();
        foreach (var item in items)
        {
            if (item.Severity == DiagnosticSeverity.Error)
            {
                errors.Add(item.GetMessage(CultureInfo.InvariantCulture));
            }
        }

        if (errors.Count > 0)
        {
            throw new ExpressionParsingException(errors);
        }
    }

    SyntaxTree Parse(string filename, string text)
    {
        var options = new CSharpParseOptions(LanguageVersion.Preview)
            .WithFeatures(new[]
            {
                new KeyValuePair<string, string>("InterceptorsPreviewNamespaces", "$(InterceptorsPreviewNamespaces);Dapper.AOT"),
            });

        var stringText = SourceText.From(text, Encoding.UTF8);
        return SyntaxFactory.ParseSyntaxTree(stringText, options, filename);
    }

    public class Poco
    {
        public string Name { get; set; }
    }

    public class ExpressionParsingException : Exception
    {
        public IEnumerable<string> Errors { get; private set; }

        public ExpressionParsingException(IEnumerable<string> errors)
            : base(string.Join(Environment.NewLine, errors))
        {
            this.Errors = errors;
        }

        public ExpressionParsingException(params string[] errors)
            : base(string.Join(Environment.NewLine, errors))
        {
            this.Errors = errors;
        }
    }
}