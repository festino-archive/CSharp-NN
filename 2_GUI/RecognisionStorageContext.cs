using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System.IO;

namespace Lab
{
    class RecognisionData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int X1 { get; set; }
        public int X2 { get; set; }
        public int Y1 { get; set; }
        public int Y2 { get; set; }
        public string Category { get; set; }
        public byte[] ObjectImage { get; set; }
    }

    class RecognisionStorageContext : DbContext
    {
        public DbSet<RecognisionData> Recognised { get; set; }

        public string DbPath { get; private set; }

        public RecognisionStorageContext(bool tryLoad)
        {
            var sep = Path.DirectorySeparatorChar;
            var path = "." + sep;
            DbPath = $"{path}{sep}recognised.db";
            if (!tryLoad)
                if (File.Exists(DbPath))
                    File.Delete(DbPath);
            if (!File.Exists(DbPath))
            {
                Database.Migrate();
                RelationalDatabaseCreator databaseCreator =
                    (RelationalDatabaseCreator)Database.GetService<IDatabaseCreator>();
                databaseCreator.CreateTables();
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite($"Data Source={DbPath}");
        }
    }
}
