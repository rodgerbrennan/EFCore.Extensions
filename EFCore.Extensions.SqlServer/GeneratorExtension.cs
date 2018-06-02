using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFCore.Extensions.SqlServer
{
    public static class GeneratorExtension
    {

        public static string Generate(this DbContext @context)
        {
            var sBuilder = new StringBuilder();

            var contextChanges = context.ChangeTracker.Entries();

            var sqlMapper = new SqlServerTypeMapper(new RelationalTypeMapperDependencies());

            var entries = context.ChangeTracker.Entries();

            var addedEntities = (from ent in entries
                                 where ent.State == EntityState.Added
                                 select ent);

            var updatedEntities = (from ent in entries
                                 where ent.State == EntityState.Modified
                                 select ent);

            var deletedEntities = (from ent in entries
                                   where ent.State == EntityState.Deleted
                                   select ent);

            GenerateInserts(sBuilder, context, addedEntities, sqlMapper);
            GenerateUpdates(sBuilder, context, updatedEntities, sqlMapper);
            GenerateDeletes(sBuilder, context, deletedEntities, sqlMapper);

            return sBuilder.ToString();
        }

        private static void GenerateInserts(StringBuilder sBuilder, DbContext context, IEnumerable<EntityEntry> entries, SqlServerTypeMapper mapper)
        {
            
            foreach (var entry in entries)
            {
                var tableName = GetTableName(entry.Entity.GetType(), context);
                var lastProperty = entry.Properties.Last();
                sBuilder.Append("EXECUTE sp_executesql N'INSERT INTO ");
                sBuilder.Append(tableName);
                sBuilder.Append(" ");
                sBuilder.Append("(");

                //We'll need this list later to add only the necesary parameters in order
                var handledProperties = GetPropertiesToHandle(entry);

                foreach (var prop in entry.Properties)
                {
                    //Skip identity columnns and other generated values
                    if (prop.Metadata.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd) continue;
                    GenerateColumnName(sBuilder, prop);
                    sBuilder.Append((prop.Metadata.Name != lastProperty.Metadata.Name) ? ", ": "");
                    handledProperties.Add(prop);
                }

                sBuilder.Append(") VALUES (");

                var handledPropertyCount = handledProperties.Count();
                
                for (var i = 0; i < handledPropertyCount; i++)
                {
                    var prop = handledProperties[i];
                    sBuilder.Append(GenerateParameterName(i));
                    
                    sBuilder.Append((i != (handledPropertyCount - 1)) ? "," : "");                        
                }
                sBuilder.Append(")'");

                //Generate Parameters
                sBuilder.Append(", N'");
                GenerateParameters(sBuilder, handledProperties, handledPropertyCount);
                
                sBuilder.Append(";");
            }

        }

        private static void GenerateUpdates(StringBuilder sBuilder, DbContext context, IEnumerable<EntityEntry> entries, SqlServerTypeMapper mapper)
        {
            
            foreach (var entry in entries)
            {
                var tableName = GetTableName(entry.Entity.GetType(), context);
                var lastProperty = entry.Properties.Last();

                sBuilder.Append("EXECUTE sp_executesql N'UPDATE ");
                sBuilder.Append(tableName);
                sBuilder.Append(" SET ");

                //We'll need this list later to deal with only the necesary parameters in order
                var handledProperties = GetPropertiesToHandle(entry);
                PropertyEntry primaryKeyProperty = GetPrimaryKeyProperty(entry.Properties);
                var handledPropertyCount = handledProperties.Count;

                for (var i = 0; i < handledPropertyCount; i++)
                {
                    var prop = handledProperties[i];
                    GenerateColumnName(sBuilder, prop);
                    sBuilder.Append(" = ");
                    sBuilder.Append(GenerateParameterName(i));
                    sBuilder.Append((prop.Metadata.Name != lastProperty.Metadata.Name) ? ", " : "");
                }

                sBuilder.Append(" WHERE ");
                GenerateColumnName(sBuilder, primaryKeyProperty);
                sBuilder.Append("=");
                sBuilder.Append(GenerateParameterName(handledPropertyCount));
                sBuilder.Append("', N'");
                GenerateParameters(sBuilder, handledProperties, handledPropertyCount, primaryKeyProperty);
                sBuilder.Append(";");
            }

        }

        private static void GenerateDeletes(StringBuilder sBuilder, DbContext context, IEnumerable<EntityEntry> entries, SqlServerTypeMapper mapper)
        {
            
            foreach (var entry in entries)
            {
                var tableName = GetTableName(entry.Entity.GetType(), context);
                var lastProperty = entry.Properties.Last();
                var primaryKeyProperty = GetPrimaryKeyProperty(entry.Properties);

                sBuilder.Append("EXECUTE sp_executesql N'DELETE FROM ");
                sBuilder.Append(tableName);
                sBuilder.Append(" WHERE ");
                GenerateColumnName(sBuilder, primaryKeyProperty);
                sBuilder.Append(" = ");
                sBuilder.Append(GenerateParameterName(0));
                sBuilder.Append("'");

                sBuilder.Append(", N'");
                GenerateParameterTypeString(sBuilder, primaryKeyProperty, 0);
                sBuilder.Append("', ");
                GenerateParameterValueString(sBuilder, primaryKeyProperty, 0);
                sBuilder.Append(";");

            }
            
        }

        private static PropertyEntry GetPrimaryKeyProperty(IEnumerable<PropertyEntry> properties)
        {
            return (from prop in properties
                    where prop.Metadata.IsPrimaryKey()
                    select prop).FirstOrDefault();
        }

        private static List<PropertyEntry> GetPropertiesToHandle(EntityEntry entity)
        {
            var handledProperties = new List<PropertyEntry>();
            PropertyEntry primaryKeyProperty = null;

            foreach (var prop in entity.Properties)
            {
                if (prop.Metadata.IsPrimaryKey())
                {
                    primaryKeyProperty = prop;
                    continue;
                }
                //Skip identity columnns and other generated values
                if (prop.Metadata.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnUpdate ||
                    !prop.IsModified) continue;
                handledProperties.Add(prop);
            }

            return handledProperties;
        }

        private static void GenerateParameterTypeString(StringBuilder sBuilder, PropertyEntry prop, int position)
        {
            
            sBuilder.Append(GenerateParameterName(position));
            sBuilder.Append(" ");
            sBuilder.Append(prop.Metadata.SqlServer().ColumnType);
            
        }

        private static void GenerateParameterTypeDefinitions(StringBuilder sBuilder, List<PropertyEntry> handledProperties, int handledPropertyCount, PropertyEntry primaryKey)
        {

            for (var i = 0; i < handledPropertyCount; i++)
            {
                GenerateParameterTypeString(sBuilder, handledProperties[i], i);
                sBuilder.Append((i != (handledPropertyCount - 1)) ? "," : "");
            }

            if (primaryKey != null)
            {
                sBuilder.Append(',');
                GenerateParameterTypeString(sBuilder, primaryKey, handledPropertyCount);
            }

            sBuilder.Append("'");
        }

        private static void GenerateParameterValues(StringBuilder sBuilder, List<PropertyEntry> handledProperties, int handledPropertyCount)
        {
            
            for (var i = 0; i < handledPropertyCount; i++)
            {
                GenerateParameterValue(sBuilder, handledProperties[i], i, handledPropertyCount);
            }

        }

        private static void GenerateParameterValueString(StringBuilder sBuilder, PropertyEntry prop, int position)
        {
            
            sBuilder.Append(GenerateParameterName(position));
            sBuilder.Append(" = ");
            if (IsQuoteNeededForProperty(prop.Metadata.SqlServer().ColumnType))
            {
                //quote property
                sBuilder.Append("'");
                sBuilder.Append(prop.CurrentValue);
                sBuilder.Append("'");
            }
            else
            {
                //check for binary type (may need to do this for image type as well)
                if (prop.Metadata.SqlServer().ColumnType == "varbinary(max)")
                {
                    // This seems hacky, but it works
                    var binaryBuilder = new StringBuilder();
                    binaryBuilder.Append("0x");
                    binaryBuilder.Append(BitConverter.ToString((byte[])prop.CurrentValue).Replace("-", ""));
                    sBuilder.Append(binaryBuilder.ToString());
                    binaryBuilder = null;
                }
                else
                {
                    sBuilder.Append(prop.CurrentValue);
                }
            }

        }

        private static void GenerateParameterValue(StringBuilder sBuilder, PropertyEntry prop, int position, int handledPropertyCount)
        {

            GenerateParameterValueString(sBuilder, prop, position);

            // if we're not on the key and we're not on the last property (excluding the key) add a comma
            if (position != handledPropertyCount)
            {
                sBuilder.Append((position != (handledPropertyCount - 1)) ? "," : "");
            }            
        }

        private static void GenerateParameterValues(StringBuilder sBuilder, List<PropertyEntry> handledProperties, int handledPropertyCount, PropertyEntry primaryKey)
        {
            
            GenerateParameterValues(sBuilder, handledProperties, handledPropertyCount);

            if (primaryKey != null)
            {
                sBuilder.Append(",");
                GenerateParameterValue(sBuilder, primaryKey, handledPropertyCount, handledPropertyCount);
            }

        }

        private static void  GenerateParameters(StringBuilder sBuilder, List<PropertyEntry> handledProperties, int handledPropertyCount, PropertyEntry primaryKey)
        {

            GenerateParameterTypeDefinitions(sBuilder, handledProperties, handledPropertyCount, primaryKey);            

            sBuilder.Append(", ");
            //Generate Parameter Values

            GenerateParameterValues(sBuilder, handledProperties, handledPropertyCount, primaryKey);            
        }

        private static void GenerateParameters(StringBuilder sBuilder, List<PropertyEntry> handledProperties, int handledPropertyCount)
        {

            GenerateParameters(sBuilder, handledProperties, handledPropertyCount, null);

        }

        private static bool IsQuoteNeededForProperty(string propertyType)
        {
            
            return QuotedTypes().Where(q => q.Contains(propertyType)).Any();

        }

        private static string GenerateParameterName(int position)
        {
            return "@p" + position;
        }

        private static void GenerateColumnName(StringBuilder sBuilder, PropertyEntry property)
        {

            sBuilder.Append("[");
            sBuilder.Append(property.Metadata.SqlServer().ColumnName);
            sBuilder.Append("]");
            
        }

        private static string GetTableName(Type entityType, DbContext context)
        {
            return context.Model.FindEntityType(entityType).Relational().TableName;
        }

        private static List<string> QuotedTypes()
        {
            var retVal = new List<string>
            {
                "char varying",
                "char",
                "character varying",
                "character",
                "date",
                "datetime",
                "datetime2",
                "datetimeoffset",
                "national char varying",
                "national character varying",
                "national character",
                "nchar",
                "ntext",
                "nvarchar",
                "nvarchar(max)",
                "rowversion",
                "text",
                "time",
                "timestamp",
                "varchar",
                "uniqueidentifier",
                "xml"
            };

            return retVal;
        }

        private static List<string> UnQuotedTypes()
        {
            var retVal = new List<string>
            {
                "dec", 
                "decimal",
                "float",
                "image", 
                "int",
                "money", 
                "numeric",
                "real", 
                "smalldatetime",
                "smallint",
                "smallmoney",
                "tinyint",
                "varbinary"
            };
            
            return retVal;
        }
    }
}
