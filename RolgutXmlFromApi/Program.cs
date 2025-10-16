using System.ServiceProcess;

namespace GaskaSyncService
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
                new GaskaSyncService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}