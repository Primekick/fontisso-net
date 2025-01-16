namespace Fontisso.NET.Data;

public record StoreChangedMessage<TState>(TState State);