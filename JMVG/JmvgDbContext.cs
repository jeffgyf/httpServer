using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JMVG
{
    public class JmvgDbContext:DbContext
    {
        private string username;
        private string password;

        public JmvgDbContext(string username, string password)
        {
            this.username = username;
            this.password = password;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString = new SqlConnectionStringBuilder
            {
                DataSource = "jmvg.database.windows.net",
                Authentication = SqlAuthenticationMethod.SqlPassword,
                InitialCatalog = "jmvg",
                UserID = username,
                Password = password
            }.ConnectionString;

            optionsBuilder.UseSqlServer(connectionString).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }

        public DbSet<Video> Videos { get; set; }
    }
}
