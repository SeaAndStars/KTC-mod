namespace System.Runtime.CompilerServices
{
    /// <summary>Compiler-emitted attribute that describes nullability of the targeted member's value.</summary>
    [AttributeUsage(
        AttributeTargets.Class |
        AttributeTargets.Property |
        AttributeTargets.Field |
        AttributeTargets.Event |
        AttributeTargets.Parameter |
        AttributeTargets.ReturnValue |
        AttributeTargets.GenericParameter,
        AllowMultiple = false,
        Inherited = false)]
    internal sealed class NullableAttribute : Attribute
    {
        /// <summary>Creates nullability flags from a single byte value.</summary>
        public NullableAttribute(byte value) => NullableFlags = new[] { value };

        /// <summary>Creates nullability flags from an array of byte values.</summary>
        public NullableAttribute(byte[] value) => NullableFlags = value;

        /// <summary>Nullability flags describing whether each value type member is nullable.</summary>
        public readonly byte[] NullableFlags;
    }

    /// <summary>Compiler-emitted attribute describing the default nullability context of the targeted type.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    internal sealed class NullableContextAttribute : Attribute
    {
        /// <summary>Creates a nullability context flag from a single byte value.</summary>
        public NullableContextAttribute(byte value) => Flag = value;

        /// <summary>Flag indicating the nullability context (0 = oblivious, 1 = non-nullable, 2 = nullable).</summary>
        public readonly byte Flag;
    }
}
