namespace Printify.TestServices.Printing;

public interface ITestPortRegistry
{
    void ClaimPort(int port);
    void ReleasePort(int port);
}
