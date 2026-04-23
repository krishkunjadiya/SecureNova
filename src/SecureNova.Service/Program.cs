using System.ServiceProcess;

namespace SecureNova.Service;

internal static class Program
{
    private static void Main()
    {
        ServiceBase.Run(new SecureNovaService());
    }
}
