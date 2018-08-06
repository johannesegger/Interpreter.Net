namespace Interpreter.Net
{
    [Equals]
    public class Property
    {
        public Property(string type, string name)
        {
            Type = type;
            Name = name.ToPascalCase();
        }

        public string Type { get; }
        public string Name { get; }
    }
}
