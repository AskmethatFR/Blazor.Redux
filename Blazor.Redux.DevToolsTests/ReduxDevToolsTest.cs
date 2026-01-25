
using Blazor.Redux.Core;
using Blazor.Redux.DevTools;
using Blazor.Redux.DevTools.Interfaces;
using Blazor.Redux.Interfaces;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;
using System.Text.Json;

namespace Blazor.Redux.DevToolsTests;

[TestSubject(typeof(ReduxDevTools))]
public class ReduxDevToolsTests
{
    private readonly Mock<IJSRuntime> _mockJsRuntime;
    private readonly Mock<ILogger<ReduxDevTools>> _mockLogger;
    private readonly Mock<IRootStateStore> _mockRootStateStore;
    private readonly Mock<IStateSnapshotApplier> _mockSnapshotApplier;
    private readonly Mock<IReduxSerializer> _mockSerializer;

    public ReduxDevToolsTests()
    {
        _mockJsRuntime = new Mock<IJSRuntime>();
        _mockLogger = new Mock<ILogger<ReduxDevTools>>();
        _mockRootStateStore = new Mock<IRootStateStore>();
        _mockSnapshotApplier = new Mock<IStateSnapshotApplier>();
        _mockSerializer = new Mock<IReduxSerializer>();
        _mockRootStateStore.Setup(store => store.Current)
            .Returns(new RootStateSnapshot(new Dictionary<Type, ISlice>()));
    }

    #region Constructor Tests

    [Fact]
    public void ReduxDevToolsShouldImplementIReduxDevTools()
    {
        var devTools = CreateReduxDevTools();
        Assert.IsAssignableFrom<IReduxDevTools>(devTools);
    }

    [Fact]
    public void ReduxDevToolsShouldImplementIAsyncDisposable()
    {
        var devTools = CreateReduxDevTools();
        Assert.IsAssignableFrom<IAsyncDisposable>(devTools);
    }

    [Fact]
    public void ReduxDevToolsConstructorShouldAcceptValidParameters()
    {
        var exception = Record.Exception(() => CreateReduxDevTools());
        Assert.Null(exception);
    }

