using System.Diagnostics;
using System.Runtime.CompilerServices;

[module: SkipLocalsInit]
[assembly:InternalsVisibleTo("Dapper.AOT.Test, PublicKey=0024000004800000940000000602000000240000525341310004000001000100a17ba361da0990b3da23f3c20f2a002242397b452a28f27832d61d49f35edb54a68b98d98557b8a02be79be42142339c7861af309c8917dee972775e2c358dd6b96109a9147987652b25b8dc52e7f61f22a755831674f0a3cea17bef9abb6b23ef1856a02216864a1ffbb04a4c549258d32ba740fe141dad2f298a8130ea56d0")]

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    [Conditional("DEBUG")] // not needed post-build, so: evaporate
    [AttributeUsage(AttributeTargets.Module)]
    sealed file class SkipLocalsInitAttribute : Attribute
    {
    }
}
#endif