namespace DatabaseManager
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Data.SqlClient;

    /// <summary>
    /// Async part of DBManager.
    /// </summary>
    public partial class DBManager : IDisposable
    {
        #region GetListAsync<T>(sql, isStoredProcedure, parameters)

        /// <summary>
        /// Returns array of objects from query result.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="sql">SQL query.</param>
        /// <param name="isStoredProcedure">Whether the query is a stored procedure.</param>
        /// <param name="parameters">Object with parameters. Parameter name is object key, parameter value is object value.</param>
        /// <returns>Array of objects.</returns>
        public async Task<List<T>> GetListAsync<T>(string sql, bool isStoredProcedure, object? parameters)
            where T : class, new()
        {
            List<T> result = new();
            using (SqlCommand cmd = new(sql, Connection))
            {
                cmd.CommandTimeout = TimeOut;
                if (isStoredProcedure)
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                }

                if (parameters != null)
                {
                    PropertyInfo[] parameterProperties = parameters.GetType().GetProperties();
                    if (parameterProperties != null)
                    {
                        foreach (PropertyInfo parameter in parameterProperties)
                        {
                            object? parameterValue = parameter.GetValue(parameters);
                            cmd.Parameters.AddWithValue(parameter.Name, parameterValue ?? DBNull.Value);
                        }
                    }
                }

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            // Getting all fields and properties, which is the primitives and don't have attribute DoNotSelect
                            T tObj = new();
                            Type tObjType = tObj.GetType();
                            MemberInfo[] fieldsAndProps = tObjType.GetProperties().
                                Where(p => (p.PropertyType.IsPrimitive ||
                                p.PropertyType.IsValueType ||
                                p.PropertyType == typeof(string)) &&
                                !p.CustomAttributes.Any(c => c.AttributeType == typeof(NotSelectAttribute))).ToArray();

                            fieldsAndProps = fieldsAndProps.Concat(
                                tObjType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                                .Where(p => (p.FieldType.IsPrimitive || p.FieldType.IsValueType || p.FieldType == typeof(string)) &&
                                !p.CustomAttributes.Any(c => c.AttributeType == typeof(NotSelectAttribute)))).ToArray();

                            // Filling properties
                            if (fieldsAndProps != null && fieldsAndProps.Length > 0)
                            {
                                foreach (MemberInfo fieldOrProp in fieldsAndProps)
                                {
                                    string columnName = fieldOrProp.Name;
                                    ColumnAttribute? columnAttribute = (ColumnAttribute?)fieldOrProp
                                        .GetCustomAttributes(false).FirstOrDefault(a => a.GetType() == typeof(ColumnAttribute));
                                    if (columnAttribute != null && !string.IsNullOrEmpty(columnAttribute.ColumnName))
                                    {
                                        columnName = columnAttribute.ColumnName;
                                    }

                                    object? readValue = null;
                                    try
                                    {
                                        readValue = reader[columnName];
                                    }
                                    catch (Exception)
                                    {
                                        continue;
                                    }

                                    SetPropertyOrField(tObj, fieldOrProp, readValue);
                                }

                                result.Add(tObj);
                            }
                        }
                    }
                }
            }

            return result;
        }
        #endregion

        #region GetListAsync<T>(sql, parameters)

        /// <summary>
        /// Returns array of objects from query result.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="sql">SQL query.</param>
        /// <param name="parameters">Object with parameters. Parameter name is object key, parameter value is object value.</param>
        /// <returns>Array of objects.</returns>
        public async Task<List<T>> GetListAsync<T>(string sql, object parameters)
            where T : class, new()
            =>
            await GetListAsync<T>(sql, false, parameters);
        #endregion

        #region GetListAsync<T>(sql, isStoredProcedure)

        /// <summary>
        /// Returns array of objects from query result.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="sql">SQL query.</param>
        /// <param name="isStoredProcedure">Whether the query is a stored procedure.</param>
        /// <returns>Array of objects.</returns>
        public async Task<List<T>> GetListAsync<T>(string sql, bool isStoredProcedure)
            where T : class, new()
            =>
            await GetListAsync<T>(sql, isStoredProcedure, null);
        #endregion

        #region GetListAsync<T>(sql)

        /// <summary>
        /// Returns array of objects from query result.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="sql">SQL query.</param>
        /// <returns>Array of objects.</returns>
        public async Task<List<T>> GetListAsync<T>(string sql)
            where T : class, new()
            =>
            await GetListAsync<T>(sql, false, null);
        #endregion

        #region GetDynamicListAsync(sql, isStoredProcedure, parameters)

        /// <summary>
        /// Returns array of objects from query result.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="isStoredProcedure">Whether the query is a stored procedure.</param>
        /// <param name="parameters">Object with parameters. Parameter name is object key, parameter value is object value.</param>
        /// <returns>Array of objects.</returns>
        public async Task<List<dynamic>> GetDynamicListAsync(string sql, bool isStoredProcedure, object? parameters)
        {
            List<dynamic> result = new();
            using (SqlCommand cmd = new(sql, Connection))
            {
                cmd.CommandTimeout = TimeOut;
                if (isStoredProcedure)
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                }

                if (parameters != null)
                {
                    PropertyInfo[] parameterProperties = parameters.GetType().GetProperties();
                    if (parameterProperties != null)
                    {
                        foreach (PropertyInfo parameter in parameterProperties)
                        {
                            object? parameterValue = parameter.GetValue(parameters);
                            cmd.Parameters.AddWithValue(parameter.Name, parameterValue ?? DBNull.Value);
                        }
                    }
                }

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        result = ConvertReaderToDynamicArray(reader).ToList();
                    }
                }
            }

            return result;
        }
        #endregion

        #region GetDynamicListAsync(sql, parameters)

        /// <summary>
        /// Returns array of objects from query result.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="parameters">Object with parameters. Parameter name is object key, parameter value is object value.</param>
        /// <returns>Array of objects.</returns>
        public async Task<List<dynamic>> GetDynamicListAsync(string sql, object parameters) =>
            await GetDynamicListAsync(sql, false, parameters);
        #endregion

        #region GetDynamicListAsync(sql, isStoredProcedure)

        /// <summary>
        /// Returns array of objects from query result.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="isStoredProcedure">Whether the query is a stored procedure.</param>
        /// <returns>Array of objects.</returns>
        public async Task<List<dynamic>> GetDynamicListAsync(string sql, bool isStoredProcedure) =>
            await GetDynamicListAsync(sql, isStoredProcedure, null);
        #endregion

        #region GetDynamicListAsync(sql)

        /// <summary>
        /// Returns array of objects from query result.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <returns>Array of objects.</returns>
        public async Task<List<dynamic>> GetDynamicListAsync(string sql) =>
            await GetDynamicListAsync(sql, false, null);
        #endregion

        #region GetMultipleDynamicListAsync(sql, isStoredProcedure, parameters)

        /// <summary>
        /// Query result with multiple selections.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="isStoredProcedure">Whether the query is a stored procedure.</param>
        /// <param name="parameters">Object with parameters. Parameter name is object key, parameter value is object value.</param>
        /// <returns>Multiple lists.</returns>
        public async Task<List<List<dynamic>>> GetMultipleDynamicListAsync(string sql, bool isStoredProcedure, object? parameters)
        {
            List<List<dynamic>> result = new();
            using (SqlCommand cmd = new(sql, Connection))
            {
                cmd.CommandTimeout = TimeOut;
                if (isStoredProcedure)
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                }

                if (parameters != null)
                {
                    PropertyInfo[] parameterProperties = parameters.GetType().GetProperties();
                    if (parameterProperties != null)
                    {
                        foreach (PropertyInfo parameter in parameterProperties)
                        {
                            object? parameterValue = parameter.GetValue(parameters);
                            cmd.Parameters.AddWithValue(parameter.Name, parameterValue ?? DBNull.Value);
                        }
                    }
                }

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    do
                    {
                        if (reader.HasRows)
                        {
                            result.Add(ConvertReaderToDynamicArray(reader).ToList());
                        }
                        else
                        {
                            result.Add(new List<dynamic>());
                        }
                    }
                    while (reader.NextResult());
                }
            }

            return result;
        }
        #endregion

        #region GetMultipleDynamicListAsync(sql, parameters)

        /// <summary>
        /// Query result with multiple selections.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="parameters">Object with parameters. Parameter name is object key, parameter value is object value.</param>
        /// <returns>Multiple lists.</returns>
        public async Task<List<List<dynamic>>> GetMultipleDynamicListAsync(string sql, object parameters) =>
            await GetMultipleDynamicListAsync(sql, false, parameters);
        #endregion

        #region GetMultipleDynamicListAsync(sql, isStoredProcedure)

        /// <summary>
        /// Query result with multiple selections.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="isStoredProcedure">Whether the query is a stored procedure.</param>
        /// <returns>Multiple lists.</returns>
        public async Task<List<List<dynamic>>> GetMultipleDynamicListAsync(string sql, bool isStoredProcedure) =>
            await GetMultipleDynamicListAsync(sql, isStoredProcedure, null);
        #endregion

        #region GetMultipleDynamicList(sql)

        /// <summary>
        /// Query result with multiple selections.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <returns>Multiple lists.</returns>
        public async Task<List<List<dynamic>>> GetMultipleDynamicListAsync(string sql) =>
            await GetMultipleDynamicListAsync(sql, false, null);
        #endregion

        #region GetAsync<T>(sql, isStoredProcedure, parameters)

        /// <summary>
        /// Returns T object from query result.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="sql">SQL query.</param>
        /// <param name="isStoredProcedure">Whether the query is a stored procedure.</param>
        /// <param name="parameters">Object with parameters. Parameter name is object key, parameter value is object value.</param>
        /// <returns>T object.</returns>
        public async Task<T?> GetAsync<T>(string sql, bool isStoredProcedure, object? parameters)
            where T : class, new()
        {
            T result = new();
            using (SqlCommand cmd = new(sql, Connection))
            {
                cmd.CommandTimeout = TimeOut;
                if (isStoredProcedure)
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                }

                if (parameters != null)
                {
                    PropertyInfo[] parameterProperties = parameters.GetType().GetProperties();
                    if (parameterProperties != null)
                    {
                        foreach (PropertyInfo parameter in parameterProperties)
                        {
                            object? parameterValue = parameter.GetValue(parameters);
                            cmd.Parameters.AddWithValue(parameter.Name, parameterValue ?? DBNull.Value);
                        }
                    }
                }

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        reader.Read();

                        // Getting all primitive and don't marked DoNotSelect attribute fields and properties
                        Type tObjType = result.GetType();
                        MemberInfo[] fieldsAndProps = tObjType.GetProperties()
                            .Where(p => (
                            p.PropertyType.IsPrimitive ||
                            p.PropertyType.IsValueType ||
                            p.PropertyType == typeof(string)) &&
                            !p.CustomAttributes.Any(c => c.AttributeType == typeof(NotSelectAttribute))).ToArray();

                        fieldsAndProps = fieldsAndProps.Concat(
                            tObjType
                            .GetFields(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p =>
                            (p.FieldType.IsPrimitive ||
                            p.FieldType.IsValueType ||
                            p.FieldType == typeof(string)) &&
                            !p.CustomAttributes.Any(c => c.AttributeType == typeof(NotSelectAttribute))))
                            .ToArray();

                        if (fieldsAndProps != null && fieldsAndProps.Length > 0)
                        {
                            foreach (MemberInfo fieldOrProp in fieldsAndProps)
                            {
                                string columnName = fieldOrProp.Name;
                                ColumnAttribute? columnAttribute = (ColumnAttribute?)fieldOrProp.GetCustomAttributes(false)
                                    .FirstOrDefault(a => a.GetType() == typeof(ColumnAttribute));

                                if (columnAttribute != null && !string.IsNullOrEmpty(columnAttribute.ColumnName))
                                {
                                    columnName = columnAttribute.ColumnName;
                                }

                                object? readValue = null;
                                try
                                {
                                    readValue = reader[columnName];
                                }
                                catch (Exception)
                                {
                                    continue;
                                }

                                SetPropertyOrField(result, fieldOrProp, readValue);
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }

            return result;
        }
        #endregion

        #region GetAsync<T>(sql, parameters)

        /// <summary>
        /// Returns T object from query result.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="sql">SQL query.</param>
        /// <param name="parameters">Object with parameters. Parameter name is object key, parameter value is object value.</param>
        /// <returns>T object.</returns>
        public async Task<T?> GetAsync<T>(string sql, object parameters)
            where T : class, new()
            =>
            await GetAsync<T>(sql, false, parameters);
        #endregion

        #region GetAsync<T>(sql, isStoredProcedure)

        /// <summary>
        /// Returns T object from query result.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="sql">SQL query.</param>
        /// <param name="isStoredProcedure">Whether the query is a stored procedure.</param>
        /// <returns>T object.</returns>
        public async Task<T?> GetAsync<T>(string sql, bool isStoredProcedure)
            where T : class, new()
            =>
            await GetAsync<T>(sql, isStoredProcedure, null);
        #endregion

        #region GetAsync<T>(sql)

        /// <summary>
        /// Returns T object from query result.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="sql">SQL query.</param>
        /// <returns>T object.</returns>
        public async Task<T?> GetAsync<T>(string sql)
            where T : class, new()
            =>
            await GetAsync<T>(sql, false, null);
        #endregion

        #region GetDynamicAsync(sql, isStoredProcedure, parameters)

        /// <summary>
        /// Returns dynamic object of first row.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="isStoredProcedure">Whether the query is a stored procedure.</param>
        /// <param name="parameters">Object with parameters. Parameter name is object key, parameter value is object value.</param>
        /// <returns>Dynamic object.</returns>
        public async Task<dynamic?> GetDynamicAsync(string sql, bool isStoredProcedure, object? parameters)
        {
            dynamic? result = null;
            using (SqlCommand cmd = new(sql, Connection))
            {
                cmd.CommandTimeout = TimeOut;
                if (isStoredProcedure)
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                }

                if (parameters != null)
                {
                    PropertyInfo[] parameterProperties = parameters.GetType().GetProperties();
                    if (parameterProperties != null)
                    {
                        foreach (PropertyInfo parameter in parameterProperties)
                        {
                            object? parameterValue = parameter.GetValue(parameters);
                            cmd.Parameters.AddWithValue(parameter.Name, parameterValue ?? DBNull.Value);
                        }
                    }
                }

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        result = ConvertReaderToDynamic(reader).ToList();
                    }
                }
            }

            return result;
        }
        #endregion

        #region GetDynamicAsync(sql, parameters)

        /// <summary>
        /// Returns dynamic object of first row.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="parameters">Object with parameters. Parameter name is object key, parameter value is object value.</param>
        /// <returns>Dynamic object.</returns>
        public async Task<dynamic?> GetDynamicAsync(string sql, object? parameters) =>
            await GetDynamicAsync(sql, false, parameters);
        #endregion

        #region GetDynamicAsync(sql, isStoredProcedure)

        /// <summary>
        /// Returns dynamic object of first row.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="isStoredProcedure">Whether the query is a stored procedure.</param>
        /// <returns>Dynamic object.</returns>
        public async Task<dynamic?> GetDynamicAsync(string sql, bool isStoredProcedure) =>
            await GetDynamicAsync(sql, isStoredProcedure, null);
        #endregion

        #region GetDynamicAsync(sql)

        /// <summary>
        /// Returns dynamic object of first row.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <returns>Dynamic object.</returns>
        public async Task<dynamic?> GetDynamicAsync(string sql) =>
            await GetDynamicAsync(sql, false, null);
        #endregion

        #region GetScalarAsync(sql, isStoredProcedure, parameters)

        /// <summary>
        /// Returns scalar value.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="isStoredProcedure">Whether the query is a stored procedure.</param>
        /// <param name="parameters">Object with parameters. Parameter name is object key, parameter value is object value.</param>
        /// <returns>Scalar value.</returns>
        public async Task<object?> GetScalarAsync(string sql, bool isStoredProcedure, object? parameters)
        {
            object? result = null;
            using (SqlCommand cmd = new(sql, Connection))
            {
                cmd.CommandTimeout = TimeOut;
                if (isStoredProcedure)
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                }

                if (parameters != null)
                {
                    PropertyInfo[] parameterProperties = parameters.GetType().GetProperties();
                    if (parameterProperties != null)
                    {
                        foreach (PropertyInfo parameter in parameterProperties)
                        {
                            object? parameterValue = parameter.GetValue(parameters);
                            cmd.Parameters.AddWithValue(parameter.Name, parameterValue ?? DBNull.Value);
                        }
                    }
                }

                result = await cmd.ExecuteScalarAsync();
            }

            return result;
        }
        #endregion

        #region GetScalarAsync(sql, parameters)

        /// <summary>
        /// Returns scalar value.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="parameters">Object with parameters. Parameter name is object key, parameter value is object value.</param>
        /// <returns>Scalar value.</returns>
        public async Task<object?> GetScalarAsync(string sql, object parameters) => await GetScalarAsync(sql, false, parameters);
        #endregion

        #region GetScalarAsync(sql, isStoredProcedure)

        /// <summary>
        /// Returns scalar value.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="isStoredProcedure">Whether the query is a stored procedure.</param>
        /// <returns>Scalar value.</returns>
        public async Task<object?> GetScalarAsync(string sql, bool isStoredProcedure) => await GetScalarAsync(sql, isStoredProcedure, null);
        #endregion

        #region GetScalarAsync(sql)

        /// <summary>
        /// Returns scalar value.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <returns>Scalar value.</returns>
        public async Task<object?> GetScalarAsync(string sql) => await GetScalarAsync(sql, false, null);
        #endregion

        #region ExecuteNonQueryAsync(sql, isStoredProcedure, parameters)

        /// <summary>
        /// Executes script.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="isStoredProcedure">Whether the query is a stored procedure.</param>
        /// <param name="parameters">Object with parameters. Parameter name is object key, parameter value is object value.</param>
        /// <returns>Integer status.</returns>
        public async Task<int> ExecuteNonQueryAsync(string sql, bool isStoredProcedure, object? parameters)
        {
            int result = 0;
            using (SqlCommand cmd = new(sql, Connection))
            {
                cmd.CommandTimeout = TimeOut;
                if (isStoredProcedure)
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                }

                if (parameters != null)
                {
                    PropertyInfo[] parameterProperties = parameters.GetType().GetProperties();
                    if (parameterProperties != null)
                    {
                        foreach (PropertyInfo parameter in parameterProperties)
                        {
                            object? parameterValue = parameter.GetValue(parameters);
                            cmd.Parameters.AddWithValue(parameter.Name, parameterValue ?? DBNull.Value);
                        }
                    }
                }

                result = await cmd.ExecuteNonQueryAsync();
            }

            return result;
        }
        #endregion

        #region ExecuteNonQueryAsync(sql, parameters)

        /// <summary>
        /// Executes script.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="parameters">Object with parameters. Parameter name is object key, parameter value is object value.</param>
        /// <returns>Integer status.</returns>
        public async Task<int> ExecuteNonQueryAsync(string sql, object parameters) => await ExecuteNonQueryAsync(sql, false, parameters);
        #endregion

        #region ExecuteNonQueryAsync(sql, isStoredProcedure)

        /// <summary>
        /// Executes script.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="isStoredProcedure">Whether the query is a stored procedure.</param>
        /// <returns>Integer status.</returns>
        public async Task<int> ExecuteNonQueryAsync(string sql, bool isStoredProcedure) => await ExecuteNonQueryAsync(sql, isStoredProcedure, null);
        #endregion

        #region ExecuteNonQueryAsync(sql)

        /// <summary>
        /// Executes script.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <returns>Integer status.</returns>
        public async Task<int> ExecuteNonQueryAsync(string sql) => await ExecuteNonQueryAsync(sql, false, null);
        #endregion

        #region GetListAutoAsync(from, take, orderType)

        /// <summary>
        /// Generates a query and populates the model. Makes a selection in a certain range of rows, if needed.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="from">From which line to take the data. If NULL, taken from the first line.</param>
        /// <param name="take">How many lines to take. If NULL, all rows are taken.</param>
        /// <param name="orderType">Sort type.</param>
        /// <returns>Array of objects.</returns>
        public async Task<List<T>> GetListAutoAsync<T>(uint? from, uint? take, OrderTypes orderType)
            where T : class, new()
        {
            string sql = GenerateSQLForSelect<T>(from, take, orderType);
            return await GetListAsync<T>(sql);
        }
        #endregion

        #region GetListAutoAsync()

        /// <summary>
        /// Generates a query and populates the model. Makes a selection in a certain range of rows, if needed.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <returns>Array of objects.</returns>
        public async Task<List<T>> GetListAutoAsync<T>()
            where T : class, new()
        {
            string sql = GenerateSQLForSelect<T>(null, null, OrderTypes.NONE);
            return await GetListAsync<T>(sql);
        }
        #endregion

        #region InsertModelAsync(model, getIdentity)

        /// <summary>
        /// Adds an object to the database.
        /// </summary>
        /// <typeparam name="T">Type of object.</typeparam>
        /// <param name="model">Data to insert.</param>
        /// <param name="getIdentity">Вернуть Identity или вернуть результат ExecuteNonQuery.</param>
        /// <returns>Integer status.</returns>
        public async Task<int> InsertModelAsync(object model, bool getIdentity)
        {
            string sql = string.Empty;
            string SQLFirstPart = string.Empty;
            string SQLSecondPart = string.Empty;
            Type modelType = model.GetType();
            TableAttribute? tableAttribute = modelType.GetCustomAttribute<TableAttribute>();
            if (tableAttribute != null || string.IsNullOrEmpty(tableAttribute?.TableName))
            {
                throw new DBManagerException($"У типа {modelType.FullName} должен стоять атрибут [Table]");
            }

            SQLFirstPart += $"INSERT INTO {tableAttribute.TableName} (";
            SQLSecondPart += $"VALUES (";
            MemberInfo[] fieldsAndProps = modelType
                .GetProperties()
                .Where(p =>
                (p.PropertyType.IsPrimitive ||
                p.PropertyType.IsValueType ||
                p.PropertyType == typeof(string)) &&
                !p.CustomAttributes.Any(c => c.AttributeType == typeof(NotInsertAttribute)))
                .ToArray();
            fieldsAndProps = fieldsAndProps.Concat(modelType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(p =>
                (p.FieldType.IsPrimitive ||
                p.FieldType.IsValueType ||
                p.FieldType == typeof(string)) &&
                !p.CustomAttributes.Any(c => c.AttributeType == typeof(NotInsertAttribute)))).ToArray();
            Dictionary<string, object> parameters = new();
            for (int i = 0; i < fieldsAndProps.Length; i++)
            {
                string columnName = fieldsAndProps[i].Name;
                ColumnAttribute? columnAttribute = fieldsAndProps[i].GetCustomAttribute<ColumnAttribute>();
                if (columnAttribute != null)
                {
                    columnName = columnAttribute.ColumnName;
                }

                SQLFirstPart += columnName;
                SQLFirstPart += i == fieldsAndProps.Length - 1 ? ") " : ", ";
                if (fieldsAndProps[i] is PropertyInfo)
                {
                    object? value = ((PropertyInfo)fieldsAndProps[i]).GetValue(model);
                    if (value != null)
                    {
                        SQLSecondPart += "@parameter" + i;
                        parameters.Add("@parameter" + i, value);
                    }
                    else
                    {
                        SQLSecondPart += i == fieldsAndProps.Length - 1 ? ");" : ", ";
                    }
                }
            }

            sql = SQLFirstPart + SQLSecondPart;
            if (getIdentity)
            {
                sql += "SELECT @@IDENTITY";
            }

            using (SqlCommand cmd = new(sql, Connection))
            {
                cmd.CommandTimeout = TimeOut;
                foreach (KeyValuePair<string, object> item in parameters)
                {
                    cmd.Parameters.AddWithValue(item.Key, item.Value);
                }

                if (getIdentity)
                {
                    return Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                else
                {
                    return await cmd.ExecuteNonQueryAsync();
                }
            }
        }
        #endregion

        #region InsertModelAsync(model)

        /// <summary>
        /// Adds an object to the database.
        /// </summary>
        /// <typeparam name="T">Type of object.</typeparam>
        /// <param name="model">Data to insert.</param>
        /// <returns>Integer status.</returns>
        public async Task<int> InsertModelAsync(object model) => await InsertModelAsync(model, false);
        #endregion

        #region UpdateModelAsync(model)

        /// <summary>
        /// Updates a record in the database by attribute [PrimaryKey].
        /// </summary>
        /// <typeparam name="T">Type of object.</typeparam>
        /// <param name="model">Data to insert.</param>
        /// <returns>Integer status.</returns>
        public async Task<int> UpdateModelAsync(object model)
        {
            string sql = string.Empty;
            Type modelType = model.GetType();
            TableAttribute? tableAttribute = modelType.GetCustomAttribute<TableAttribute>();
            if (tableAttribute != null || string.IsNullOrEmpty(tableAttribute?.TableName))
            {
                throw new DBManagerException($"У типа {modelType.FullName} должен стоять атрибут [Table]");
            }

            sql = $"UPDATE {tableAttribute.TableName} SET ";
            MemberInfo[] fieldsAndProps = modelType
                .GetProperties()
                .Where(p =>
                (p.PropertyType.IsPrimitive ||
                p.PropertyType.IsValueType ||
                p.PropertyType == typeof(string)) &&
                !p.CustomAttributes.Any(c => c.AttributeType == typeof(NotInsertAttribute)))
                .ToArray();
            fieldsAndProps = fieldsAndProps.Concat(
                modelType
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(p =>
                (p.FieldType.IsPrimitive ||
                p.FieldType.IsValueType ||
                p.FieldType == typeof(string)) &&
                !p.CustomAttributes.Any(c => c.AttributeType == typeof(NotInsertAttribute)))).ToArray();
            MemberInfo? primaryKeyColumn = fieldsAndProps.FirstOrDefault(m => m.GetCustomAttribute<PrimaryKeyAttribute>() != null);
            if (primaryKeyColumn == null)
            {
                throw new DBManagerException($"Для обновления типа {modelType.FullName} одно из его свойств должно иметь атрибут [PrimaryKey]");
            }

            string id = primaryKeyColumn.Name;
            ColumnAttribute? primaryKeyColumnAttribute = primaryKeyColumn.GetCustomAttribute<ColumnAttribute>();
            if (primaryKeyColumnAttribute != null)
            {
                id = primaryKeyColumnAttribute.ColumnName;
            }

            fieldsAndProps = fieldsAndProps.Where(m => m.GetCustomAttribute<NotUpdateAttribute>() == null).ToArray();
            Dictionary<string, object> parameters = new();

            for (int i = 0; i < fieldsAndProps.Length; i++)
            {
                string columnName = fieldsAndProps[i].Name;
                ColumnAttribute? columnAttribute = fieldsAndProps[i].GetCustomAttribute<ColumnAttribute>();
                if (columnAttribute != null)
                {
                    columnName = columnAttribute.ColumnName;
                }

                sql += $"{columnName} = ";
                if (fieldsAndProps[i] is PropertyInfo)
                {
                    object? value = ((PropertyInfo)fieldsAndProps[i]).GetValue(model);
                    if (value != null)
                    {
                        sql += "@parameter" + i;
                        parameters.Add("@parameter" + i, value);
                    }
                    else
                    {
                        sql += "NULL";
                    }

                    sql += i == fieldsAndProps.Length - 1 ? " " : ", ";
                }
                else
                {
                    object? value = ((FieldInfo)fieldsAndProps[i]).GetValue(model);
                    if (value != null)
                    {
                        sql += "@parameter" + i;
                        parameters.Add("@parameter" + i, value);
                    }
                    else
                    {
                        sql += "NULL";
                    }

                    sql += i == fieldsAndProps.Length - 1 ? " " : ", ";
                }
            }

            sql += $"WHERE {id} = ";
            if (primaryKeyColumn is PropertyInfo)
            {
                object? value = ((PropertyInfo)primaryKeyColumn).GetValue(model);
                if (value != null)
                {
                    sql += "@id";
                    parameters.Add("@id", value);
                }
                else
                {
                    sql += "NULL";
                }
            }
            else
            {
                object? value = ((FieldInfo)primaryKeyColumn).GetValue(model);
                if (value != null)
                {
                    sql += "@id";
                    parameters.Add("@id", value);
                }
                else
                {
                    sql += "NULL";
                }
            }

            using (SqlCommand cmd = new(sql, Connection))
            {
                cmd.CommandTimeout = TimeOut;
                foreach (KeyValuePair<string, object> item in parameters)
                {
                    cmd.Parameters.AddWithValue(item.Key, item.Value);
                }

                return await cmd.ExecuteNonQueryAsync();
            }
        }
        #endregion

        #region SetModelAsync(model, getIdentityAfterInsert)

        /// <summary>
        /// Updates the record in the database if it exists or adds a new one if it doesn't.
        /// </summary>
        /// <param name="model">Data to insert.</param>
        /// <param name="getIdentityAfterInsert">Return IDENTITY or return the result of ExecuteNonQuery in case of data insertion.</param>
        /// <returns>Integer status.</returns>
        public async Task<int> SetModelAsync(object model, bool getIdentityAfterInsert)
        {
            Type modelType = model.GetType();
            TableAttribute? tableAttribute = modelType.GetCustomAttribute<TableAttribute>();
            if (tableAttribute != null || string.IsNullOrEmpty(tableAttribute?.TableName))
            {
                throw new DBManagerException($"У типа {modelType.FullName} должен стоять атрибут [Table]");
            }

            MemberInfo[] fieldsAndProps = modelType
                .GetProperties()
                .Where(p =>
                (p.PropertyType.IsPrimitive ||
                p.PropertyType.IsValueType ||
                p.PropertyType == typeof(string)))
                .ToArray();
            fieldsAndProps = fieldsAndProps.Concat(
                modelType
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(p =>
                (p.FieldType.IsPrimitive ||
                p.FieldType.IsValueType ||
                p.FieldType == typeof(string))))
                .ToArray();
            MemberInfo? primaryKeyColumn = fieldsAndProps.FirstOrDefault(m => m.GetCustomAttribute<PrimaryKeyAttribute>() != null);
            if (primaryKeyColumn == null)
            {
                throw new DBManagerException($"Для обновления типа {modelType.FullName} одно из его свойств должно иметь атрибут [PrimaryKey]");
            }

            string id = primaryKeyColumn.Name;
            ColumnAttribute? primaryKeyColumnAttribute = primaryKeyColumn.GetCustomAttribute<ColumnAttribute>();
            if (primaryKeyColumnAttribute != null)
            {
                id = primaryKeyColumnAttribute.ColumnName;
            }

            string checkSql = $@"IF EXISTS(SELECT TOP 1 1 FROM {tableAttribute.TableName} WHERE {id} = ";
            Dictionary<string, object> parameters = new();
            if (primaryKeyColumn is PropertyInfo)
            {
                object? value = ((PropertyInfo)primaryKeyColumn).GetValue(model);
                if (value != null)
                {
                    checkSql += "@id";
                    parameters.Add("@id", value);
                }
                else
                {
                    checkSql += "NULL";
                }
            }
            else
            {
                object? value = ((FieldInfo)primaryKeyColumn).GetValue(model);
                if (value != null)
                {
                    checkSql += "@id";
                    parameters.Add("@id", value);
                }
                else
                {
                    checkSql += "NULL";
                }
            }

            checkSql += ") SELECT 1 ELSE SELECT 0";
            bool hasRecords = false;
            using (SqlCommand cmd = new(checkSql, Connection))
            {
                cmd.CommandTimeout = TimeOut;
                foreach (KeyValuePair<string, object> item in parameters)
                {
                    cmd.Parameters.AddWithValue(item.Key, item.Value);
                }

                hasRecords = Convert.ToBoolean(await cmd.ExecuteScalarAsync());
            }

            if (hasRecords)
            {
                return await UpdateModelAsync(model);
            }
            else
            {
                return await InsertModelAsync(model, getIdentityAfterInsert);
            }
        }
        #endregion

        #region SetModelAsync(model)

        /// <summary>
        /// Updates the record in the database if it exists or adds a new one if it doesn't.
        /// </summary>
        /// <param name="model">Data to insert.</param>
        /// <returns>Integer status.</returns>
        public async Task<int> SetModelAsync(object model) => await SetModelAsync(model, false);
        #endregion
    }
}
