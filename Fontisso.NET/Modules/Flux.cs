using System;
using CommunityToolkit.Mvvm.Messaging;

namespace Fontisso.NET.Modules;

public static class Flux
{
    public interface IAction { }

    public record StoreChangedMessage<TState>(TState State);

    public abstract class Store<TState> where TState : struct
    {
        private TState _state;
        protected TState State => _state;

        protected Store(TState initialState)
        {
            _state = initialState;
            WeakReferenceMessenger.Default.Send(new StoreChangedMessage<TState>(_state));
        }

        protected void SetState(Func<TState, TState> updateFunc)
        {
            _state = updateFunc(_state);
            WeakReferenceMessenger.Default.Send(new StoreChangedMessage<TState>(_state));
        }

        public abstract void Dispatch(IAction action);
    }
}