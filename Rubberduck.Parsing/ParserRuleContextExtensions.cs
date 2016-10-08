using System;
using Antlr4.Runtime;
using Rubberduck.Parsing.Grammar;
using Rubberduck.Parsing.Symbols;
using Rubberduck.VBEditor;

namespace Rubberduck.Parsing
{
    public static class ParserRuleContextExtensions
    {
        public static Selection GetSelection(this ParserRuleContext context)
        {
            if (context == null)
                return Selection.Home;

            // ANTLR indexes are 0-based, but VBE's are 1-based.
            // 1 is the default value that will select all lines. Replace zeroes with ones.
            // See also: https://msdn.microsoft.com/en-us/library/aa443952(v=vs.60).aspx

            var startLine = context.Start.Line == 0 ? 1 : context.Start.Line;
            var startCol = context.Start.Column + 1;
            var endLine = context.Stop.Line == 0 ? 1 : context.Stop.Line;
            var endCol = context.Stop.Column + context.Stop.Text.Length + 1;

            return new Selection(
                startLine,
                startCol,
                endLine,
                endCol
                );
        }
    }
}
