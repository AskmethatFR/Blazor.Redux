namespace Blazor.Redux.Interfaces;

public interface IReducer<TS, TA>
    where TS : class, ISlice
    where TA : class, IAction
{
    public TS Reduce(TS slice, TA action);
}