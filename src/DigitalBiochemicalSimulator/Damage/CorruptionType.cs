namespace DigitalBiochemicalSimulator.Damage
{
    /// <summary>
    /// Types of metadata corruption that can occur to tokens.
    /// Based on section 4.1 of the design specification.
    /// </summary>
    public enum CorruptionType
    {
        /// <summary>
        /// No corruption present
        /// </summary>
        NONE = 0,

        /// <summary>
        /// Metadata becomes garbled/unclear but structure remains
        /// Example: "int" -> "i@t", "function" -> "fun€tion"
        /// </summary>
        OBFUSCATION = 1,

        /// <summary>
        /// Token type or value mutates to a different but valid type
        /// Example: "int" -> "float", "+" -> "-", "5" -> "7"
        /// </summary>
        MUTATION = 2,

        /// <summary>
        /// Metadata is partially or completely erased
        /// Example: "identifier" -> "", electronegativity -> 0.0
        /// </summary>
        ERASURE = 3,

        /// <summary>
        /// Multiple corruption types combined
        /// </summary>
        COMPOUND = 4
    }
}
