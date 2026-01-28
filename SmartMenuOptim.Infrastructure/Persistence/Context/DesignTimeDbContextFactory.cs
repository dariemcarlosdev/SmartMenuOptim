using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SmartMenuOptim.Infrastructure.Persistence.Context
{
    /// <summary>
    /// Factory for creating <see cref="AppDbContext"/> instances at design time, primarily for Entity Framework Core tooling.
    /// </summary>
    /// <remarks>
    /// <para><strong>Purpose:</strong></para>
    /// <para>
    /// This factory enables EF Core CLI tools (dotnet-ef) to create database context instances when the application
    /// is not running. This is essential for design-time operations such as creating migrations, updating the database,
    /// and scaffolding database schemas.
    /// </para>
    /// 
    /// <para><strong>How It Works:</strong></para>
    /// <para>
    /// When you run EF Core commands like "dotnet ef migrations add" or "dotnet ef database update", the tooling
    /// looks for a class implementing <see cref="IDesignTimeDbContextFactory{TContext}"/>. This factory:
    /// </para>
    /// <list type="number">
    ///   <item>Loads configuration from appsettings.json and environment variables (simulating runtime configuration)</item>
    ///   <item>Retrieves the database connection string from the configuration</item>
    ///   <item>Configures DbContext options with PostgreSQL provider and retry logic</item>
    ///   <item>Returns a fully configured <see cref="AppDbContext"/> instance ready for design-time operations</item>
    /// </list>
    /// 
    /// <para><strong>When To Use:</strong></para>
    /// <list type="bullet">
    ///   <item><strong>Automatic:</strong> EF Core tools automatically discover and use this factory for migrations and database updates</item>
    ///   <item><strong>Manual:</strong> Can be used in development scripts or testing scenarios where you need a DbContext without DI</item>
    ///   <item><strong>Multi-Project Solutions:</strong> Essential when your DbContext is in a class library separate from the startup project</item>
    /// </list>
    /// 
    /// <para><strong>Important Notes:</strong></para>
    /// <list type="bullet">
    ///   <item>This is NOT used at runtime - runtime DbContext instances should be injected via dependency injection</item>
    ///   <item>Ensure appsettings.json exists in the Infrastructure project directory for EF tools to work correctly</item>
    ///   <item>The connection string must be named "DefaultConnection" in your configuration files</item>
    ///   <item>This class must be in the same assembly as your DbContext for EF Core to discover it</item>
    /// </list>
    /// 
    /// <para><strong>Example Usage (Automatic by EF Tools):</strong></para>
    /// <code>
    /// # Run from the Infrastructure project directory
    /// dotnet ef migrations add InitialCreate
    /// dotnet ef database update
    /// </code>
    /// </remarks>
    internal class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        // Configuration constants - These define where and how to load connection strings
        private const string DefaultConnectionStringName = "DefaultConnection";
        private const string AppSettingsFileName = "appsettings.json";
        private const string DevelopmentSettingsFileName = "appsettings.Development.json";

        /// <summary>
        /// Creates a new instance of the AppDbContext for design-time operations such as migrations.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is the entry point called by Entity Framework Core tools when they need a DbContext instance.
        /// It orchestrates the entire process of configuration loading, connection string retrieval, and DbContext creation.
        /// </para>
        /// <para>
        /// The method follows a clear three-step process:
        /// 1. Build configuration from JSON files and environment variables
        /// 2. Extract and validate the connection string
        /// 3. Configure and return the DbContext with PostgreSQL provider settings
        /// </para>
        /// <para><strong>Important:</strong> This method should never be called directly in application code. 
        /// At runtime, always use dependency injection to obtain DbContext instances.</para>
        /// </remarks>
        /// <param name="args">Command-line arguments passed by EF Core tools. Currently not used by this implementation,
        /// but can be utilized for advanced scenarios like selecting different connection strings based on arguments.</param>
        /// <returns>A fully configured <see cref="AppDbContext"/> instance ready for design-time operations.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the connection string cannot be found or is invalid.</exception>
        AppDbContext IDesignTimeDbContextFactory<AppDbContext>.CreateDbContext(string[] args)
        {
            // Step 1: Load configuration from files and environment
            var configuration = BuildConfiguration();
            
            // Step 2: Retrieve and validate the connection string
            var connectionString = GetConnectionString(configuration);
            
            // Step 3: Build DbContext options with PostgreSQL configuration
            var options = BuildDbContextOptions(connectionString);
            
            // Step 4: Return the configured DbContext instance
            return new AppDbContext(options);
        }

        /// <summary>
        /// Builds the application configuration by loading settings from multiple sources in order of precedence.
        /// </summary>
        /// <remarks>
        /// <para><strong>Configuration Sources (in order of precedence):</strong></para>
        /// <list type="number">
        ///   <item><strong>appsettings.json:</strong> Base configuration (required) - contains default settings for all environments</item>
        ///   <item><strong>appsettings.Development.json:</strong> Development overrides (optional) - contains development-specific settings</item>
        ///   <item><strong>Environment Variables:</strong> Highest priority - can override any setting, useful for secrets in CI/CD</item>
        /// </list>
        /// 
        /// <para><strong>Why This Matters:</strong></para>
        /// <para>
        /// This layered approach allows you to:
        /// - Keep sensitive data (like production connection strings) out of source control using environment variables
        /// - Maintain different configurations per environment without code changes
        /// - Override settings easily in CI/CD pipelines or container deployments
        /// </para>
        /// 
        /// <para><strong>Base Path Behavior:</strong></para>
        /// <para>
        /// Uses <c>Directory.GetCurrentDirectory()</c> which returns the directory from where the dotnet-ef command is executed.
        /// Typically, this should be the Infrastructure project directory where appsettings.json resides.
        /// If running from a different directory, ensure configuration files are accessible or provide the full path.
        /// </para>
        /// </remarks>
        /// <returns>An <see cref="IConfigurationRoot"/> containing merged settings from all configuration sources.</returns>
        private static IConfigurationRoot BuildConfiguration()
        {
            // Get the directory where dotnet-ef command is executed (usually the project directory)
            var basePath = Directory.GetCurrentDirectory();
            
            return new ConfigurationBuilder()
                .SetBasePath(basePath) // Set the base directory for relative file paths
                .AddJsonFile(AppSettingsFileName, optional: false, reloadOnChange: false) // Load base settings (required)
                .AddJsonFile(DevelopmentSettingsFileName, optional: true, reloadOnChange: false) // Load dev overrides (optional)
                .AddEnvironmentVariables() // Allow environment variables to override any setting (highest priority)
                .Build();
        }

        /// <summary>
        /// Retrieves and validates the database connection string from the application configuration.
        /// </summary>
        /// <remarks>
        /// <para><strong>Expected Configuration Format:</strong></para>
        /// <para>The connection string should be defined in your appsettings.json as:</para>
        /// <code>
        /// {
        ///   "ConnectionStrings": {
        ///     "DefaultConnection": "Host=localhost;Database=SmartMenuOptim;Username=postgres;Password=yourpassword"
        ///   }
        /// }
        /// </code>
        /// 
        /// <para><strong>Security Best Practices:</strong></para>
        /// <list type="bullet">
        ///   <item>For development: Store connection strings in appsettings.Development.json (excluded from source control)</item>
        ///   <item>For production: Use environment variables or Azure Key Vault, never commit production credentials</item>
        ///   <item>For CI/CD: Set connection strings via pipeline variables or secrets management</item>
        /// </list>
        /// 
        /// <para><strong>Troubleshooting:</strong></para>
        /// <para>
        /// If you receive an InvalidOperationException, verify that:
        /// - The appsettings.json file exists in the current directory
        /// - The "ConnectionStrings" section exists in the JSON
        /// - The "DefaultConnection" key is present and has a non-empty value
        /// </para>
        /// </remarks>
        /// <param name="configuration">The configuration instance to query for the connection string. Must contain a valid
        /// "ConnectionStrings:DefaultConnection" entry.</param>
        /// <returns>The validated PostgreSQL connection string.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the connection string is missing, null, empty, or contains only whitespace.
        /// The exception message provides guidance on how to fix the configuration.
        /// </exception>
        private static string GetConnectionString(IConfiguration configuration)
        {
            // Retrieve the connection string from the "ConnectionStrings" section
            var connectionString = configuration.GetConnectionString(DefaultConnectionStringName);
            
            // Validate that a valid connection string was found
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string '{DefaultConnectionStringName}' not found in configuration. " +
                    $"Ensure {AppSettingsFileName} contains a valid connection string.");
            }

            return connectionString;
        }

        /// <summary>
        /// Configures and returns DbContext options for PostgreSQL with resilient connection settings.
        /// </summary>
        /// <remarks>
        /// <para><strong>PostgreSQL Configuration:</strong></para>
        /// <para>
        /// This method configures the DbContext to use Npgsql (PostgreSQL provider for .NET) with automatic
        /// retry logic to handle transient database failures gracefully.
        /// </para>
        /// 
        /// <para><strong>Retry Logic Details:</strong></para>
        /// <list type="bullet">
        ///   <item><strong>Max Retry Count:</strong> 3 attempts - After the initial attempt fails, retries up to 3 more times</item>
        ///   <item><strong>Max Retry Delay:</strong> 5 seconds - Maximum wait time between retry attempts (uses exponential backoff)</item>
        ///   <item><strong>Error Codes:</strong> null - Uses PostgreSQL default transient error codes (connection timeouts, deadlocks, etc.)</item>
        /// </list>
        /// 
        /// <para><strong>Why Retry Logic Matters:</strong></para>
        /// <para>
        /// During migrations or database updates, temporary issues like network hiccups, database restarts,
        /// or connection pool exhaustion can occur. Retry logic makes these operations more resilient by
        /// automatically retrying failed operations, reducing the need for manual intervention.
        /// </para>
        /// 
        /// <para><strong>Customization Options:</strong></para>
        /// <para>
        /// You can customize this further by:
        /// - Adjusting maxRetryCount for more/fewer attempts
        /// - Modifying maxRetryDelay for different timing strategies
        /// - Adding specific PostgreSQL error codes to errorCodesToAdd for custom retry scenarios
        /// - Adding MigrationsHistoryTable configuration for custom migration tracking
        /// </para>
        /// </remarks>
        /// <param name="connectionString">The PostgreSQL connection string. Must be valid and include Host, Database, and credentials.</param>
        /// <returns>
        /// Configured <see cref="DbContextOptions{AppDbContext}"/> ready to be passed to the AppDbContext constructor.
        /// </returns>
        private static DbContextOptions<AppDbContext> BuildDbContextOptions(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            
            // Configure PostgreSQL as the database provider with resilient connection settings
            optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // Enable automatic retry on transient failures (network issues, timeouts, etc.)
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,                        // Retry up to 3 times after initial failure
                    maxRetryDelay: TimeSpan.FromSeconds(5),  // Wait maximum 5 seconds between retries
                    errorCodesToAdd: null);                  // Use default PostgreSQL transient error codes
            });

            return optionsBuilder.Options;
        }
    }
}
