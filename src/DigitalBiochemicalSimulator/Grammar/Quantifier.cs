namespace DigitalBiochemicalSimulator.Grammar
{
    /// <summary>
    /// Quantifier for token patterns in grammar rules.
    /// Based on section 3.5.1 of the design specification.
    /// </summary>
    public enum Quantifier
    {
        /// <summary>
        /// Exactly one token required
        /// </summary>
        ONE,

        /// <summary>
        /// Zero or one token (optional)
        /// </summary>
        OPTIONAL,

        /// <summary>
        /// Zero or more tokens
        /// </summary>
        ZERO_OR_MORE,

        /// <summary>
        /// One or more tokens
        /// </summary>
        ONE_OR_MORE
    }
}
