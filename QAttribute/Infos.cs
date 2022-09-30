using Microsoft.CodeAnalysis;

namespace QAttribute
{
    public class FieldInfo
    {
        public TypeKind? typeKind;
        public string? typeName;
        public string? identifier;
        public IEnumerable<AttributeInfo>? attributes;
    }

    public class AttributeInfo
    {
        public string? name;
        public IEnumerable<CommandInfo>? commands;
    }

    public class CommandInfo
    {
        public string? name;
        public string? command;
    }
}
