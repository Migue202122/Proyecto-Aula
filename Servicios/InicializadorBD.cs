// // Servicios/InicializadorBD.cs
// using ProyectoAula.Servicios.Abstracciones;
// using Microsoft.Data.SqlClient;
// using Npgsql;
// using MySqlConnector;
// using System.Data.Common;

// namespace ProyectoAula.Servicios
// {
//     public class InicializadorBD : IInicializadorBD
//     {
//         private readonly IConfiguration _configuration;
//         private readonly IProveedorConexion _proveedorConexion;
//         private readonly ILogger<InicializadorBD> _logger;

//         public InicializadorBD(
//             IConfiguration configuration,
//             IProveedorConexion proveedorConexion,
//             ILogger<InicializadorBD> logger)
//         {
//             _configuration = configuration;
//             _proveedorConexion = proveedorConexion;
//             _logger = logger;
//         }

//         public async Task InicializarAsync()
//         {
//             var proveedor = _configuration.GetValue<string>("DatabaseProvider")?.ToLower();
//             var connString = _proveedorConexion.ObtenerCadenaConexion(); // Cadena con la BD específica
//             var masterConnString = ObtenerCadenaMaestra(proveedor, connString);
//             var dbName = ObtenerNombreBD(connString, proveedor);

//             if (!string.IsNullOrEmpty(masterConnString) && !string.IsNullOrEmpty(dbName))
//             {
//                 await CrearBaseDeDatosSiNoExiste(proveedor, masterConnString, dbName);
//             }

//             await CrearTablasSiNoExisten(proveedor, connString);
//             await CrearUsuarioPorDefectoSiNoExiste(proveedor, connString);
//         }

//         private string? ObtenerCadenaMaestra(string? proveedor, string connString)
//         {
//             if (proveedor?.Contains("sqlserver") == true || proveedor == "localdb")
//             {
//                 var builder = new SqlConnectionStringBuilder(connString);
//                 builder.InitialCatalog = "master";
//                 return builder.ConnectionString;
//             }
//             else if (proveedor == "postgres")
//             {
//                 var builder = new NpgsqlConnectionStringBuilder(connString);
//                 builder.Database = "postgres";
//                 return builder.ConnectionString;
//             }
//             else if (proveedor == "mysql" || proveedor == "mariadb")
//             {
//                 var builder = new MySqlConnectionStringBuilder(connString);
//                 builder.Database = "mysql";
//                 return builder.ConnectionString;
//             }
//             return null;
//         }

//         private string? ObtenerNombreBD(string connString, string? proveedor)
//         {
//             if (proveedor?.Contains("sqlserver") == true || proveedor == "localdb")
//                 return new SqlConnectionStringBuilder(connString).InitialCatalog;
//             else if (proveedor == "postgres")
//                 return new NpgsqlConnectionStringBuilder(connString).Database;
//             else if (proveedor == "mysql" || proveedor == "mariadb")
//                 return new MySqlConnectionStringBuilder(connString).Database;
//             return null;
//         }

//         private async Task CrearBaseDeDatosSiNoExiste(string? proveedor, string masterConnString, string dbName)
//         {
//             if (string.IsNullOrEmpty(dbName)) return; // Validación temprana
//             try
//             {
//                 if (proveedor?.Contains("sqlserver") == true || proveedor == "localdb")
//                 {
//                     await using var conn = new SqlConnection(masterConnString);
//                     await conn.OpenAsync();
//                     var cmd = new SqlCommand($"SELECT COUNT(*) FROM sys.databases WHERE name = @dbName", conn);
//                     cmd.Parameters.AddWithValue("@dbName", dbName);
//                     var result = await cmd.ExecuteScalarAsync();
//                     int exists = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;
//                     if (exists == 0)
//                     {
//                         await new SqlCommand($"CREATE DATABASE [{dbName}]", conn).ExecuteNonQueryAsync();
//                         _logger.LogInformation("Base de datos '{DbName}' creada.", dbName);
//                     }
//                 }
//                 else if (proveedor == "postgres")
//                 {
//                     await using var conn = new NpgsqlConnection(masterConnString);
//                     await conn.OpenAsync();
//                     var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM pg_database WHERE datname = @dbName", conn);
//                     cmd.Parameters.AddWithValue("@dbName", dbName);
//                     var result = await cmd.ExecuteScalarAsync();
//                     long exists = (result != null && result != DBNull.Value) ? Convert.ToInt64(result) : 0;
//                     if (exists == 0)
//                     {
//                         await new NpgsqlCommand($"CREATE DATABASE \"{dbName}\"", conn).ExecuteNonQueryAsync();
//                         _logger.LogInformation("Base de datos '{DbName}' creada.", dbName);
//                     }
//                 }
//                 else if (proveedor == "mysql" || proveedor == "mariadb")
//                 {
//                     await using var conn = new MySqlConnection(masterConnString);
//                     await conn.OpenAsync();
//                     var cmd = new MySqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = @dbName", conn);
//                     cmd.Parameters.AddWithValue("@dbName", dbName);
//                     var result = await cmd.ExecuteScalarAsync();
//                     long exists = (result != null && result != DBNull.Value) ? Convert.ToInt64(result) : 0;
//                     if (exists == 0)
//                     {
//                         await new MySqlCommand($"CREATE DATABASE `{dbName}`", conn).ExecuteNonQueryAsync();
//                         _logger.LogInformation("Base de datos '{DbName}' creada.", dbName);
//                     }
//                 }
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogWarning(ex, "No se pudo crear la base de datos '{DbName}'. Probablemente ya existe o no hay permisos.", dbName);
//             }
//         }

