namespace RolgutXmlFromApi.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<RolgutXmlFromApi.Data.MyDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(RolgutXmlFromApi.Data.MyDbContext context)
        {
            string sql = @"
            DECLARE @loginName sysname;

            SELECT @loginName = name FROM sys.server_principals WHERE sid = 0x010100000000000514000000;

            IF @loginName IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = @loginName)
                BEGIN
                    EXEC('CREATE USER [' + @loginName + '] FOR LOGIN [' + @loginName + ']');
                    EXEC('EXEC sp_addrolemember ''db_datareader'', ''' + @loginName + '''');
                    EXEC('EXEC sp_addrolemember ''db_datawriter'', ''' + @loginName + '''');
                END
            END";

            context.Database.ExecuteSqlCommand(sql);
            context.Database.ExecuteSqlCommand("SET IDENTITY_INSERT Products ON;");
        }
    }
}