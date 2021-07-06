using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class RemoveRedundantReadMessagesQuery : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"with cte as (
                                    select row_number() over(partition by MessageId, ReadBy ORDER by (select null)) rn
                                    from dbo.Chat_MessageRead
                                )
                                delete from cte where rn > 1
                                ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
