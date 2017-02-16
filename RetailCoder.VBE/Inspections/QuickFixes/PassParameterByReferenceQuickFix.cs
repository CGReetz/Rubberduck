using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Rubberduck.Inspections.Abstract;
using Rubberduck.Inspections.Resources;
using Rubberduck.Parsing.Grammar;
using Rubberduck.Parsing.Symbols;
using Rubberduck.VBEditor;
using static Rubberduck.Parsing.Grammar.VBAParser;

namespace Rubberduck.Inspections.QuickFixes
{
    /// <summary>
    /// Encapsulates a code inspection quickfix that changes a ByVal parameter into an explicit ByRef parameter.
    /// </summary>
    public class PassParameterByReferenceQuickFix : QuickFixBase
    {
        private Declaration _target;

        public PassParameterByReferenceQuickFix(Declaration target, QualifiedSelection selection)
            : base(target.Context, selection, InspectionsUI.PassParameterByReferenceQuickFix)
        {
            _target = target;
        }

        public override void Fix()
        {
            var argCtxt = GetArgContextForIdentifier(Context, _target.IdentifierName);

            var terminalNodeImpl = GetByValNodeForArgCtx(argCtxt);

            var replacementLine = GenerateByRefReplacementLine(terminalNodeImpl);

            ReplaceModuleLine(terminalNodeImpl.Symbol.Line, replacementLine);

        }
        private ArgContext GetArgContextForIdentifier(ParserRuleContext context, string identifier)
        {
            var procStmtCtx = (ParserRuleContext)context.Parent.Parent;
            var procStmtCtxChildren = procStmtCtx.children;
            for (int idx = 0; idx < procStmtCtxChildren.Count; idx++)
            {
                if (procStmtCtxChildren[idx] is SubstmtContext)
                {
                    var procStmtCtxChild = (SubstmtContext)procStmtCtxChildren[idx];
                    var arg = procStmtCtxChild.children;
                    for (int idx2 = 0; idx2 < arg.Count; idx2++)
                    {
                        if (arg[idx2] is ArgContext)
                        {
                            var name = GetIdentifierNameFor((ArgContext)arg[idx2]);
                            if (name.Equals(identifier))
                            {
                                return (ArgContext)arg[idx2];
                            }
                        }
                    }
                }
            }
            return null;
        }
        private string GetIdentifierNameFor(ArgContext argCtxt)
        {
            var argCtxtChild = argCtxt.children;
            var idRef = GetUnRestrictedIdentifierCtx(argCtxt);
            return idRef.GetText();
        }
        private UnrestrictedIdentifierContext GetUnRestrictedIdentifierCtx(ArgContext argCtxt)
        {
            var argCtxtChild = argCtxt.children;
            for (int idx = 0; idx < argCtxtChild.Count; idx++)
            {
                if (argCtxtChild[idx] is UnrestrictedIdentifierContext)
                {
                    return (UnrestrictedIdentifierContext)argCtxtChild[idx];
                }
            }
            return null;
        }
        private TerminalNodeImpl GetByValNodeForArgCtx(ArgContext argCtxt)
        {
            var argCtxtChild = argCtxt.children;
            for (int idx = 0; idx < argCtxtChild.Count; idx++)
            {
                if (argCtxtChild[idx] is TerminalNodeImpl)
                {
                    var candidate = (TerminalNodeImpl)argCtxtChild[idx];
                    if (candidate.Symbol.Text.Equals(Tokens.ByVal))
                    {
                        return candidate;
                    }
                }
            }
            return null;
        }
        private string GenerateByRefReplacementLine(TerminalNodeImpl terminalNodeImpl)
        {
            var module = Selection.QualifiedName.Component.CodeModule;
            var byValTokenLine = module.GetLines(terminalNodeImpl.Symbol.Line, 1);

            return ReplaceAtIndex(byValTokenLine, Tokens.ByVal, Tokens.ByRef, terminalNodeImpl.Symbol.Column);
        }
        private string ReplaceAtIndex(string input, string toReplace, string replacement, int index)
        {
            int stopIndex = index + toReplace.Length;
            var prefix = input.Substring(0, index);
            var suffix = input.Substring(stopIndex + 1);
            var target = input.Substring(index, stopIndex - index + 1);
            return prefix + target.Replace(toReplace, replacement) + suffix;
        }
        private void ReplaceModuleLine(int lineNumber, string replacementLine)
        {
            var module = Selection.QualifiedName.Component.CodeModule;
            module.DeleteLines(lineNumber);
            module.InsertLines(lineNumber, replacementLine);
        }
    }
}