using Microsoft.CodeAnalysis;

namespace Interpreter.Net
{
    public class InterpreterCode
    {
        public InterpreterCode(string fileName, SyntaxTree syntaxTree)
        {
            FileName = fileName;
            SyntaxTree = syntaxTree;
        }

        public string FileName { get; }
        public SyntaxTree SyntaxTree { get; }
    }
}