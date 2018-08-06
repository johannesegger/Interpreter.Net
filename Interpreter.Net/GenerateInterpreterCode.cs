using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Interpreter.Net
{
    public class GenerateInterpreterCode : Task
    {
        public ITaskItem[] InterpreterInput { get; set; }

        public string OutputPath { get; set; }

        [Output]
        public ITaskItem[] InterpreterOutput { get; set; }

        public override bool Execute()
        {
            InterpreterOutput = InterpreterInput
                .Select(input => input.ItemSpec)
                .Select(File.ReadAllText)
                .Select(content => CSharpSyntaxTree.ParseText(content))
                .Select(ExtractInterpreterInfo)
                .Select(GenerateCode)
                .Select(code => CreateFile(code, OutputPath))
                .ToArray();

            return true;
        }

        public static InterpreterInfo ExtractInterpreterInfo(SyntaxTree tree)
        {
            var root = (CompilationUnitSyntax)tree.GetRoot();
            var namespaceDeclaration = root.Members.Cast<NamespaceDeclarationSyntax>().Single();
            var rootType = namespaceDeclaration.Members.Cast<TypeDeclarationSyntax>().Single();
            var instructions = rootType.Members
                .Cast<MethodDeclarationSyntax>()
                .Select(instructionMethod =>
                {
                    var returnType = ((GenericNameSyntax)instructionMethod.ReturnType).TypeArgumentList.Arguments.Single().ToString();
                    var properties = instructionMethod.ParameterList.Parameters
                        .Select(parameter =>
                        {
                            return new Property(
                                parameter.Type.ToString(),
                                parameter.Identifier.Text
                            );
                        })
                        .ToImmutableList();

                    return new Instruction(
                        instructionMethod.Identifier.Text,
                        returnType,
                        properties
                    );
                })
                .ToImmutableList();

            return new InterpreterInfo(
                root.Usings.Select(p => p.ToString()).ToImmutableList(),
                namespaceDeclaration.Name.ToString(),
                rootType.Identifier.Text,
                instructions
            );
        }

        public static InterpreterCode GenerateCode(InterpreterInfo info)
        {
            string GenerateInstructionType(Instruction instruction)
            {
                var constructorParameters = instruction.Properties
                    .Select(property => $"{property.Type} {property.Name.ToCamelCase()}")
                    .JoinString(", ");
                
                var propertyAssignment = instruction.Properties
                    .Select(property => $"            {property.Name} = {property.Name.ToCamelCase()};")
                    .JoinString(Environment.NewLine);

                var propertyDeclaration = instruction.Properties
                    .Select(property => $"        public {property.Type} {property.Name} {{ get; }}")
                    .JoinString(Environment.NewLine);

                return $@"    public class {instruction.Name} : {info.BaseName}<{instruction.ReturnType}>
    {{
        public {instruction.Name}({constructorParameters})
        {{
{propertyAssignment}
        }}

{propertyDeclaration}

        public override System.Threading.Tasks.Task Accept(I{info.BaseName}Handler handler) => handler.Handle(this);
    }}";
            }

            string GenerateInstructionHandlerMethod(Instruction instruction)
            {
                return $"        System.Threading.Tasks.Task Handle({instruction.Name} instruction);";
            }

            var code = $@"{info.UsingDirectives.JoinString(Environment.NewLine)}

namespace {info.Namespace}
{{
    public interface I{info.BaseName} : System.Runtime.CompilerServices.ICriticalNotifyCompletion
    {{
        System.Threading.Tasks.Task Accept(I{info.BaseName}Handler handler);
    }}

    public abstract class {info.BaseName}<TResult> : I{info.BaseName}
    {{
        public void OnCompleted(Action continuation) => throw new NotImplementedException();
        public void UnsafeOnCompleted(Action continuation) => throw new NotImplementedException();

        private bool hasResult;
        public bool IsCompleted => hasResult;

        private TResult result;
        public TResult GetResult() => result;

        public {info.BaseName}<TResult> GetAwaiter() => this;

        public void SetResult(TResult result)
        {{
            hasResult = true;
            this.result = result;
        }}

        public abstract System.Threading.Tasks.Task Accept(I{info.BaseName}Handler handler);
    }}

{info.Instructions.Select(GenerateInstructionType).JoinString(Environment.NewLine)}

    public interface I{info.BaseName}Handler
    {{
{info.Instructions.Select(GenerateInstructionHandlerMethod).JoinString(Environment.NewLine)}
    }}

    public class {info.BaseName}ProgramBuilder<TResult>
    {{
        private {info.BaseName}Program<TResult> prg;
        
        private Func<{info.BaseName}Program<TResult>> cont;

        public static {info.BaseName}ProgramBuilder<TResult> Create() => new {info.BaseName}ProgramBuilder<TResult>();

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine
        {{
            var state = stateMachine;
            this.cont = () =>
            {{
                state.MoveNext();
                return this.prg;
            }};
            this.prg = new Delay<TResult>(cont);
        }}

        public void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine)
        {{
        }}

        public void SetResult(TResult result)
        {{
            this.prg = new Return<TResult>(result);
        }}

        public void SetException(Exception exception)
        {{
            throw exception;
        }}

        public {info.BaseName}Program<TResult> Task => this.prg;

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : I{info.BaseName}
            where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine
        {{
            this.prg = new Await<TResult>(awaiter, this.cont);
        }}

        [System.Security.SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : I{info.BaseName}
            where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine
        {{
            this.prg = new Await<TResult>(awaiter, this.cont);
        }}

    }}

    [System.Runtime.CompilerServices.AsyncMethodBuilder(typeof({info.BaseName}ProgramBuilder<>))]
    public abstract class {info.BaseName}Program<TResult>
    {{
    }}

    public class Await<TResult> : {info.BaseName}Program<TResult>
    {{
        public I{info.BaseName} Instruction {{ get; }}
        public Func<{info.BaseName}Program<TResult>> Cont {{ get; }}
        
        public Await(I{info.BaseName} instruction, Func<{info.BaseName}Program<TResult>> cont)
        {{
            Instruction = instruction;
            Cont = cont;
        }}
    }}

    public class Return<TResult> : {info.BaseName}Program<TResult>
    {{
        public TResult Result {{ get; }}

        public Return(TResult result)
        {{
            Result = result;
        }}
    }}

    public class Delay<TResult> : {info.BaseName}Program<TResult>
    {{
        public Func<{info.BaseName}Program<TResult>> Func {{ get; }}

        public Delay(Func<{info.BaseName}Program<TResult>> func)
        {{
            Func = func;
        }}
    }}

    public static class {info.BaseName}ProgramExecutor
    {{
        public static async System.Threading.Tasks.Task<TResult> Run<TResult>(
            this {info.BaseName}Program<TResult> prg,
            I{info.BaseName}Handler handler)
        {{
            var result = default(TResult);
            var done = false;
            while (!done)
            {{
                switch (prg)
                {{
                    case Return<TResult> ret:
                        result = ret.Result;
                        done = true;
                        break;
                    case Delay<TResult> delay:
                        prg = delay.Func();
                        break;
                    case Await<TResult> _await:
                        await _await.Instruction.Accept(handler);
                        prg = _await.Cont();
                        break;
                    default:
                        throw new NotSupportedException($""Can't handle instruction type `{{prg.GetType().Name}}`"");
                }}
            }}

            return result;
        }}
    }}
}}
            ";
            return new InterpreterCode($"{info.BaseName}.generated.cs", CSharpSyntaxTree.ParseText(code));
        }

        public static ITaskItem CreateFile(InterpreterCode code, string baseDir)
        {
            var path = Path.Combine(baseDir, code.FileName);
            var dir = Path.GetDirectoryName(path);
            Directory.CreateDirectory(dir);
            File.WriteAllText(path, code.SyntaxTree.ToString());
            return new TaskItem(path);
        }
    }
}
