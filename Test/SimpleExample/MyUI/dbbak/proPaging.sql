
create PROCEDURE [dbo].[proPaging]
  @TableName nvarchar(50)=null, --表名
  @OrderBy nvarchar(100)=null, --按排序SQL语句，例如（CreatedDate DESC,LastAlterDate ASC）
  @ColumnList nvarchar(500)='*',--要查询出的字段列表,*表示全部字段
  @PageSize int=10,        --每页记录数
  @PageIndex int=1,         --指定页
  @Condition nvarchar(max)=null,--查询条件
  @PageCount int=0 OUTPUT,   --总页数
  @RecordCount int=0 OUTPUT   --总记录数
 
AS

SET @TableName='['+@TableName+'] WITH(NOLOCK) '
DECLARE @sql nvarchar(max),@where nvarchar(max)

IF @Condition is null or rtrim(@Condition)=''
BEGIN--没有查询条件
  SET @Condition=' IsDeleted=0 '
END
ELSE
BEGIN--有查询条件
  SET @Condition=' '+@Condition+' AND IsDeleted=0 '
END
 
SET @where=' WHERE ('+@Condition+') '--原本没有条件而加上此条件

SET @sql='SELECT @PageCount=CEILING((COUNT(*)+0.0)/'+CAST(@PageSize AS nvarchar)+'),@RecordCount=COUNT(*) FROM '+@TableName+@where
EXEC sp_executesql @sql,N'@PageCount int OUTPUT,@RecordCount int OUTPUT',@PageCount OUTPUT,@RecordCount OUTPUT

DECLARE @Start INT
DECLARE @End INT
SELECT @Start = (@PageIndex-1)*@PageSize,@End = @PageIndex*@PageSize;
 
SET @sql='
WITH TMPWITH AS (SELECT '+@ColumnList+', ROW_NUMBER() OVER (ORDER BY '+@OrderBy+') AS RowNumber
FROM '+@TableName+@where+') SELECT '+@ColumnList+' FROM TMPWITH
WHERE RowNumber > '+CAST(@Start AS nvarchar)+' AND RowNumber <= '+CAST(@End AS nvarchar)+' ORDER BY '+@OrderBy
--print @sql
EXEC(@sql)


