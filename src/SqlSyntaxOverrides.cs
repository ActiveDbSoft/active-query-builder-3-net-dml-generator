using ActiveQueryBuilder.Core;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace YourCompany.YourProjectName.Infrastructure
{
    public interface ISqlSyntaxOverride
    {
        bool IsComparableField(MetadataField field);
        void SetParameterType(IDbDataParameter parameter, DbType type, string fieldTypeName = "");
        bool IsEditableType(DbType type);
        string GetParamSymbol();
    }

    public class CommonSqlSyntaxOverride : ISqlSyntaxOverride
    {
        private static readonly HashSet<DbType> NotEditableTypes = new HashSet<DbType>
            {
                DbType.Binary,
                DbType.Object
            };
        private static readonly HashSet<string> CommonNonComparableTypes = new HashSet<string>();

        public bool IsComparableField(MetadataField field)
            => !CommonNonComparableTypes.Contains(field.FieldTypeName);

        public void SetParameterType(IDbDataParameter parameter, DbType type, string fieldTypename = "")
            => parameter.DbType = type;

        public bool IsEditableType(DbType type)
            => !NotEditableTypes.Contains(type);

        public string GetParamSymbol()
            => ":";
    }

    public class MsSqlSyntaxOverride : ISqlSyntaxOverride
    {
        private static readonly HashSet<string> MsSqlNonComparableTypes = new HashSet<string>
            {
                "xml"
            };

        public bool IsComparableField(MetadataField field)
            => !MsSqlNonComparableTypes.Contains(field.FieldTypeName) && new CommonSqlSyntaxOverride().IsComparableField(field);

        public void SetParameterType(IDbDataParameter parameter, DbType type, string fieldTypename = "")
        {
            switch (type)
            {
                case DbType.Time:
                    ((SqlParameter)parameter).SqlDbType = SqlDbType.Time;
                    break;
                case DbType.Date:
                    ((SqlParameter)parameter).SqlDbType = SqlDbType.Date;
                    break;
                default:
                    new CommonSqlSyntaxOverride().SetParameterType(parameter, type);
                    break;
            }

            if (fieldTypename == "date")
                ((SqlParameter)parameter).SqlDbType = SqlDbType.Date;
        }

        public bool IsEditableType(DbType type)
            => new CommonSqlSyntaxOverride().IsEditableType(type);

        public string GetParamSymbol()
            => "@";
    }
}
