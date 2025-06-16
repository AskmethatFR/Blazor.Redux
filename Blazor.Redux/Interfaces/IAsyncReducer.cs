namespace Blazor.Redux.Interfaces;

public interface IAsyncReducer<TS, TA>
    where TS : class, ISlice
    where TA : class, IAction
{
    public Task<TS> ReduceAsync(TS slice, TA action);
}