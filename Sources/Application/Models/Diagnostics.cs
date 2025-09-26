using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Mmu.Mlra.ReferenceBuddy.Services;

namespace Mmu.Mlra.ReferenceBuddy.Models
{
    internal static class Diagnostics
    {
        public const string RefNotAllowedId = "RefBuddy001";
        public const string RuleFileInvalidId = "RefBuddy002";

        public static readonly DiagnosticDescriptor ReferenceNotAllowedDiagnostic = new DiagnosticDescriptor(
            RefNotAllowedId,
            "Reference is not allowed",
            "A reference between '{0}' and '{1}' is not allowed",
            "ReferenceBuddy",
            DiagnosticSeverity.Error,
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
            DiagnosticSeverity.Error,
            true,
            $"There must be exactly one file named {RulesFileFactory.FileName}.");
    }
}