﻿using System.Collections.Generic;
using System.Linq;
using Rubberduck.Inspections.Abstract;
using Rubberduck.Resources.Inspections;
using Rubberduck.Parsing.Symbols;
using Rubberduck.Parsing.VBA;
using Rubberduck.Inspections.Inspections.Extensions;
using Rubberduck.Inspections.Results;
using Rubberduck.Parsing.Grammar;
using Rubberduck.Parsing.Inspections;
using Rubberduck.Parsing.Inspections.Abstract;
using Rubberduck.Parsing.VBA.DeclarationCaching;
using Rubberduck.VBEditor;

namespace Rubberduck.Inspections.Concrete
{
    /// <summary>
    /// Identifies places in which an object is used but a procedure is required and a default member exists on the object.
    /// </summary>
    /// <why>
    /// Providing an object in a place in which a procedure is required leads to a call to the objects default member.
    /// This behavior is not obvious and most likely an error. 
    /// </why>
    /// <example hasResult="true">
    /// <![CDATA[
    /// Class1:
    ///
    /// Public Function Foo() As Long
    /// Attibute VB_UserMemId = 0
    ///     Foo = 42
    /// End Function
    ///
    /// Module:
    /// 
    /// Public Sub DoSomething(ByVal arg As Class1)
    ///     arg
    /// End Sub
    /// ]]>
    /// </example>
    /// <example hasResult="false">
    /// <![CDATA[
    /// Class1:
    ///
    /// Public Function Foo() As Long
    /// Attibute VB_UserMemId = 0
    ///     Foo = 42
    /// End Function
    ///
    /// Module:
    /// 
    /// Public Sub DoSomething(ByVal arg As Class1)
    ///     arg.Foo
    /// End Sub
    /// ]]>
    /// </example>
    public sealed class ObjectWhereProcedureIsRequiredInspection : InspectionBase
    {
        private readonly IDeclarationFinderProvider _declarationFinderProvider;

        public ObjectWhereProcedureIsRequiredInspection(RubberduckParserState state)
            : base(state)
        {
            _declarationFinderProvider = state;
            Severity = CodeInspectionSeverity.Warning;
        }

        protected override IEnumerable<IInspectionResult> DoGetInspectionResults()
        {
            var results = new List<IInspectionResult>();
            foreach (var moduleDeclaration in State.DeclarationFinder.UserDeclarations(DeclarationType.Module))
            {
                if (moduleDeclaration == null || moduleDeclaration.IsIgnoringInspectionResultFor(AnnotationName))
                {
                    continue;
                }

                var module = moduleDeclaration.QualifiedModuleName;
                results.AddRange(DoGetInspectionResults(module));
            }

            return results;
        }

        private IEnumerable<IInspectionResult> DoGetInspectionResults(QualifiedModuleName module)
        {
            var finder = _declarationFinderProvider.DeclarationFinder;
            return BoundInspectionResults(module, finder)
                .Concat(UnboundInspectionResults(module, finder));
        }

        private IEnumerable<IInspectionResult> BoundInspectionResults(QualifiedModuleName module, DeclarationFinder finder)
        {
            var objectionableReferences = finder
                .IdentifierReferences(module)
                .Where(IsResultReference);

            return objectionableReferences
                .Select(reference => BoundInspectionResult(reference, _declarationFinderProvider))
                .ToList();
        }

        private bool IsResultReference(IdentifierReference reference)
        {
            return reference.IsProcedureCoercion
                   && !reference.IsIgnoringInspectionResultFor(AnnotationName);
        }

        private IInspectionResult BoundInspectionResult(IdentifierReference reference, IDeclarationFinderProvider declarationFinderProvider)
        {
            return new IdentifierReferenceInspectionResult(
                this,
                BoundResultDescription(reference),
                declarationFinderProvider,
                reference);
        }

        private string BoundResultDescription(IdentifierReference reference)
        {
            var expression = reference.IdentifierName;
            var defaultMember = reference.Declaration.QualifiedName.ToString();
            return string.Format(InspectionResults.ObjectWhereProcedureIsRequiredInspection, expression, defaultMember);
        }

        private IEnumerable<IInspectionResult> UnboundInspectionResults(QualifiedModuleName module, DeclarationFinder finder)
        {
            var objectionableReferences = finder
                .UnboundDefaultMemberAccesses(module)
                .Where(IsResultReference);

            return objectionableReferences
                .Select(reference => UnboundInspectionResult(reference, _declarationFinderProvider))
                .ToList();
        }

        private IInspectionResult UnboundInspectionResult(IdentifierReference reference, IDeclarationFinderProvider declarationFinderProvider)
        {
            var result = new IdentifierReferenceInspectionResult(
                this,
                UnboundResultDescription(reference),
                declarationFinderProvider,
                reference);
            result.Properties.DisableFixes = "ExpandDefaultMemberQuickFix";
            return result;
        }

        private string UnboundResultDescription(IdentifierReference reference)
        {
            var expression = reference.IdentifierName;
            return string.Format(InspectionResults.ObjectWhereProcedureIsRequiredInspection_Unbound, expression);
        }
    }
}