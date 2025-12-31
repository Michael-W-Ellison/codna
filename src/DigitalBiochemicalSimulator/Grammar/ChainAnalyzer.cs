using System;
using System.Collections.Generic;
using DigitalBiochemicalSimulator.Core;

namespace DigitalBiochemicalSimulator.Grammar
{
    /// <summary>
    /// Comprehensive chain analyzer that integrates grammar, AST, and semantic validation.
    /// Provides deep analysis of token chains for code correctness.
    /// </summary>
    public class ChainAnalyzer
    {
        private readonly SemanticValidator _semanticValidator;

        public ChainAnalyzer()
        {
            _semanticValidator = new SemanticValidator();
        }

        /// <summary>
        /// Performs comprehensive analysis on a token chain
        /// </summary>
        public ComprehensiveAnalysisResult AnalyzeChain(TokenChain chain)
        {
            var result = new ComprehensiveAnalysisResult
            {
                ChainId = chain.Id,
                Length = chain.Length,
                CodeString = chain.ToCodeString()
            };

            // Step 1: Build AST
            try
            {
                result.AST = chain.BuildAST();
                result.ASTBuilt = result.AST != null;

                if (result.AST != null)
                {
                    result.ASTRepresentation = result.AST.ToString();
                }
            }
            catch (Exception ex)
            {
                result.ASTBuilt = false;
                result.ASTError = $"AST building failed: {ex.Message}";
            }

            // Step 2: Perform semantic validation
            try
            {
                result.SemanticResult = _semanticValidator.ValidateChain(chain);
                result.SemanticValid = result.SemanticResult.IsValid;
            }
            catch (Exception ex)
            {
                result.SemanticValid = false;
                result.SemanticResult = new SemanticValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { $"Semantic validation failed: {ex.Message}" },
                    Warnings = new List<string>()
                };
            }

            // Step 3: Grammar validation (from chain's built-in validation)
            var grammarResult = chain.Validate();
            result.GrammarValid = grammarResult.IsValid;
            result.GrammarErrors = grammarResult.Errors;

            // Step 4: Calculate overall validity
            result.IsFullyValid = result.GrammarValid && result.SemanticValid && result.ASTBuilt;

            // Step 5: Calculate quality score
            result.QualityScore = CalculateQualityScore(result);

            return result;
        }

        /// <summary>
        /// Calculates a quality score for the chain (0.0 - 1.0)
        /// </summary>
        private float CalculateQualityScore(ComprehensiveAnalysisResult result)
        {
            float score = 0.0f;

            // Grammar validity: 40%
            if (result.GrammarValid)
                score += 0.4f;

            // Semantic validity: 40%
            if (result.SemanticValid)
                score += 0.4f;

            // AST successfully built: 20%
            if (result.ASTBuilt)
                score += 0.2f;

            // Penalty for warnings (reduce by 5% per warning, max 20% reduction)
            if (result.SemanticResult != null)
            {
                int warnings = result.SemanticResult.Warnings.Count;
                float warningPenalty = Math.Min(warnings * 0.05f, 0.2f);
                score -= warningPenalty;
            }

            return Math.Clamp(score, 0.0f, 1.0f);
        }

        /// <summary>
        /// Analyzes multiple chains and returns sorted by quality
        /// </summary>
        public List<ComprehensiveAnalysisResult> AnalyzeMultipleChains(List<TokenChain> chains)
        {
            var results = new List<ComprehensiveAnalysisResult>();

            foreach (var chain in chains)
            {
                results.Add(AnalyzeChain(chain));
            }

            // Sort by quality score descending
            results.Sort((a, b) => b.QualityScore.CompareTo(a.QualityScore));

            return results;
        }

        /// <summary>
        /// Gets chains that are fully valid (grammar + semantics + AST)
        /// </summary>
        public List<TokenChain> GetFullyValidChains(List<TokenChain> chains)
        {
            var validChains = new List<TokenChain>();

            foreach (var chain in chains)
            {
                var result = AnalyzeChain(chain);
                if (result.IsFullyValid)
                {
                    validChains.Add(chain);
                }
            }

            return validChains;
        }
    }

    /// <summary>
    /// Comprehensive analysis result including grammar, AST, and semantics
    /// </summary>
    public class ComprehensiveAnalysisResult
    {
        public long ChainId { get; set; }
        public int Length { get; set; }
        public string CodeString { get; set; }

        // Grammar validation
        public bool GrammarValid { get; set; }
        public List<string> GrammarErrors { get; set; }

        // AST analysis
        public bool ASTBuilt { get; set; }
        public ASTNode AST { get; set; }
        public string ASTRepresentation { get; set; }
        public string ASTError { get; set; }

        // Semantic validation
        public bool SemanticValid { get; set; }
        public SemanticValidationResult SemanticResult { get; set; }

        // Overall
        public bool IsFullyValid { get; set; }
        public float QualityScore { get; set; }

        public ComprehensiveAnalysisResult()
        {
            GrammarErrors = new List<string>();
        }

        public override string ToString()
        {
            return $"Chain {ChainId}: Quality={QualityScore:F2}, " +
                   $"Grammar={GrammarValid}, AST={ASTBuilt}, Semantic={SemanticValid}, " +
                   $"Code=\"{CodeString}\"";
        }

        /// <summary>
        /// Gets a detailed report of the analysis
        /// </summary>
        public string GetDetailedReport()
        {
            var report = new System.Text.StringBuilder();

            report.AppendLine($"=== Chain Analysis Report ===");
            report.AppendLine($"Chain ID: {ChainId}");
            report.AppendLine($"Length: {Length}");
            report.AppendLine($"Code: \"{CodeString}\"");
            report.AppendLine($"Overall Quality: {QualityScore:F2}/1.0");
            report.AppendLine($"Fully Valid: {(IsFullyValid ? "YES" : "NO")}");
            report.AppendLine();

            // Grammar section
            report.AppendLine($"Grammar Validation: {(GrammarValid ? "PASS" : "FAIL")}");
            if (!GrammarValid && GrammarErrors.Count > 0)
            {
                report.AppendLine("  Errors:");
                foreach (var error in GrammarErrors)
                {
                    report.AppendLine($"    - {error}");
                }
            }
            report.AppendLine();

            // AST section
            report.AppendLine($"AST Building: {(ASTBuilt ? "SUCCESS" : "FAILED")}");
            if (ASTBuilt && !string.IsNullOrEmpty(ASTRepresentation))
            {
                report.AppendLine($"  AST: {ASTRepresentation}");
            }
            else if (!string.IsNullOrEmpty(ASTError))
            {
                report.AppendLine($"  Error: {ASTError}");
            }
            report.AppendLine();

            // Semantic section
            report.AppendLine($"Semantic Validation: {(SemanticValid ? "PASS" : "FAIL")}");
            if (SemanticResult != null)
            {
                if (SemanticResult.Errors.Count > 0)
                {
                    report.AppendLine("  Errors:");
                    foreach (var error in SemanticResult.Errors)
                    {
                        report.AppendLine($"    - {error}");
                    }
                }
                if (SemanticResult.Warnings.Count > 0)
                {
                    report.AppendLine("  Warnings:");
                    foreach (var warning in SemanticResult.Warnings)
                    {
                        report.AppendLine($"    - {warning}");
                    }
                }
            }

            report.AppendLine("=============================");

            return report.ToString();
        }
    }
}
