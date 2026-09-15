namespace Komissar.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class RunsAfterAttribute(Type systemType) : Attribute
{
	public Type SystemType { get; } = systemType;
}