using Blazor.Redux.Core;
using Blazor.Redux.Interfaces;

namespace Blazor.ReduxTests.Core;

public class StoreTests
{
    private Store _sut;

    [Fact]
    private void ReturnANewInstanceOfModifiedSlice()
    {
        var expected = new StoreSlice()
        {
            Name = "titi"
        };
        var result = Act(new StoreSlice()
        {
            Name = "toto"
        }, expected);

        Assert.Equivalent(expected, _sut.GetSlice<StoreSlice>());
        Assert.Equivalent(expected, result);
        Assert.NotSame(expected, result);
    }

    [Fact]
    private void ExceptionIfSliceIsNotFound()
    {
        try
        {
            Act(new StoreSlice()
            {
                Name = "toto"
            }, new StoreSlice2()
            {
                Value = "titi"
            });
            Assert.Fail("Adding unknown slice is prohibited");
        }
        catch (InvalidOperationException)
        {
            Assert.True(true);
        }
    }

    private void InitStore(ISlice slice)
    {
        _sut = Store.Init(slice);
    }

    private TE Act<TI, TE>(TI init, TE update)
        where TI : class, ISlice
        where TE : class, ISlice
    {
        InitStore(init);
        return _sut.UpdateSlice(update);
    }
}

public record StoreSlice : ISlice
{
    public required string Name { get; init; }
}

public record StoreSlice2 : ISlice
{
    public required string Value { get; init; }
}