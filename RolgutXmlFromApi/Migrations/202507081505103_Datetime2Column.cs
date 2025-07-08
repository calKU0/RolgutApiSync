namespace RolgutXmlFromApi.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class Datetime2Column : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Products", "CreatedDate", c => c.DateTime(nullable: false, precision: 7, storeType: "datetime2"));
            AlterColumn("dbo.Products", "UpdatedDate", c => c.DateTime(nullable: false, precision: 7, storeType: "datetime2"));
        }

        public override void Down()
        {
            AlterColumn("dbo.Products", "UpdatedDate", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Products", "CreatedDate", c => c.DateTime(nullable: false));
        }
    }
}