namespace Blazor.Redux.Interfaces;

public interface IDispatcher
{
    void Dispatch<TSlice, TAction>(TAction action)
        where TSlice : class, ISlice
        where TAction : class, IAction;
}