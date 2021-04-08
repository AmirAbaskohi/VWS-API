using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class UpdatedMarkMessageAsReadStoredProcedure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER PROCEDURE [dbo].[MarkUnreadMessages]
                                @MessageId BIGINT,
                                @UserId UNIQUEIDENTIFIER
                            AS
                            BEGIN
                            
                                DECLARE @ChannelId UNIQUEIDENTIFIER;
                                DECLARE @FromUserId UNIQUEIDENTIFIER;
                                DECLARE @ChannelTypeId TINYINT;
                                DECLARE @Result TABLE
                                (
                                    MessageId BIGINT,
                                    ChannelId UNIQUEIDENTIFIER,
                                    ChannelTypeId TINYINT
                                );
                            
                            
                                SELECT @ChannelTypeId = m.ChannelTypeId,
                                       @ChannelId = m.ChannelId,
                                       @FromUserId = m.FromUserId
                                FROM dbo.Chat_Message m
                                WHERE m.Id = @MessageId;
                            
                                IF (@ChannelTypeId = 1 AND @FromUserId = @UserId)
                                BEGIN
                                    SELECT TOP (1) @MessageId = m.Id,
                                           @ChannelTypeId = m.ChannelTypeId,
                                           @ChannelId = m.ChannelId
                                    FROM dbo.Chat_Message m
                                    WHERE m.FromUserId = @ChannelId AND m.ChannelId = @FromUserId
                                    ORDER BY m.SendOn DESC;
                                END;
                            
                            
                                INSERT INTO @Result
                                (
                                    MessageId,
                                    ChannelId,
                                    ChannelTypeId
                                )
                                SELECT m.Id,
                                       m.ChannelId,
                                       m.ChannelTypeId
                                FROM dbo.Chat_Message m
                                    LEFT OUTER JOIN
                                    (
                                        SELECT *
                                        FROM dbo.Chat_MessageRead
                                        WHERE dbo.Chat_MessageRead.ReadBy = @UserId
                                    ) mr
                                        ON m.Id = mr.MessageId
                                WHERE m.Id <= @MessageId
                                      AND mr.Id IS NULL
                                      AND m.FromUserId <> @UserId
                                      AND m.ChannelId = @ChannelId
                                      AND m.ChannelTypeId = @ChannelTypeId;
                            
                                INSERT dbo.Chat_MessageRead
                                (
                                    MessageId,
                                    ChannelId,
                                    ChannelTypeId,
                                    ReadBy
                                )
                                SELECT MessageId,
                                       ChannelId,
                                       ChannelTypeId,
                                       @UserId
                                FROM @Result;
                            
                                SELECT *
                                FROM dbo.Chat_MessageRead mr
                                WHERE EXISTS
                                (
                                    SELECT *
                                    FROM @Result
                                    WHERE MessageId = mr.MessageId
                                          AND ChannelId = mr.ChannelId
                                          AND ChannelTypeId = mr.ChannelTypeId
                                )
                                      AND mr.ReadBy = @UserId;
                            
                            END;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER PROCEDURE [dbo].[MarkUnreadMessages]
@MessageId BIGINT,
@UserId UNIQUEIDENTIFIER
AS
BEGIN

	DECLARE @ChannelId UNIQUEIDENTIFIER
	DECLARE @ChannelTypeId TINYINT
	DECLARE @Result TABLE (MessageId BIGINT, ChannelId UNIQUEIDENTIFIER, ChannelTypeId TINYINT)


	SELECT @ChannelTypeId = m.ChannelTypeId,
         @ChannelId = m.ChannelId
	FROM dbo.Chat_Message m
	WHERE m.Id = @MessageId

	INSERT INTO @Result
	(
	    MessageId,
	    ChannelId,
	    ChannelTypeId
	)
	SELECT m.Id,
		   m.ChannelId,
           m.ChannelTypeId
	FROM dbo.Chat_Message m
	LEFT OUTER JOIN (SELECT * FROM dbo.Chat_MessageRead WHERE dbo.Chat_MessageRead.ReadBy = @UserId) mr
	ON m.Id = mr.MessageId
	WHERE m.Id <= @MessageId AND
		  mr.Id IS NULL AND
		  m.FromUserId <> @UserId AND
		  m.ChannelId = @ChannelId AND
		  m.ChannelTypeId = @ChannelTypeId

	INSERT dbo.Chat_MessageRead
	(
	    MessageId,
	    ChannelId,
	    ChannelTypeId,
	    ReadBy
	)
	SELECT MessageId,
           ChannelId,
           ChannelTypeId,
		   @UserId
	FROM @Result

	SELECT *
	FROM dbo.Chat_MessageRead mr
	WHERE EXISTS (SELECT * FROM @Result WHERE MessageId = mr.MessageId AND ChannelId = mr.ChannelId AND ChannelTypeId = mr.ChannelTypeId) AND mr.ReadBy = @UserId

END
");
        }
    }
}
