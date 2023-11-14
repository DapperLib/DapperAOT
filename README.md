Let's face it: ADO.NET is a complicated API, and writing "good" ADO.NET code by hand is time consuming and error-prone. But a lot of times you also don't want
the ceremony of an ORM like EF or LLBLGenPro - you just want to execute SQL!

For years now, Dapper helped by providing a great low-friction way of talking to arbitrary ADO.NET databases, handling command preparation, invocation, and result parsing.

Dapper.AOT radically changes how Dapper works, generating the necessary code *during build*, and offers a range of usage guidance to improve how you use Dapper.

[Getting Started](https://aot.dapperlib.dev/gettingstarted) | [Documentation](https://aot.dapperlib.dev/)