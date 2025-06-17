using Blazor.Redux.Core.Events;
using Blazor.Redux.DevTools;
using Blazor.Redux.DevTools.Interfaces;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Moq;

namespace Blazor.Redux.DevToolsTests;

[TestSubject(typeof(ReduxDevToolsSubscriber))]
public class ReduxDevToolsSubscriberTests
{
    private readonly StoreEventPublisher _eventPublisher;
    private readonly Mock<IReduxDevTools> _mockDevTools;
    private readonly Mock<ILogger<ReduxDevToolsSubscriber>> _mockLogger;
    private ReduxDevToolsSubscriber _subscriber;

    public ReduxDevToolsSubscriberTests()
    {
        _eventPublisher = new StoreEventPublisher();
        _mockDevTools = new Mock<IReduxDevTools>();
        _mockLogger = new Mock<ILogger<ReduxDevToolsSubscriber>>();

        // Setup commun pour les mocks - toujours fait
        _mockDevTools.Setup(dt => dt.InitAsync()).Returns(Task.CompletedTask);
        _mockDevTools.Setup(dt => dt.SendAsync(It.IsAny<object>(), It.IsAny<object>())).Returns(Task.CompletedTask);
        _mockDevTools.Setup(dt => dt.DisconnectAsync()).Returns(Task.CompletedTask);
    }

    #region Constructor Tests

    [Fact]
    public void ReduxDevToolsSubscriberShouldImplementIAsyncDisposable()
    {
        CreateSubscriber();
        Assert.IsAssignableFrom<IAsyncDisposable>(_subscriber);
    }

    [Fact]
    public void ReduxDevToolsSubscriberConstructorShouldAcceptValidParameters()
    {
        var exception = Record.Exception(() => CreateSubscriber());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(true, false, false)] // eventPublisher null
    [InlineData(false, true, false)] // devTools null
    [InlineData(false, false, true)] // logger null
    public void ReduxDevToolsSubscriberConstructorShouldThrowWhenParameterIsNull(
        bool nullEventPublisher, bool nullDevTools, bool nullLogger)
    {
        var eventPublisher = nullEventPublisher ? null : _eventPublisher;
        var devTools = nullDevTools ? null : _mockDevTools.Object;
        var logger = nullLogger ? null : _mockLogger.Object;

        Assert.Throws<ArgumentNullException>(() =>
            new ReduxDevToolsSubscriber(eventPublisher!, devTools!, logger!));
    }

    #endregion

    #region InitializeAsync Tests

    [Fact]
    public async Task InitializeAsyncShouldCallDevToolsInitAsync()
    {
        _mockDevTools.Setup(dt => dt.IsEnabled).Returns(true);
        CreateSubscriber();

        await _subscriber.InitializeAsync();

        _mockDevTools.Verify(dt => dt.InitAsync(), Times.Once);
    }

    [Fact]
    public async Task InitializeAsyncShouldSubscribeToEventPublisherWhenDevToolsEnabled()
    {
        _mockDevTools.Setup(dt => dt.IsEnabled).Returns(true);
        CreateSubscriber();
        var initialSubscribers = GetEventSubscriberCount();

        await _subscriber.InitializeAsync();

        Assert.Equal(initialSubscribers + 1, GetEventSubscriberCount());
        VerifyInfoLogCalled("Redux DevTools subscriber initialized");
    }

    [Fact]
    public async Task InitializeAsyncShouldNotSubscribeToEventPublisherWhenDevToolsDisabled()
    {
        _mockDevTools.Setup(dt => dt.IsEnabled).Returns(false);
        CreateSubscriber();
        var initialSubscribers = GetEventSubscriberCount();

        await _subscriber.InitializeAsync();

        Assert.Equal(initialSubscribers, GetEventSubscriberCount());
        VerifyInfoLogNeverCalled();
    }

    #endregion

    #region OnStoreEvent Tests

    [Fact]
    public async Task OnStoreEventShouldCallDevToolsSendAsyncWithCorrectData()
    {
        await CreateInitializedSubscriber(enabled: true);
        var storeEvent = CreateTestStoreEvent();

        _eventPublisher.PublishEvent(storeEvent);
        await Task.Delay(50); // Allow async void to complete

        VerifyDevToolsSendCalled(storeEvent);
    }

    [Fact]
    public async Task OnStoreEventShouldFormatActionDataCorrectly()
    {
        await CreateInitializedSubscriber(enabled: true);
        var storeEvent = CreateTestStoreEvent("TestAction", "TestSlice");

        _eventPublisher.PublishEvent(storeEvent);
        await Task.Delay(50);

        VerifyDevToolsSendCalled(storeEvent);
    }

