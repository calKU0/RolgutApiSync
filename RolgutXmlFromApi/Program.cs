using System.ServiceProcess;

namespace RolgutXmlFromApi
{
    internal static class Program
    {
        private static void Main()
        {
            var configuration = new Migrations.Configuration();
            var migrator = new System.Data.Entity.Migrations.DbMigrator(configuration);
            migrator.Update();

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new RolgutService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}