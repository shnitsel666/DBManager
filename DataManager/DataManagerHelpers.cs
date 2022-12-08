namespace Database.DataManager
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Dynamic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Common methods for DataManager.
    /// </summary>
    public partial class DataManager : IDisposable
    {
        #region SetPropertyOrField()

        /// <summary>
        /// Populates a model property.
        /// </summary>
        /// <param name="model">The model whose property is populated.</param>
        /// <param name="fieldOrProp">Data about the property to fill.</param>
        /// <param name="data">Data to fill.</param>
        private void SetPropertyOrField(object model, MemberInfo fieldOrProp, object? data)
        {
            Type propFieldType;
            if (fieldOrProp is PropertyInfo)
            {
                propFieldType = ((PropertyInfo)fieldOrProp).PropertyType;
            }
            else
            {
                propFieldType = ((FieldInfo)fieldOrProp).FieldType;
            }

            Action<object?> setValue = (value) =>
            {
                if (fieldOrProp is PropertyInfo)
                {
                    ((PropertyInfo)fieldOrProp).SetValue(model, value);
                }
                else
                {
                    ((FieldInfo)fieldOrProp).SetValue(model, value);
                }
            };

            // The order of types is important for optimization. Most used types first, then rare ones
            if (propFieldType == typeof(string))
            {
                setValue(data == DBNull.Value ? null : Convert.ToString(data));
            }
            else if (propFieldType == typeof(int))
            {
                setValue(Convert.ToInt32(data));
            }
            else if (propFieldType == typeof(int?))
            {
                setValue(data == DBNull.Value ? (int?)null : Convert.ToInt32(data));
            }
            else if (propFieldType == typeof(double))
            {
                setValue(Convert.ToDouble(data));
            }
            else if (propFieldType == typeof(double?))
            {
                setValue(data == DBNull.Value ? (double?)null : Convert.ToDouble(data));
            }
            else if (propFieldType == typeof(decimal))
            {
                setValue(Convert.ToDecimal(data));
            }
            else if (propFieldType == typeof(decimal?))
            {
                setValue(data == DBNull.Value ? (decimal?)null : Convert.ToDecimal(data));
            }
            else if (propFieldType == typeof(bool))
            {
                setValue(Convert.ToBoolean(data));
            }
            else if (propFieldType == typeof(bool?))
            {
                setValue(data == DBNull.Value ? (bool?)null : Convert.ToBoolean(data));
            }
            else if (propFieldType == typeof(DateTime))
            {
                setValue(Convert.ToDateTime(data));
            }
            else if (propFieldType == typeof(DateTime?))
            {
                setValue(data == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(data));
            }
            else if (propFieldType == typeof(Guid))
            {
                setValue(new Guid(Convert.ToString(data)));
            }
            else if (propFieldType == typeof(Guid?))
            {
                setValue(data == DBNull.Value ? (Guid?)null : new Guid(Convert.ToString(data)));
            }
            else if (propFieldType == typeof(long))
            {
                setValue(Convert.ToInt64(data));
            }
            else if (propFieldType == typeof(long?))
            {
                setValue(data == DBNull.Value ? (long?)null : Convert.ToInt64(data));
            }
            else if (propFieldType == typeof(float))
            {
                setValue((float)Convert.ToDouble(data));
            }
            else if (propFieldType == typeof(float?))
            {
                setValue(data == DBNull.Value ? (float?)null : (float)Convert.ToDouble(data));
            }
            else if (propFieldType == typeof(short))
            {
                setValue(Convert.ToInt16(data));
            }
            else if (propFieldType == typeof(short?))
            {
                setValue(data == DBNull.Value ? (short?)null : Convert.ToInt16(data));
            }
            else if (propFieldType == typeof(uint))
            {
                setValue(Convert.ToUInt32(data));
            }
            else if (propFieldType == typeof(uint?))
            {
                setValue(data == DBNull.Value ? (uint?)null : Convert.ToUInt32(data));
            }
            else if (propFieldType == typeof(ushort))
            {
                setValue(Convert.ToUInt16(data));
            }
            else if (propFieldType == typeof(ushort?))
            {
                setValue(data == DBNull.Value ? (ushort?)null : Convert.ToUInt16(data));
            }
            else if (propFieldType == typeof(ulong))
            {
                setValue(Convert.ToUInt64(data));
            }
            else if (propFieldType == typeof(ulong?))
            {
                setValue(data == DBNull.Value ? (ulong?)null : Convert.ToUInt64(data));
            }
            else
            {
                throw new DataManagerException("Неизвестный тип данных " + propFieldType.FullName);
            }
        }
        #endregion

        #region GenerateSQLForSelect()

        /// <summary>
        /// Генерация скрипта для выборки.
        /// </summary>
        /// <typeparam name="T">Тип модели.</typeparam>
        /// <param name="from">Откуда брать.</param>
        /// <param name="take">Сколько брать.</param>
        private string GenerateSQLForSelect<T>(uint? from, uint? take, OrderTypes orderType)
            where T : class, new()
        {
            string sql = string.Empty;
            Type modelType = typeof(T);
            TableAttribute? tableAttribute = (TableAttribute?)modelType
                .GetCustomAttributes(false)
                .FirstOrDefault(a =>
                a.GetType() == typeof(TableAttribute));
            if (tableAttribute == null)
            {
                throw new DataManagerException("У класса " + modelType.FullName + "не установлен атрибут [Table]");
            }

            if (from != null)
            {
                MemberInfo[] fieldsAndProps = modelType
                    .GetProperties()
                    .Where(p =>
                    (p.PropertyType.IsPrimitive ||
                    p.PropertyType.IsValueType ||
                    p.PropertyType == typeof(string)) &&
                    !p.CustomAttributes.Any(c => c.AttributeType == typeof(OrderAttribute)))
                    .ToArray();
                fieldsAndProps = fieldsAndProps.Concat(
                    modelType
                    .GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p =>
                    (p.FieldType.IsPrimitive ||
                    p.FieldType.IsValueType ||
                    p.FieldType == typeof(string)) &&
                    p.CustomAttributes.Any(c => c.AttributeType == typeof(OrderAttribute))))
                    .ToArray();
                if (fieldsAndProps == null || fieldsAndProps.Length <= 0)
                {
                    throw new DataManagerException("Для использования FROM TAKE в выборке вы должны указать атрибут [Order] на одном из свойств класса");
                }

                string? overColumn = fieldsAndProps?.FirstOrDefault()?.Name;
                ColumnAttribute? columnAttribute = (ColumnAttribute?)fieldsAndProps?.FirstOrDefault()?.GetCustomAttribute(typeof(ColumnAttribute));
                if (columnAttribute != null)
                {
                    overColumn = columnAttribute.ColumnName;
                }

                OrderAttribute? rovOverAttribute = (OrderAttribute?)fieldsAndProps?.FirstOrDefault()?.GetCustomAttribute(typeof(OrderAttribute));
                string rowOverType = orderType == OrderTypes.ASC ? "ASC" : orderType == OrderTypes.DESC ? "DESC" : string.Empty;
                string takeStr = take == null ? string.Empty : "TOP " + take + " ";
                sql = $@"SELECT {takeStr} * FROM (SELECT *, ROW_NUMBER() OVER(ORDER BY {overColumn} {rowOverType}) AS [ROW_NUMBER]
                FROM {tableAttribute.TableName} WITH (NOLOCK)) AS [RESULT_TABLE] WHERE [RESULT_TABLE].[ROW_NUMBER] >= {from}" +
                (orderType == OrderTypes.NONE ? string.Empty : $"ORDER BY {overColumn} {rowOverType}");
            }
            else if (take != null)
            {
                sql = $"SELECT TOP {take} * FROM {tableAttribute.TableName} WITH(NOLOCK)";
            }
            else
            {
                sql = $"SELECT * FROM {tableAttribute.TableName} WITH(NOLOCK)";
            }

            return sql;
        }
        #endregion

        #region ConvertReaderToDynamicArray()

        /// <summary>
        /// Turns SqlDataReader в List T.
        /// </summary>
        /// <param name="reader">IDataReader reader.</param>
        /// <returns>Returns IEnumerable dynamyc object.</returns>
        private static IEnumerable<dynamic> ConvertReaderToDynamicArray(IDataReader reader)
        {
            List<string> names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
            foreach (IDataRecord record in reader as IEnumerable)
            {
                var expando = new ExpandoObject() as IDictionary<string, object>;
                foreach (var name in names)
                {
                    expando[name] = record[name];
                }

                yield return expando;
            }
        }
        #endregion

        #region ConvertReaderToDynamic()
        private static dynamic? ConvertReaderToDynamic(IDataReader reader)
        {
            List<string> names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
            foreach (IDataRecord record in reader as IEnumerable)
            {
                var expando = new ExpandoObject() as IDictionary<string, object>;
                foreach (var name in names)
                {
                    expando[name] = record[name];
                }

                return expando;
            }

            return null;
        }
        #endregion

        #region ConvertListToDataTable()
        public static DataTable ConvertListToDataTable<T>(List<T> data, params string[]? headers)
        {
            DataTable table = new();
            if (data == null)
            {
                return table;
            }

            var first = data.FirstOrDefault();
            if (first == null)
            {
                return table;
            }

            if (first is ExpandoObject)
            {
                IDictionary<string, object>? dictionary = first as IDictionary<string, object>;
                if (headers != null && headers.Count() > 0)
                {
                    foreach (var item in headers)
                    {
                        table.Columns.Add(item);
                    }
                }
                else
                {
                    foreach (var item in dictionary)
                    {
                        table.Columns.Add(item.Key);
                    }
                }

                foreach (var item in data)
                {
                    DataRow row = table.NewRow();
                    List<object> itemArray = new();
                    IDictionary<string, object>? valuePairs = item as IDictionary<string, object>;
                    foreach (var pair in valuePairs)
                    {
                        itemArray.Add(pair.Value);
                    }

                    row.ItemArray = itemArray.ToArray();
                    table.Rows.Add(row);
                }
            }
            else
            {
                Type typeForHeaders = first.GetType();
                MemberInfo[] fieldsAndProps = typeForHeaders
                    .GetProperties()
                    .Where(p =>
                    (p.PropertyType.IsPrimitive ||
                    p.PropertyType.IsValueType ||
                    p.PropertyType == typeof(string)))
                    .ToArray();
                fieldsAndProps = fieldsAndProps.Concat(
                    typeForHeaders
                    .GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p =>
                    (p.FieldType.IsPrimitive ||
                    p.FieldType.IsValueType ||
                    p.FieldType == typeof(string))))
                    .ToArray();
                if (headers != null && headers.Count() > 0)
                {
                    foreach (var item in headers)
                    {
                        table.Columns.Add(item);
                    }
                }
                else
                {
                    foreach (var item in fieldsAndProps)
                    {
                        ColumnAttribute? columnAttribute = item.GetCustomAttribute<ColumnAttribute>();
                        table.Columns.Add(columnAttribute == null ? item.Name : columnAttribute.ColumnName);
                    }
                }

                foreach (var item in data)
                {
                    DataRow row = table.NewRow();
                    List<object?> itemArray = new();
                    foreach (var fieldOrProp in fieldsAndProps)
                    {
                        if (fieldOrProp is PropertyInfo)
                        {
                            itemArray.Add(((PropertyInfo)fieldOrProp).GetValue(item));
                        }
                        else
                        {
                            itemArray.Add(((FieldInfo)fieldOrProp).GetValue(item));
                        }
                    }

                    row.ItemArray = itemArray.ToArray();
                    table.Rows.Add(row);
                }
            }

            return table;
        }

        public static DataTable ConvertListToDataTable<T>(List<T> data) => ConvertListToDataTable(data, null);
        #endregion

        public void Dispose()
        {
            Connection.Close();
            Connection.Dispose();
        }
    }
}
