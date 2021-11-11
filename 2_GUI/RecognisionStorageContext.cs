using Microsoft.EntityFrameworkCore;
using System;
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
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = $"{path}{Path.DirectorySeparatorChar}recognised.db";
            if (!tryLoad)
                if (File.Exists(DbPath))
                    File.Delete(DbPath);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
             => options.UseSqlite($"Data Source={DbPath}");
    }
}
