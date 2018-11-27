using Api4GP.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Api4GP.SqlServer
{

    /// <summary>
    /// Api manager for sql server
    /// </summary>
    public class SqlServerApiManager : IApiManager
    {


        /// <summary>
        /// Creates a new instance of <see cref="SqlServerApiManager"/>
        /// </summary>
        /// <param name="connectionString">Connection string for access the database</param>
        /// <param name="routeRoot">Route to map on the request URL (default = sql)</param>
        /// <param name="dbSchema">Schema of the database to use (default = dbo)</param>
        public SqlServerApiManager(string connectionString, string routeRoot = "sql", string dbSchema = "dbo")
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            RouteRoot = routeRoot ?? throw new ArgumentNullException(nameof(routeRoot));
            DbSchema = dbSchema ?? throw new ArgumentNullException(nameof(dbSchema));
        }

        /// <summary>
        /// Connection string
        /// </summary>
        private string ConnectionString { get; }


        /// <summary>
        /// Route root
        /// </summary>
        private string RouteRoot { get; }
        

        /// <summary>
        /// Database schema
        /// </summary>
        private string DbSchema { get; set; }


        /// <summary>
        /// Executes the work
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns>Response</returns>
        public async Task<ApiResponse> ExecuteRequestAsync(ApiRequest request)
        {
            // procedure name
            var procedure = request.Paths[1];
            var fullProcedureName = $"{DbSchema}.{procedure}";

            // check for procedures
            var dbProcedures = await GetStoredProcedureAsync();

            // check if procedure exists (full name)
            var dbProc = dbProcedures.SingleOrDefault(x => x.ProcedureName.Equals(procedure, StringComparison.InvariantCultureIgnoreCase));
            // check if procedure exists (alias)
            if (dbProc == null)
            {
                dbProc = dbProcedures.SingleOrDefault(x => x.ProcedureAlias.Equals(procedure, StringComparison.InvariantCultureIgnoreCase));
            }

            // if not, not found response
            if (dbProc == null)
            {
                return new ApiResponse
                {
                    StatusCode = 400,
                    Content = $"Procedure '{fullProcedureName}' not found"
                };
            }

            // Checks if the procedure is valid
            var checkResult = ValidateProcedure(dbProc);
            if (checkResult != null) return checkResult;

            
            // Open connection
            using (var connection = new SqlConnection(ConnectionString))
            {
                await connection.OpenAsync();

                // Command for execute the procedure
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = dbProc.ProcedureName;
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Adds the parameter (if any)
                    if (dbProc.Parameters.Count == 1)
                    {
                        // from content
                        var paramText = await request.GetContentAsync();

                        // from query string
                        if (string.IsNullOrEmpty(paramText) &&
                            request.ActualRequest.Query.Count == 1)
                        {
                            paramText = request.ActualRequest.Query.First().Value;
                        }
                        // empty json
                        if (string.IsNullOrEmpty(paramText))
                        {
                            paramText = "{}";
                        }
                        // adds parameter
                        cmd.Parameters.Add(new SqlParameter(dbProc.Parameters[0].ParamName, paramText));
                    }

                    // Execute the procedure
                    //var responseContent = (await cmd.ExecuteScalarAsync()) as string;
                    int statusCode = 200;
                    string content = null;
                    try
                    {
                        using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
                        {
                            if (await reader.ReadAsync())
                            {
                                statusCode = reader.GetInt32(0);
                                if (!await reader.IsDBNullAsync(1))
                                {
                                    content = reader.GetString(1);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // On exception return 500
                        return new ApiResponse
                        {
                            StatusCode = 500,
                            Content = $"Error executing the '{fullProcedureName}' procedure.{Environment.NewLine}" +
                                      $"It has to return two parameters: Status code (int) and JSON result (string or null).{Environment.NewLine}" +
                                      $"Error message: {ex.Message}"
                        };
                    }

                    // Returns the response
                    return new ApiResponse
                    {
                        StatusCode = statusCode,
                        Content = content
                    };

                }
            }
        }


        /// <summary>
        /// True if the request is managed by this manager
        /// </summary>
        /// <param name="request">Request to check</param>
        /// <returns>True if the request is managed, false elsewhere</returns>
        public bool IsManaged(ApiRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // First path has to match the route root
            var path = request.Paths[0];
            if (!string.IsNullOrEmpty(path) &&
                path.Equals(RouteRoot, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            // Else not managed
            return false;
        }



        #region Stored procedures



        /// <summary>
        /// Validates the procedure
        /// </summary>
        /// <param name="pInfo">Procedure to validate</param>
        /// <returns>ApiResponse for invalid check, null if anything is ok</returns>
        private ApiResponse ValidateProcedure(StoredProcedureInfo pInfo)
        {
            var isOk = true;

            // It has to have 0 or 1 string parameter input
            switch (pInfo.Parameters.Count)
            {
                case 0:
                    break;
                case 1:
                    if (pInfo.Parameters[0].SqlDbType != SqlDbType.NVarChar)
                    {
                        isOk = false;
                    }
                    break;
                default:
                    isOk = false;
                    break;
            }

            // If ok returns null
            if (isOk)
            {
                return null;
            }

            // return bad request
            return new ApiResponse
            {
                StatusCode = 400,
                Content = $"Procedure '{pInfo.ProcedureName}' must have at max a single NVARCHAR input parameter"
            };
        }


        /// <summary>
        /// Info of the stored procedure
        /// </summary>
        private class StoredProcedureInfo
        {
            /// <summary>
            /// Name of the procedure
            /// </summary>
            public string ProcedureName { get; set; }


            private string _procedureAlias { get; set; }

            /// <summary>
            /// Procedure alias (without underscore)
            /// </summary>
            public string ProcedureAlias
            {
                get
                {
                    if (_procedureAlias == null)
                    {
                        _procedureAlias = ProcedureName.Replace("_", "");
                    }
                    return _procedureAlias;
                }
            }

            /// <summary>
            /// List of parameters
            /// </summary>
            public List<StoredProcedureParameterInfo> Parameters { get; } = new List<StoredProcedureParameterInfo>();

        }


        /// <summary>
        /// Stored procedure parameter info
        /// </summary>
        private class StoredProcedureParameterInfo
        {

            /// <summary>
            /// Name of the param
            /// </summary>
            public string ParamName { get; set; }

            /// <summary>
            /// Type of the param
            /// </summary>
            public SqlDbType SqlDbType { get; set; }
        }



        private static List<StoredProcedureInfo> _storedProcedures = null;



        /// <summary>
        /// Loads stored procedures for the given connection
        /// </summary>
        private async Task<List<StoredProcedureInfo>> GetStoredProcedureAsync()
        {
            // If loaded, uses the cache
            if (_storedProcedures == null)
            {

                using (var connection = new SqlConnection(ConnectionString))
                {
                    // Open connection
                    await connection.OpenAsync();

                    // Read all procedures
                    var proceduresTable = connection.GetSchema(SqlClientMetaDataCollectionNames.Procedures);

                    _storedProcedures = new List<StoredProcedureInfo>();

                    // Iterate thru them
                    foreach (var procedureRow in proceduresTable.Rows)
                    {
                        var columns = ((DataRow)procedureRow).ItemArray;
                        // Check for schema
                        if (columns[1].Equals(DbSchema))
                        {
                            var procedure = new StoredProcedureInfo
                            {
                                ProcedureName = columns[2].ToString()
                            };

                            try
                            {
                                // Check for parameters
                                using (var cmd = connection.CreateCommand())
                                {
                                    cmd.CommandText = $"{DbSchema}.{procedure.ProcedureName}";
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    SqlCommandBuilder.DeriveParameters(cmd);
                                    foreach (SqlParameter parameter in cmd.Parameters)
                                    {
                                        // Add parameter
                                        if (parameter.Direction != ParameterDirection.ReturnValue)
                                        {
                                            procedure.Parameters.Add(new StoredProcedureParameterInfo
                                            {
                                                ParamName = parameter.ParameterName,
                                                SqlDbType = parameter.SqlDbType
                                            });
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                var x = ex.Message;
                            }

                            // Add the new procedure
                            _storedProcedures.Add(procedure);
                        }
                    }
                }
            }
            return _storedProcedures;
        }

        

        #endregion


    }
}