    [Fact]
    public async Task OnStoreEventShouldHandleDevToolsExceptionGracefully()
    {
        await CreateInitializedSubscriber(enabled: true);
        var storeEvent = CreateTestStoreEvent();
        _mockDevTools.Setup(dt => dt.SendAsync(It.IsAny<object>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("DevTools error"));

        _eventPublisher.PublishEvent(storeEvent);
        await Task.Delay(50);

        VerifyErrorLogCalled("Error sending event to Redux DevTools");
    }

    [Fact]
    public async Task OnStoreEventShouldNotCallDevToolsSendAsyncWhenDisabled()
    {
        await CreateInitializedSubscriber(enabled: false);
        var storeEvent = CreateTestStoreEvent();

        _eventPublisher.PublishEvent(storeEvent);
        await Task.Delay(50);

        _mockDevTools.Verify(dt => dt.SendAsync(It.IsAny<object>(), It.IsAny<object>()), Times.Never);
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsyncShouldUnsubscribeFromEventPublisherWhenEnabled()
    {
        await CreateInitializedSubscriber(enabled: true);
        var subscribersBeforeDispose = GetEventSubscriberCount();

        await _subscriber.DisposeAsync();

        Assert.Equal(subscribersBeforeDispose - 1, GetEventSubscriberCount());
        _mockDevTools.Verify(dt => dt.DisconnectAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsyncShouldNotUnsubscribeFromEventPublisherWhenDisabled()
    {
        await CreateInitializedSubscriber(enabled: false);
        var subscribersBeforeDispose = GetEventSubscriberCount();

        await _subscriber.DisposeAsync();

        Assert.Equal(subscribersBeforeDispose, GetEventSubscriberCount());
        _mockDevTools.Verify(dt => dt.DisconnectAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsyncShouldCallDevToolsDisconnectAsync()
    {
        await CreateInitializedSubscriber(enabled: true);

        await _subscriber.DisposeAsync();

        _mockDevTools.Verify(dt => dt.DisconnectAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsyncShouldBeCallableMultipleTimes()
    {
        await CreateInitializedSubscriber(enabled: true);

        await _subscriber.DisposeAsync();
        var exception = await Record.ExceptionAsync(async () => await _subscriber.DisposeAsync());

        Assert.Null(exception);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task FullLifecycleShouldWorkWithEnabledDevTools()
    {
        _mockDevTools.Setup(dt => dt.IsEnabled).Returns(true);
        CreateSubscriber();
        var storeEvent = CreateTestStoreEvent();
        var initialSubscribers = GetEventSubscriberCount();

        // Initialize
        await _subscriber.InitializeAsync();
        Assert.Equal(initialSubscribers + 1, GetEventSubscriberCount());

        // Handle event
        _eventPublisher.PublishEvent(storeEvent);
        await Task.Delay(50);
        VerifyDevToolsSendCalled(storeEvent);


        // Dispose
        await _subscriber.DisposeAsync();
        Assert.Equal(initialSubscribers, GetEventSubscriberCount());
        _mockDevTools.Verify(dt => dt.DisconnectAsync(), Times.Once);
    }

    [Fact]
    public async Task FullLifecycleShouldWorkWithDisabledDevTools()
    {
        _mockDevTools.Setup(dt => dt.IsEnabled).Returns(false);
        CreateSubscriber();
        var storeEvent = CreateTestStoreEvent();
        var initialSubscribers = GetEventSubscriberCount();

        // Initialize
        await _subscriber.InitializeAsync();
        Assert.Equal(initialSubscribers, GetEventSubscriberCount());

        // Handle event (should do nothing)
        _eventPublisher.PublishEvent(storeEvent);
        await Task.Delay(50);
        _mockDevTools.Verify(dt => dt.SendAsync(It.IsAny<object>(), It.IsAny<object>()), Times.Never);

        // Dispose
        await _subscriber.DisposeAsync();
        Assert.Equal(initialSubscribers, GetEventSubscriberCount());
        _mockDevTools.Verify(dt => dt.DisconnectAsync(), Times.Once);
    }

    [Fact]
    public async Task MultipleEventsShouldBeHandledCorrectly()
    {
        await CreateInitializedSubscriber(enabled: true);
        var events = new[]
        {
            CreateTestStoreEvent("Action1", "Slice1"),
            CreateTestStoreEvent("Action2", "Slice2"),
            CreateTestStoreEvent("Action3", "Slice1")
        };

        foreach (var storeEvent in events)
        {
            _eventPublisher.PublishEvent(storeEvent);
        }

        await Task.Delay(100);

        _mockDevTools.Verify(dt => dt.SendAsync(It.IsAny<object>(), It.IsAny<object>()), Times.Exactly(events.Length));
    }

    #endregion

    #region Helper Methods

    private void CreateSubscriber() =>
        _subscriber = new(_eventPublisher, _mockDevTools.Object, _mockLogger.Object);

    private async Task CreateInitializedSubscriber(bool enabled)
    {
        _mockDevTools.Setup(dt => dt.IsEnabled).Returns(enabled);
        CreateSubscriber();
        await _subscriber.InitializeAsync();
    }

    private static StoreEvent CreateTestStoreEvent(string eventType = "TestAction", string sliceType = "TestSlice") =>
        new(
            eventType,
            sliceType,
            new { data = "test" },
            new { value = 42 },
            new { value = 0 },
            DateTime.UtcNow
        );

    private int GetEventSubscriberCount()
    {
        var eventField = typeof(StoreEventPublisher).GetField("EventOccurred",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var eventDelegate = (MulticastDelegate?)eventField?.GetValue(_eventPublisher);
        return eventDelegate?.GetInvocationList().Length ?? 0;
    }

    #endregion

    #region Verification Methods

    private void VerifyDevToolsSendCalled(StoreEvent storeEvent) =>
        _mockDevTools.Verify(dt => dt.SendAsync(
            It.IsAny<object>(),
            storeEvent.NewState), Times.Once);
    private void VerifyInfoLogCalled(string message) =>
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

    private void VerifyInfoLogNeverCalled() =>
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);

    private void VerifyErrorLogCalled(string message) =>
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);


    #endregion
}