#if !NET8_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, Inherited = false)]
    public sealed class CollectionBuilderAttribute : Attribute
    {
        public CollectionBuilderAttribute(Type builderType, string methodName)
        {
            BuilderType = builderType;
            MethodName = methodName;
        }

        public Type BuilderType { get; init; }
        public string MethodName { get; init; }
    }
}
#endif