    [Fact]
    public void ReduxDevToolsConstructorShouldThrowWhenJsRuntimeIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ReduxDevTools(null!, _mockLogger.Object, _mockRootStateStore.Object, _mockSnapshotApplier.Object,
                _mockSerializer.Object));
    }

    [Fact]
    public void ReduxDevToolsConstructorShouldThrowWhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ReduxDevTools(_mockJsRuntime.Object, null!, _mockRootStateStore.Object, _mockSnapshotApplier.Object,
                _mockSerializer.Object));
    }

    [Fact]
    public void ReduxDevToolsConstructorShouldThrowWhenRootStateStoreIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ReduxDevTools(_mockJsRuntime.Object, _mockLogger.Object, null!, _mockSnapshotApplier.Object,
                _mockSerializer.Object));
    }

    [Fact]
    public void ReduxDevToolsConstructorShouldThrowWhenSnapshotApplierIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ReduxDevTools(_mockJsRuntime.Object, _mockLogger.Object, _mockRootStateStore.Object, null!,
                _mockSerializer.Object));
    }

    [Fact]
    public void ReduxDevToolsConstructorShouldThrowWhenSerializerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ReduxDevTools(_mockJsRuntime.Object, _mockLogger.Object, _mockRootStateStore.Object,
                _mockSnapshotApplier.Object, null!));
    }

    #endregion

    #region InitAsync Tests

    [Fact]
    public async Task InitAsyncShouldCallJsRuntimeInvokeAsync()
    {
        SetupJsInit(true);
        var devTools = CreateReduxDevTools();

        await devTools.InitAsync();

        VerifyJsInitCalled();
    }

    [Fact]
    public async Task InitAsyncShouldSetIsEnabledWhenJsReturnsTrue()
    {
        SetupJsInit(true);
        var devTools = CreateReduxDevTools();

        await devTools.InitAsync();

        Assert.True(devTools.IsEnabled);
    }

    [Fact]
    public async Task InitAsyncShouldSetIsEnabledFalseWhenJsReturnsFalse()
    {
        SetupJsInit(false);
        var devTools = CreateReduxDevTools();

        await devTools.InitAsync();

        Assert.False(devTools.IsEnabled);
    }

    [Fact]
    public async Task InitAsyncShouldHandleJsExceptionGracefully()
    {
        SetupJsInitException();
        var devTools = CreateReduxDevTools();

        var exception = await Record.ExceptionAsync(async () => await devTools.InitAsync());

        Assert.Null(exception);
        Assert.False(devTools.IsEnabled);
    }

    #endregion

    #region SendAsync Tests

    [Fact]
    public async Task SendAsyncShouldCallJsRuntimeInvokeAsyncWhenEnabled()
    {
        var devTools = await CreateEnabledDevTools();
        SetupJsSend();
        var (actionData, state) = CreateTestData();

        await devTools.SendAsync(actionData, state);

        VerifyJsSendCalled(actionData, state);
    }

    [Fact]
    public async Task SendAsyncShouldNotCallJsRuntimeWhenDisabled()
    {
        var devTools = await CreateDisabledDevTools();
        var (actionData, state) = CreateTestData();

        await devTools.SendAsync(actionData, state);

        VerifyJsSendNeverCalled();
    }

    [Fact]
    public async Task SendAsyncShouldHandleJsExceptionGracefully()
    {
        var devTools = await CreateEnabledDevTools();
        SetupJsSendException();
        var (actionData, state) = CreateTestData();

        var exception = await Record.ExceptionAsync(async () =>
            await devTools.SendAsync(actionData, state));

        Assert.Null(exception);
    }

    #endregion

    #region DisconnectAsync Tests

    [Fact]
    public async Task DisconnectAsyncShouldCallJsRuntimeInvokeAsyncWhenEnabled()
    {
        var devTools = await CreateEnabledDevTools();
        SetupJsDisconnect();

        await devTools.DisconnectAsync();

        VerifyJsDisconnectCalled();
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsyncShouldCallDisconnectAsync()
    {
        var devTools = await CreateEnabledDevTools();
        SetupJsDisconnect();

        await devTools.DisposeAsync();

        VerifyJsDisconnectCalled();
    }

    [Fact]
    public async Task DisposeAsyncShouldBeCallableMultipleTimes()
    {
        var devTools = await CreateEnabledDevTools();
        SetupJsDisconnect();

        await devTools.DisposeAsync();
        var exception = await Record.ExceptionAsync(async () =>
            await devTools.DisposeAsync());

        Assert.Null(exception);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task MultipleOperationsShouldWorkInSequence()
    {
        SetupAllJsOperations();
        var devTools = CreateReduxDevTools();
        var (actionData1, state1) = CreateTestData("ACTION1", 1);
        var (actionData2, state2) = CreateTestData("ACTION2", 2);

        await devTools.InitAsync();
        Assert.True(devTools.IsEnabled);

        await devTools.SendAsync(actionData1, state1);
        await devTools.SendAsync(actionData2, state2);

        await devTools.DisconnectAsync();
        await devTools.DisposeAsync();

        VerifyFullSequence();
    }

    [Fact]
    public async Task OnDevToolsMessageShouldApplySnapshotOnJumpToState()
    {
        var snapshot = new RootStateSnapshot(new Dictionary<Type, ISlice>());
        _mockSerializer
            .Setup(serializer => serializer.DeserializeState(It.IsAny<string>()))
            .Returns(snapshot);

        var devTools = await CreateEnabledDevTools();
        var message = JsonDocument.Parse(
            "{\"type\":\"DISPATCH\",\"payload\":{\"type\":\"JUMP_TO_STATE\"},\"state\":\"{}\"}").RootElement;

        await devTools.OnDevToolsMessage(message);

        _mockSnapshotApplier.Verify(applier =>
            applier.ApplySnapshot(snapshot, true), Times.Once);
    }

    #endregion

    #region Helper Methods

    private ReduxDevTools CreateReduxDevTools() =>
        new(_mockJsRuntime.Object, _mockLogger.Object, _mockRootStateStore.Object, _mockSnapshotApplier.Object,
            _mockSerializer.Object);

    private async Task<ReduxDevTools> CreateEnabledDevTools()
    {
        SetupJsInit(true);
        _mockRootStateStore.Setup(store => store.Current)
            .Returns(new RootStateSnapshot(new Dictionary<Type, ISlice>()));
        var devTools = CreateReduxDevTools();
        await devTools.InitAsync();
        return devTools;
    }

    private async Task<ReduxDevTools> CreateDisabledDevTools()
    {
        SetupJsInit(false);
        _mockRootStateStore.Setup(store => store.Current)
            .Returns(new RootStateSnapshot(new Dictionary<Type, ISlice>()));
        var devTools = CreateReduxDevTools();
        await devTools.InitAsync();
        return devTools;
    }

    private static (object actionData, object state) CreateTestData(string actionType = "TESTACTION", int value = 1) =>
        (new { type = actionType, payload = "test" }, new { count = value });

    #endregion

    #region JS Setup Methods

    private void SetupJsInit(bool returnValue) =>
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<bool>("ReduxDevTools.init", It.IsAny<object[]>()))
            .ReturnsAsync(returnValue);

    private void SetupJsInitException() =>
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<bool>("ReduxDevTools.init", It.IsAny<object[]>()))
            .ThrowsAsync(new JSException("DevTools not available"));

    private void SetupJsSend() =>
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<IJSVoidResult>("ReduxDevTools.send", It.IsAny<object[]>()))
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

    private void SetupJsSendException() =>
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<IJSVoidResult>("ReduxDevTools.send", It.IsAny<object[]>()))
            .ThrowsAsync(new JSException("Send failed"));

    private void SetupJsDisconnect() =>
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<IJSVoidResult>("ReduxDevTools.disconnect", It.IsAny<object[]>()))
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

    private void SetupAllJsOperations()
    {
        SetupJsInit(true);
        SetupJsSend();
        SetupJsDisconnect();
    }

    #endregion

    #region Verification Methods

    private void VerifyJsInitCalled() =>
        _mockJsRuntime.Verify(
            js => js.InvokeAsync<bool>("ReduxDevTools.init", It.IsAny<object[]>()),
            Times.Once);

    private void VerifyJsSendCalled(object actionData, object state) =>
        _mockJsRuntime.Verify(
            js => js.InvokeAsync<IJSVoidResult>("ReduxDevTools.send",
                It.Is<object[]>(args => args.Length == 2 &&
                                        args[0].Equals(actionData) &&
                                        args[1].Equals(state))),
            Times.Once);

    private void VerifyJsSendNeverCalled() =>
        _mockJsRuntime.Verify(
            js => js.InvokeAsync<IJSVoidResult>("ReduxDevTools.send", It.IsAny<object[]>()),
            Times.Never);

    private void VerifyJsDisconnectCalled() =>
        _mockJsRuntime.Verify(
            js => js.InvokeAsync<IJSVoidResult>("ReduxDevTools.disconnect", It.IsAny<object[]>()),
            Times.Once);

    private void VerifyJsDisconnectNeverCalled() =>
        _mockJsRuntime.Verify(
            js => js.InvokeAsync<IJSVoidResult>("ReduxDevTools.disconnect", It.IsAny<object[]>()),
            Times.Never);

    private void VerifyFullSequence()
    {
        VerifyJsInitCalled();
        _mockJsRuntime.Verify(
            js => js.InvokeAsync<IJSVoidResult>("ReduxDevTools.send", It.IsAny<object[]>()),
            Times.Exactly(2));
        _mockJsRuntime.Verify(
            js => js.InvokeAsync<IJSVoidResult>("ReduxDevTools.disconnect", It.IsAny<object[]>()),
            Times.Exactly(1)); // DisconnectAsync + DisposeAsync
    }

    #endregion
}
