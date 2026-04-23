using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace SecureNova.Service
{
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller _processInstaller;
        private ServiceInstaller _serviceInstaller;

        public ProjectInstaller()
        {
            _processInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalSystem
            };

            _serviceInstaller = new ServiceInstaller
            {
                ServiceName = "SecureNovaService",
                DisplayName = "SecureNova Security Monitor",
                Description = "Provides real-time security monitoring and threat detection for your system.",
                StartType = ServiceStartMode.Automatic
            };

            Installers.Add(_processInstaller);
            Installers.Add(_serviceInstaller);
        }
    }
} 