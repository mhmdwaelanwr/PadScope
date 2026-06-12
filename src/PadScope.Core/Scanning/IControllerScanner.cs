using PadScope.Core.Models;

namespace PadScope.Core.Scanning;

public interface IControllerScanner
{
    IReadOnlyList<ControllerDevice> Scan();
}
