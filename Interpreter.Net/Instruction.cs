using System.Collections.Immutable;

namespace Interpreter.Net
{
    [Equals]
    public class Instruction
    {
        public Instruction(
            string name,
            string returnType,
            IImmutableList<Property> properties)
        {
            Name = name;
            ReturnType = returnType;
            Properties = properties;
        }

        public string Name { get; }
        public string ReturnType { get; }
        public IImmutableList<Property> Properties { get; }
    }
}
