namespace Blazor.Redux.Interfaces;

public interface IEffectAction
{
    void Dispatch(IDispatcher dispatcher);
}
