namespace Fontisso.NET.Flux;

public record StoreChangedMessage<TState>(TState State);