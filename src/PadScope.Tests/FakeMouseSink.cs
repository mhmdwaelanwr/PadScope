using PadScope.Core.Input;

namespace PadScope.Tests;

internal sealed class FakeMouseSink : IMouseSink
{
    public List<MouseAction> Actions { get; } = new();

    public void Send(MouseAction action)
    {
        Actions.Add(action);
    }
}