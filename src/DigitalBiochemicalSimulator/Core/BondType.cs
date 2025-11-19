namespace DigitalBiochemicalSimulator.Core
{
    /// <summary>
    /// Represents the type of chemical bond between tokens.
    /// Based on section 3.4.1 of the design specification.
    /// </summary>
    public enum BondType
    {
        /// <summary>
        /// Strong bonds (0.9-1.0): Grammar-mandated pairs like (), {}, type declarations
        /// </summary>
        COVALENT,

        /// <summary>
        /// Medium bonds (0.6-0.9): Keyword-context, operator-operand pairs
        /// </summary>
        IONIC,

        /// <summary>
        /// Weak bonds (0.1-0.6): Stylistic separators, whitespace conventions
        /// </summary>
        VAN_DER_WAALS
    }
}
