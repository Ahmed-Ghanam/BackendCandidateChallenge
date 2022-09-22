using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace QuizService;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc();
        services.AddSingleton(InitializeDb());
        services.AddControllers();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }

    private IDbConnection InitializeDb()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        // Migrate up
        var assembly = typeof(Startup).GetTypeInfo().Assembly;
        var migrationResourceNames = assembly.GetManifestResourceNames()
            .Where(x => x.EndsWith(".sql"))
            .OrderBy(x => x);

        //TODO I prefer using parentheses with program flow control statements.
        if (!migrationResourceNames.Any()) throw new System.Exception("No migration files found!");

        //TODO The migrationResourceNames is begin enumerated twice. It will be better to force enumeration at the variable initialization by converting it to an array or a list.
        foreach (var resourceName in migrationResourceNames)
        {
            var sql = GetResourceText(assembly, resourceName);
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        return connection;
    }

    private static string GetResourceText(Assembly assembly, string resourceName)
    {
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}