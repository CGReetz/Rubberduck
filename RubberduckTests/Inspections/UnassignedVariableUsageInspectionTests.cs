using System.Linq;
using NUnit.Framework;
using Rubberduck.Inspections.Concrete;
using Rubberduck.Parsing.Inspections.Abstract;
using Rubberduck.Parsing.VBA;
using Rubberduck.VBEditor.SafeComWrappers;

namespace RubberduckTests.Inspections
{
    [TestFixture]
    public class UnassignedVariableUsageInspectionTests : InspectionTestsBase
    {
        [Test]
        [Category("Inspections")]
        public void IgnoresExplicitArrays()
        {
            const string code = @"
Sub Foo()
    Dim bar() As String
    bar(1) = ""value""
End Sub
";
            Assert.AreEqual(0, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void IgnoresArrayReDim()
        {
            const string code = @"
Sub Foo()
    Dim bar As Variant
    ReDim bar(1 To 10)
End Sub
";
            Assert.AreEqual(0, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void IgnoresArraySubscripts()
        {
            const string code = @"
Sub Foo()
    Dim bar As Variant
    ReDim bar(1 To 10)
    bar(1) = 42
End Sub
";
            Assert.AreEqual(0, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_ReturnsResult()
        {
            const string code = @"
Sub Foo()
    Dim b As Boolean
    Dim bb As Boolean
    bb = b
End Sub
";
            Assert.AreEqual(1, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_DoesNotReturnResult()
        {
            const string code = @"
Sub Foo()
    Dim b As Boolean
    Dim bb As Boolean
    b = True
    bb = b
End Sub
";

            Assert.AreEqual(0, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_Ignored_DoesNotReturnResult()
        {
            const string code = @"
Sub Foo()
    Dim b As Boolean
    Dim bb As Boolean

'@Ignore UnassignedVariableUsage
    bb = b
End Sub
";
            Assert.AreEqual(0, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_Ignored_DoesNotReturnResultMultipleIgnores()
        {
            const string code = @"
Sub Foo()    
    Dim b As Boolean
    Dim bb As Boolean

'@Ignore UnassignedVariableUsage, VariableNotAssigned
    bb = b
End Sub
";
            Assert.AreEqual(0, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Ignore("Test is green if executed manually, red otherwise. Possible concurrency issue?")]
        [Category("Inspections")]
        public void UnassignedVariableUsage_NoResultForAssignedByRefReference()
        {
            const string code = @"
Sub DoSomething()
    Dim foo
    AssignThing foo
    Dim bar As Variant
    bar = foo
End Sub

Sub AssignThing(ByRef thing As Variant)
    thing = 42
End Sub
";
            Assert.AreEqual(0, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_NoResultIfNoReferences()
        {
            const string code = @"
Sub DoSomething()
    Dim foo
End Sub
";
            Assert.AreEqual(0, InspectionResultsForStandardModule(code).Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_NoResultForLenFunction()
        {
            const string code = @"
Sub DoSomething()
    Dim foo As LongPtr
    Dim bar As Variant
    bar = Len(foo)
End Sub
";
            var inspectionResults = InspectionResultsForModules(("TestModule", code, ComponentType.StandardModule), "VBA");
            Assert.AreEqual(0, inspectionResults.Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_NoResultForLenBFunction()
        {
            const string code = @"
Sub DoSomething()
    Dim foo As LongPtr
    Dim bar As Variant
    bar = LenB(foo)
End Sub
";
            var inspectionResults = InspectionResultsForModules(("TestModule", code, ComponentType.StandardModule), "VBA");
            Assert.AreEqual(0, inspectionResults.Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_ResultForOthersIfLenFunctionIsUsed()
        {
            const string code = @"
Sub DoSomething()
    Dim foo As Variant
    Dim bar As Variant
    bar = Len(foo)
    bar = foo + 5
End Sub
";
            var inspectionResults = InspectionResultsForModules(("TestModule", code, ComponentType.StandardModule), "VBA");
            Assert.AreEqual(1, inspectionResults.Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_ResultForOthersIfLenBFunctionIsUsed()
        {
            const string code = @"
Sub DoSomething()
    Dim foo As Variant
    Dim bar As Variant
    bar = LenB(foo)
    bar = foo + 5
End Sub
";
            var inspectionResults = InspectionResultsForModules(("TestModule", code, ComponentType.StandardModule), "VBA");
            Assert.AreEqual(1, inspectionResults.Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_ResultForUsageInsideArgumentOfLen()
        {
            const string code = @"
Sub DoSomething()
    Dim foo As Variant
    Dim bar As Variant
    bar = Len(foo + 5)
End Sub
";
            var inspectionResults = InspectionResultsForModules(("TestModule", code, ComponentType.StandardModule), "VBA");
            Assert.AreEqual(1, inspectionResults.Count());
        }

        [Test]
        [Category("Inspections")]
        public void UnassignedVariableUsage_ResultForUsageInsideArgumentOfLenB()
        {
            const string code = @"
Sub DoSomething()
    Dim foo As Variant
    Dim bar As Variant
    bar = LenB(foo + 5)
End Sub
";
            var inspectionResults = InspectionResultsForModules(("TestModule", code, ComponentType.StandardModule), "VBA");
            Assert.AreEqual(1, inspectionResults.Count());
        }

        [Test]
        [Category("Inspections")]
        public void InspectionName()
        {
            var inspection = new UnassignedVariableUsageInspection(null);

            Assert.AreEqual(nameof(UnassignedVariableUsageInspection), inspection.Name);
        }

        protected override IInspection InspectionUnderTest(RubberduckParserState state)
        {
            return new UnassignedVariableUsageInspection(state);
        }
    }
}
