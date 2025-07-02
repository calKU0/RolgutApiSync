using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

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