//         private async Task CrearTablasSiNoExisten(string? proveedor, string connString)
//         {
//             // Aquí leerás y ejecutarás los scripts SQL para crear las tablas.
//             // Puedes tener un archivo .sql embebido o cadenas SQL en código.
//             // Por simplicidad, usaremos cadenas SQL directas para las tablas necesarias.

//             string[] scripts = ObtenerScriptsPorProveedor(proveedor);

//             DbConnection connection = proveedor?.Contains("sqlserver") == true ? new SqlConnection(connString) :
//                                         proveedor == "postgres" ? new NpgsqlConnection(connString) :
//                                             new MySqlConnection(connString);

//             await using (connection)
//             {
//                 await connection.OpenAsync();
//                 foreach (var script in scripts)
//                 {
//                     try
//                     {
//                         using var cmd = connection.CreateCommand();
//                         cmd.CommandText = script;
//                         await cmd.ExecuteNonQueryAsync();
//                     }
//                     catch (Exception ex)
//                     {
//                         _logger.LogWarning(ex, "Error al ejecutar script (probablemente la tabla ya existe). Continuando...");
//                     }
//                 }
//             }
//         }

//         private string[] ObtenerScriptsPorProveedor(string? proveedor)
//         {
//             // Define aquí las tablas que necesitas. Ejemplo:
//             var scripts = new List<string>();

//             // Tabla de usuarios (para autenticación)
//             if (proveedor?.Contains("sqlserver") == true || proveedor == "localdb")
//             {
//                 scripts.Add(@"
//                     IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='usuarios' AND xtype='U')
//                     CREATE TABLE usuarios (
//                         id INT IDENTITY(1,1) PRIMARY KEY,
//                         email NVARCHAR(100) NOT NULL UNIQUE,
//                         password_hash NVARCHAR(255) NOT NULL,
//                         nombre NVARCHAR(100),
//                         fecha_registro DATETIME DEFAULT GETDATE()
//                     );                   
                    
//                     IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='termino_clave' AND xtype='U')
//                     CREATE TABLE termino_clave (
//                         termino VARCHAR(30) PRIMARY KEY,
//                         termino_ingles VARCHAR(30)
//                     );

//                     IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='proyecto' AND xtype='U')
//                     CREATE TABLE proyecto (
//                         id INT PRIMARY KEY IDENTITY(1,1),
//                         titulo VARCHAR(70),
//                         resumen VARCHAR(256),
//                         presupuesto FLOAT,
//                         tipo_financiacion VARCHAR(45),
//                         tipo_fondos VARCHAR(45),
//                         fecha_inicio DATE,
//                         fecha_fin DATE
//                     );

