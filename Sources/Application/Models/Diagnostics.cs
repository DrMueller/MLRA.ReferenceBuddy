using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Mmu.Mlra.ReferenceBuddy.Services;

namespace Mmu.Mlra.ReferenceBuddy.Models
{
    public static class Diagnostics
    {
        public const string RefNotAllowedId = "RefBuddy001";
        private const string RuleFileInvalidId = "RefBuddy002";

        public static DiagnosticDescriptor ReferenceNotAllowedDiagnostic { get; } = new DiagnosticDescriptor(
            RefNotAllowedId,
            "Reference is not allowed",
            "A reference between '{0}' and '{1}' is not allowed",
            "ReferenceBuddy",
            DiagnosticSeverity.Warning,
            true,
            "Reference is not allowed.");

        public static ImmutableArray<DiagnosticDescriptor> All => ImmutableArray.Create(
            ReferenceNotAllowedDiagnostic,
            NoRulesFileFoundDiagnostic);

        public static DiagnosticDescriptor NoRulesFileFoundDiagnostic { get; } = new DiagnosticDescriptor(
            RuleFileInvalidId,
            "No or invalid rules files found",
            "Ensure exactly one rule file exists per project",
            "ReferenceBuddy",
            DiagnosticSeverity.Warning,
            true,
            $"There must be exactly one file named {RulesFileFactory.FileName}.");
    }
}