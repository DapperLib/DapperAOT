using System;
using System.Collections.Generic;

namespace Dapper.AOT.Test.Integration.Setup;

class ExpressionParsingException(IEnumerable<string> errors)
    : Exception(string.Join(Environment.NewLine, errors))
{
}