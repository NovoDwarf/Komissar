namespace Komissar.Systems;

public sealed class CommandBuffer<TState>
{
    private readonly List<Action<TState>> _commands = [];

    public void Enqueue(Action<TState> command)
    {
        ArgumentNullException.ThrowIfNull(command);
       
        _commands.Add(command);
    }

    public void Apply(TState state)
    {
        foreach (var command in _commands)
        {
            command(state);
        }
    }
}
