using CommunityToolkit.Mvvm.Messaging;

namespace Fontisso.NET.Data;

public abstract class Store<TState> where TState : class 
{
    private TState _state;
    protected TState State => _state;

    protected Store(TState initialState)
    {
        _state = initialState;
    }

    protected void SetState(Func<TState, TState> updateFunc)
    {
        _state = updateFunc(_state);
        WeakReferenceMessenger.Default.Send(new StoreChangedMessage<TState>(_state));
    }
    
    public abstract Task Dispatch(IAction action);
}