﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Rubberduck.Inspections.Abstract;
using Rubberduck.Inspections.Concrete;
using Rubberduck.Parsing.Annotations;
using Rubberduck.Parsing.Inspections.Abstract;
using Rubberduck.Parsing.Rewriter;
using Rubberduck.Parsing.Symbols;
using Rubberduck.Parsing.VBA;
using Rubberduck.Parsing.VBA.Parsing;

namespace Rubberduck.Inspections.QuickFixes
{
    /// <summary>
    /// Adjusts existing hidden attributes to match the corresponding Rubberduck annotations.
    /// </summary>
    /// <inspections>
    /// <inspection name="AttributeValueOutOfSyncInspection" />
    /// </inspections>
    /// <canfix procedure="true" module="true" project="true" />
    /// <example>
    /// <before>
    /// <![CDATA[
    /// Attribute VB_PredeclaredId = False
    /// '@PredeclaredId
    /// 
    /// Option Explicit
    /// 
    /// '@Description("Does something.")
    /// Public Sub DoSomething()
    /// Attribute VB_Description = "Does something else."
    /// 
    /// End Sub
    /// ]]>
    /// </before>
    /// <after>
    /// <![CDATA[
    /// Attribute VB_PredeclaredId = True
    /// '@PredeclaredId
    /// 
    /// Option Explicit
    /// 
    /// '@Description("Does something.")
    /// Public Sub DoSomething()
    /// Attribute VB_Description = "Does something."
    /// 
    /// End Sub
    /// ]]>
    /// </after>
    /// </example>
    public class AdjustAttributeValuesQuickFix : QuickFixBase
    {
        private readonly IAttributesUpdater _attributesUpdater;

        public AdjustAttributeValuesQuickFix(IAttributesUpdater attributesUpdater)
            : base(typeof(AttributeValueOutOfSyncInspection))
        {
            _attributesUpdater = attributesUpdater;
        }

        public override void Fix(IInspectionResult result, IRewriteSession rewriteSession)
        {
            var declaration = result.Target;
            IParseTreeAnnotation annotationInstance = result.Properties.Annotation;

            Debug.Assert(annotationInstance.Annotation is IAttributeAnnotation);
            IAttributeAnnotation annotation = (IAttributeAnnotation)annotationInstance.Annotation;
            IReadOnlyList<string> attributeValues = result.Properties.AttributeValues;

            var attribute = annotation.Attribute(annotationInstance);
            var attributeName = declaration.DeclarationType.HasFlag(DeclarationType.Module)
                ? attribute
                : $"{declaration.IdentifierName}.{attribute}";

            _attributesUpdater.UpdateAttribute(rewriteSession, declaration, attributeName, annotation.AttributeValues(annotationInstance), oldValues: attributeValues);
        }

        public override string Description(IInspectionResult result) => Resources.Inspections.QuickFixes.AdjustAttributeValuesQuickFix;

        public override CodeKind TargetCodeKind => CodeKind.AttributesCode;

        public override bool CanFixInProcedure => true;
        public override bool CanFixInModule => true;
        public override bool CanFixInProject => true;
    }
}