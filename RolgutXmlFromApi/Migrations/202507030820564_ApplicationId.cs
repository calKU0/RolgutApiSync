namespace GaskaSyncService.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ApplicationId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Applications", "ApplicationId", c => c.Int(nullable: false));
        }

        public override void Down()
        {
            DropColumn("dbo.Applications", "ApplicationId");
        }
    }
}