//                     IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='tipo_producto' AND xtype='U')
//                     CREATE TABLE tipo_producto (
//                         id INT PRIMARY KEY IDENTITY(1,1),
//                         categoria VARCHAR(45),
//                         clase VARCHAR(45),
//                         nombre VARCHAR(45),
//                         topologia VARCHAR(45)
//                     );
//                     ");
//                 // Agrega aquí las otras tablas de tu módulo (ej. productos, categorias, etc.)
//             }
//             else if (proveedor == "postgres")
//             {
//                 scripts.Add(@"
//                     CREATE TABLE IF NOT EXISTS usuarios (
//                         id SERIAL PRIMARY KEY,
//                         email VARCHAR(100) NOT NULL UNIQUE,
//                         password_hash VARCHAR(255) NOT NULL,
//                         nombre VARCHAR(100),
//                         fecha_registro TIMESTAMP DEFAULT CURRENT_TIMESTAMP
//                     );
//                     CREATE TABLE IF NOT EXISTS proyecto(
//                         id SERIAL PRIMARY KEY,
//                         titulo VARCHAR(70)
//                         resumen VARCHAR(256),
//                         presupuesto FLOAT,
//                         tipo_financiacion VARCHAR(45),
//                         tipo_fondos VARCHAR(45),
//                         fecha_inicio DATE,
//                         fecha_fin DATE
//                     );
//                     CREATE TABLA IF NOT EXIST termino_clave(
//                         termino VARCHAR(30) PRIMARY KEY,
//                         termino_ingles VARCHAR(30)
//                         );
//                     CREATE TABLA IF NOT EXIST tipo_producto(
//                         id SERIAL PRIMARY KEY,
//                         categoria VARCHAR(45),
//                         clase VARCHAR(45),
//                         nombre VARCHAR(45),
//                         topologia VARCHAR(45)
                        
//                     )");
//             }
//             else if (proveedor == "mysql" || proveedor == "mariadb")
//             {
//                 scripts.Add(@"
//                     CREATE TABLE IF NOT EXISTS usuarios (
//                         id INT AUTO_INCREMENT PRIMARY KEY,
//                         email VARCHAR(100) NOT NULL UNIQUE,
//                         password_hash VARCHAR(255) NOT NULL,
//                         nombre VARCHAR(100),
//                         fecha_registro TIMESTAMP DEFAULT CURRENT_TIMESTAMP
//                     );
//                     CREATE TABLE IF NOT EXISTS tipo_producto (
//                         id INT AUTO_INCREMENT PRIMARY KEY,
//                         categoria VARCHAR(45),
//                         clase VARCHAR(45),
//                         nombre VARCHAR(45),
//                         topologia VARCHAR(45)
//                     );
//                     CREATE TABLE IF NOT EXISTS termino_clave (
//                         termino VARCHAR(30) PRIMARY KEY,
//                         termino_ingles VARCHAR(30)
//                     );
//                     CREATE TABLE IF NOT EXISTS proyecto (
//                         id INT AUTO_INCREMENT PRIMARY KEY,
//                         titulo VARCHAR(70),
//                         resumen VARCHAR(256),
//                         presupuesto FLOAT,
//                         tipo_financiacion VARCHAR(45),
//                         tipo_fondos VARCHAR(45),
//                         fecha_inicio DATE,
//                         fecha_fin DATE
//                     )");
//             }

//             // Agrega más tablas según necesites...

//             return scripts.ToArray();
//         }
//         private async Task CrearUsuarioPorDefectoSiNoExiste(string? proveedor, string connString)
//         {
//             const string email = "admin@ejemplo.com";
//             const string password = "admin123"; // Cambiar por algo más seguro en producción
//             string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

//             DbConnection connection = proveedor?.Contains("sqlserver") == true ? new SqlConnection(connString) :
//                                     proveedor == "postgres" ? new NpgsqlConnection(connString) :
//                                     new MySqlConnection(connString);

//             await using (connection)
//             {
//                 await connection.OpenAsync();
//                 // Verificar si el usuario ya existe
//                 using var cmdCheck = connection.CreateCommand();
//                 cmdCheck.CommandText = "SELECT COUNT(*) FROM usuarios WHERE email = @email";
//                 var param = cmdCheck.CreateParameter();
//                 param.ParameterName = "@email";
//                 param.Value = email;
//                 cmdCheck.Parameters.Add(param);

//                 var result = await cmdCheck.ExecuteScalarAsync();
//                 long count = (result != null && result != DBNull.Value) ? Convert.ToInt64(result) : 0;

//                 if (count == 0)
//                 {
//                     using var cmdInsert = connection.CreateCommand();
//                     cmdInsert.CommandText = "INSERT INTO usuarios (email, password_hash, nombre) VALUES (@email, @hash, @nombre)";
//                     cmdInsert.Parameters.Add(new SqlParameter("@email", email)); // Ajusta según el proveedor
//                     cmdInsert.Parameters.Add(new SqlParameter("@hash", passwordHash));
//                     cmdInsert.Parameters.Add(new SqlParameter("@nombre", "Administrador"));
//                     await cmdInsert.ExecuteNonQueryAsync();
//                     _logger.LogInformation("Usuario por defecto creado: {Email}", email);
//                 }
//             }
//         }
//     }
// }