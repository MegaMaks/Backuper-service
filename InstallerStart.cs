using System;
using System.ComponentModel;
using System.ServiceProcess;


namespace ServiceBack
{
    [RunInstaller(true)]
    public partial class InstallerStart : System.Configuration.Install.Installer
    {
        ServiceInstaller serviceInstaller;
        ServiceProcessInstaller processInstaller;

        public InstallerStart()
        {
            InitializeComponent();
            serviceInstaller = new ServiceInstaller();
            processInstaller = new ServiceProcessInstaller();

            processInstaller.Account = ServiceAccount.User;
            processInstaller.Username = Environment.UserDomainName + "\\" + Environment.UserName;
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.ServiceName = "ServiceBack";
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }



        

    }
}
