using Microsoft.EntityFrameworkCore;
using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Auth.Db;

namespace SampleProject.DbContext
{
    /// <summary>
    /// Main Db Context
    /// </summary>
    public class RDbContext : RSecurityDbContext<RAppUser, RAppRole, Guid>
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="options"></param>
        public RDbContext(DbContextOptions options) : base(options)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json")
                   .Build();
            if (bool.Parse(configuration["DatabaseMigrate"] ?? false.ToString()))
            {
                Database.Migrate();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
