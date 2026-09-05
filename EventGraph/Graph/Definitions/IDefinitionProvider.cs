namespace EventGraph
{
    /// <summary>
    /// Provides immutable definition metadata independently from runtime values.
    /// </summary>
    public interface IDefinitionProvider<out TDefinition>
    {
        TDefinition Definition { get; }
    }
}