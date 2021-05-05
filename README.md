# active-query-builder-3-net-dml-generator
This repository demonstrates how to generate INSERT, UPDATE and DELETE statements for a given SELECT  query. It uses the API of the [Active Query Builder for .NET v.3](https://www.activequerybuilder.com/) to analyze the query.

There are 2 main entities:
- `ISqlSyntaxOverride` - interface to describe SQL syntax and database client specific features.
There are 2 implementations of this inteface in the demo: `CommonSqlSyntaxOverride` - for base features and `MsSqlSyntaxOverride` - for the standard MS SQL Sever client library (System.Data.SqlClient).
You can add other overrides by analogy.
- `DmlSqlGenerator` - the DML statements generator itself.

**Usage:**
```c#
var dmlGen = new DmlSqlGenerator(queryBuilder1, new MsSqlSyntaxOverride());
MessageBox.Show(dmlGen.GenerateInsertSql(), "Insert");
MessageBox.Show(dmlGen.GenerateAllFieldsUpdateSql(), "All fields update");
MessageBox.Show(dmlGen.GenerateSingleFieldUpdateSql(), "Single field update");
MessageBox.Show(dmlGen.GenerateDeleteSql(), "Delete");
```
