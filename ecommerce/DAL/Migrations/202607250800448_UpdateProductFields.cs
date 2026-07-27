namespace DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateProductFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Products", "Status", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Products", "Status");
        }
    }
}
