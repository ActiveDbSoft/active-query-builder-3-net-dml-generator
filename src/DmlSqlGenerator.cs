using ActiveQueryBuilder.Core;
using ActiveQueryBuilder.View.WinForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YourCompany.YourProjectName.Infrastructure
{
    public class DmlSqlGenerator
    {
        private readonly SQLGenerationOptions _sqlGenerationOptions;
        private readonly MetadataObject _selectedMetadataObject;
        private readonly StatisticsOutputColumnList _outputColumns;
        private readonly ISqlSyntaxOverride _syntaxOverride;

        public DmlSqlGenerator(QueryBuilder queryBuilder, ISqlSyntaxOverride syntaxOverride)
        {
            _syntaxOverride = syntaxOverride;
            _sqlGenerationOptions = queryBuilder.SQLContext.SQLGenerationOptionsForServer;
            _outputColumns = queryBuilder.QueryStatistics.OutputColumns;

            if (queryBuilder.SQLQuery.QueryRoot.IsQueryWithUnions())
                throw new Exception("Not editable query");

            var metadataObjectsList = _outputColumns
                .Select(outputColumn => outputColumn.MetadataObject)
                .Where(metadataObject => metadataObject != null)
                .Distinct();

            _selectedMetadataObject = GetSelectedObject(metadataObjectsList);

            if (_selectedMetadataObject == null)
                throw new Exception("Not editable query");
        }

        public string GenerateSelectSql()
            => $"Select * from {_selectedMetadataObject.NameFull}{Environment.NewLine}{GenerateWhereSql("Old_")}";

        public string GenerateInsertSql()
        {
            var insert = new StringBuilder();

            // header
            insert
                .AppendLine("INSERT INTO")
                .AppendLine(" " + _selectedMetadataObject.NameFull);

            // fields part
            {
                var writeSeparator = false;
                foreach (var outputColumn in _outputColumns)
                {
                    var metadataField = outputColumn.MetadataField;
                    if (metadataField == null || metadataField.ReadOnly)
                        continue;

                    if (outputColumn.MetadataObject == _selectedMetadataObject)
                    {
                        insert
                            .Append(" ")
                            .Append(writeSeparator ? ", " : " (")
                            .AppendLine(metadataField.GetNameSQL(_sqlGenerationOptions));
                        writeSeparator = true;
                    }
                }
                insert.AppendLine(" )");
            }

            // values part
            {
                insert.AppendLine("VALUES");
                var writeSeparator = false;
                foreach (var outputColumn in _outputColumns)
                {
                    var field = outputColumn.MetadataField;
                    if (outputColumn.MetadataObject == _selectedMetadataObject && field != null && !field.ReadOnly)
                    {
                        insert
                            .Append(' ')
                            .Append(writeSeparator ? ", " : " (")
                            .Append(_syntaxOverride.GetParamSymbol())
                            .AppendLine(outputColumn.FieldName);
                        writeSeparator = true;
                    }
                }
                insert.Append(" )");
            }

            return insert.ToString();
        }

        public string GenerateAllFieldsUpdateSql()
        {
            var update = new StringBuilder();

            update
                .AppendLine("UPDATE")
                .AppendLine(_selectedMetadataObject.NameFull)
                .AppendLine("SET");

            var writeSeparator = false;
            foreach (var metadataField in _selectedMetadataObject.Items.Fields.Where(f => !f.ReadOnly))
            {
                if (!_syntaxOverride.IsEditableType(metadataField.FieldType))
                    continue;

                var outputColumn = IsFieldSelected(_selectedMetadataObject, metadataField);
                if (outputColumn == null)
                    continue;

                if (writeSeparator)
                    update.Append(',');

                update
                    .Append(' ')
                    .Append(metadataField.GetNameSQL(_sqlGenerationOptions))
                    .Append(" = " + _syntaxOverride.GetParamSymbol())
                    .AppendLine(outputColumn.FieldName);

                writeSeparator = true;
            }

            update.Append(GenerateWhereSql("Old_"));

            return update.ToString();
        }

        public string GenerateSingleFieldUpdateSql()
        {
            var update = new StringBuilder()
                .AppendLine("UPDATE")
                .AppendLine(_selectedMetadataObject.NameFull)
                .AppendLine("SET")
                .AppendLine("__editingField = " + _syntaxOverride.GetParamSymbol() + "editingValue")
                .Append(GenerateWhereSql("Old_"));

            return update.ToString();
        }

        public string GenerateDeleteSql()
        {
            var delete = new StringBuilder()
                .AppendLine("DELETE FROM")
                .AppendLine(" " + _selectedMetadataObject.NameFull)
                .Append(GenerateWhereSql());

            return delete.ToString();
        }

        private string GenerateWhereSql(string prefix = "")
        {
            var where = new StringBuilder()
                .AppendLine("WHERE");
            
            bool writeSeparator;
            if (IsObjectHavePrimaryKey(_selectedMetadataObject))
            {
                writeSeparator = false;
                foreach (var metadataField in _selectedMetadataObject.Items.Fields)
                {
                    if (!metadataField.PrimaryKey)
                        continue;

                    var outputColumn = IsFieldSelected(_selectedMetadataObject, metadataField);

                    where
                        .Append(writeSeparator ? " AND " : " ")
                        .Append(metadataField.GetNameSQL(_sqlGenerationOptions))
                        .Append(" = " + _syntaxOverride.GetParamSymbol() + prefix)
                        .AppendLine(outputColumn.FieldName);

                    writeSeparator = true;
                }
            }
            else
            {
                writeSeparator = false;
                foreach (StatisticsOutputColumn oc in _outputColumns)
                {
                    if (oc.MetadataObject == _selectedMetadataObject)
                    {
                        MetadataField field = oc.MetadataField;
                        if (!_syntaxOverride.IsEditableType(field.FieldType))
                            continue;

                        if (!_syntaxOverride.IsComparableField(field))
                            continue;

                        where.Append(' ');
                        if (writeSeparator)
                            where.Append("AND ");
                        where.Append(field.GetNameSQL(_sqlGenerationOptions));
                        where.Append(" = " + _syntaxOverride.GetParamSymbol() + prefix);
                        where.Append(oc.FieldName);
                        where.Append(Environment.NewLine);
                        writeSeparator = true;
                    }
                }
            }

            return where.ToString();
        }

        private MetadataObject GetSelectedObject(IEnumerable<MetadataObject> metadataObjectsList)
        {
            foreach (var metadataObject in metadataObjectsList)
            {
                var selectedMetadataObject = metadataObject;

                if (IsObjectHavePrimaryKey(metadataObject))
                {
                    if (metadataObject.Items.Fields.Any(field => field.PrimaryKey && IsFieldSelected(metadataObject, field) == null))
                        selectedMetadataObject = null;
                }
                else
                {
                    if (metadataObject.Items.Fields.Any(field => IsFieldSelected(metadataObject, field) == null))
                        selectedMetadataObject = null;
                }

                if (selectedMetadataObject != null)
                    return selectedMetadataObject;
            }

            return null;
        }

        private bool IsObjectHavePrimaryKey(MetadataObject metadataObject)
            => metadataObject.Items.Fields.Any(field => field.PrimaryKey);

        private StatisticsOutputColumn IsFieldSelected(MetadataObject metadata, MetadataField field)
        {
            var sqlContext = metadata.SQLContext;

            return _outputColumns
                .FirstOrDefault(column => column.MetadataObject == metadata && column.MetadataField != null && sqlContext.IsIdentifiersEqual(column.MetadataField.GetNameIdentifier(), field.GetNameIdentifier()));
        }
    }
}
