using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Interpreter.Net
{
    [Equals]
    public class InterpreterInfo
    {
        public InterpreterInfo(
            IImmutableList<string> usingDirectives,
            string @namespace,
            string baseName,
            IImmutableList<Instruction> instructions)
        {
            UsingDirectives = usingDirectives;
            Namespace = @namespace;
            BaseName = baseName;
            Instructions = instructions;
        }

        [IgnoreDuringEquals]
        public IImmutableList<string> UsingDirectives { get; }
        public string Namespace { get; }
        public string BaseName { get; }
        public IImmutableList<Instruction> Instructions { get; }
    }
}
