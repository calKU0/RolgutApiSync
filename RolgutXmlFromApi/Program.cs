using System.ServiceProcess;

namespace RolgutXmlFromApi
{
    internal static class Program
    {
        private static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new RolgutService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}