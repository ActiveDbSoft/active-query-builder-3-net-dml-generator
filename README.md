# active-query-builder-3-net-dml-generator
This repository demonstrates how to generate INSERT, UPDATE and DELETE statements for the query currently edited in the Active Query Builder .NET v3.

There are 2 main entities:
 - `ISqlSyntaxOverride` - interface to describe SQL syntax and database client specific features. There are 2 implementations of this inteface in the demo: `CommonSqlSyntaxOverride` - for base features and `MsSqlSyntaxOverride` - for the standard MS SQL Sever client library (System.Data.SqlClient)
 - `DmlSqlGenerator` - the DML statements generator itself

 Usage:
 ```c#
var dmlGen = new DmlSqlGenerator(queryBuilder1, new MsSqlSyntaxOverride());

MessageBox.Show(dmlGen.GenerateInsertSql(), "Insert");
MessageBox.Show(dmlGen.GenerateAllFieldsUpdateSql(), "All fields update");
MessageBox.Show(dmlGen.GenerateSingleFieldUpdateSql(), "Single field update");
MessageBox.Show(dmlGen.GenerateDeleteSql(), "Delete");
 ```

Feel free to copy and customize this generator for your needs.