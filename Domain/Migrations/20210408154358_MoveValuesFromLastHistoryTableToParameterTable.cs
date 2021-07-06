using Microsoft.EntityFrameworkCore.Migrations;

namespace vws.web.Migrations
{
    public partial class MoveValuesFromLastHistoryTableToParameterTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DECLARE @CursorID BIGINT = 1;
DECLARE @RowCount BIGINT = 0;
			DECLARE @Parameters NVARCHAR(MAX);
			DECLARE @HistoryId BIGINT;
			DECLARE @CommaSepratedTable TABLE(Parameter NVARCHAR(MAX));

			SELECT @RowCount = COUNT(0) FROM dbo.Team_TeamHistory;

			WHILE @CursorID <= @RowCount
BEGIN
	DELETE @CommaSepratedTable
	SELECT @Parameters = CommaSepratedParameters, @HistoryId = Id
	FROM dbo.Team_TeamHistory
	WHERE Id = @CursorID;

			IF @Parameters LIKE '%,%'
	BEGIN
	WITH CommaSeprated AS
	(
		SELECT STUFF(@Parameters, 1, CHARINDEX(',', @Parameters),'') as Number,  
		CONVERT(VARCHAR(MAX), LEFT(@Parameters, CHARINDEX(',', @Parameters) - 1)) Parameter
		 UNION ALL

		 SELECT STUFF(Number, 1, CHARINDEX(',', Number + ','), '') Number,  
		convert(VARCHAR(MAX), LEFT(Number, (CASE WHEN CHARINDEX(',', Number) = 0 THEN LEN(NUMBER) ELSE CHARINDEX(',', Number) - 1 END)) )Parameter
		 FROM CommaSeprated WHERE LEN(Number) > 0
	)  
	INSERT @CommaSepratedTable
	(
		Parameter
	)
	SELECT Parameter FROM CommaSeprated
	END
	ELSE
	BEGIN
	INSERT @CommaSepratedTable
	(
		Parameter
	)
	SELECT @Parameters AS CommaSeprated
	END

	INSERT INTO dbo.Team_TeamHistoryParameter
	(
		ActivityParameterTypeId,
		TeamHistoryId,
		Body
	)
	SELECT 1, @HistoryId, Parameter FROM @CommaSepratedTable

	SET @CursorID = @CursorID + 1
END");

			migrationBuilder.Sql(@"DECLARE @CursorID BIGINT = 1;
DECLARE @RowCount BIGINT = 0;
DECLARE @Parameters NVARCHAR(MAX);
DECLARE @HistoryId BIGINT;
DECLARE @CommaSepratedTable TABLE (Parameter NVARCHAR(MAX));

SELECT @RowCount = COUNT(0) FROM dbo.Project_ProjectHistory;

WHILE @CursorID <= @RowCount
BEGIN
	DELETE @CommaSepratedTable
	SELECT @Parameters = CommaSepratedParameters, @HistoryId = Id
	FROM dbo.Project_ProjectHistory
	WHERE Id = @CursorID;
	
	IF @Parameters LIKE '%,%'
	BEGIN
	WITH CommaSeprated AS
	(
		SELECT STUFF(@Parameters, 1, CHARINDEX(',', @Parameters),'') as Number,  
		CONVERT(VARCHAR(MAX), LEFT(@Parameters, CHARINDEX(',', @Parameters)-1 )) Parameter  
		UNION ALL  
  
		SELECT STUFF(Number , 1, CHARINDEX(',', Number+',') ,'') Number,  
		convert(VARCHAR(MAX), LEFT( Number, (CASE WHEN CHARINDEX(',', Number) = 0 THEN LEN(NUMBER) ELSE CHARINDEX(',', Number)-1 END)) )Parameter  
		FROM CommaSeprated WHERE LEN(Number) >0  
	)  
	INSERT @CommaSepratedTable
	(
	    Parameter
	)
	SELECT Parameter FROM CommaSeprated  
	END
	ELSE
	BEGIN
	INSERT @CommaSepratedTable
	(
	    Parameter
	)
	SELECT @Parameters AS CommaSeprated
	END

	INSERT INTO dbo.Project_ProjectHistoryParameter
	(
	    ActivityParameterTypeId,
	    ProjectHistoryId,
	    Body
	)
	SELECT 1, @HistoryId, Parameter FROM @CommaSepratedTable

	SET @CursorID = @CursorID + 1
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
