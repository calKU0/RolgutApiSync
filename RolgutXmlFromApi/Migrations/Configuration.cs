namespace RolgutXmlFromApi.Migrations
{
    using System.Data.Entity.Migrations;

    internal sealed class Configuration : DbMigrationsConfiguration<RolgutXmlFromApi.Data.MyDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            AutomaticMigrationDataLossAllowed = false;
        }

        protected override void Seed(RolgutXmlFromApi.Data.MyDbContext context)
        {
        }
    }
}