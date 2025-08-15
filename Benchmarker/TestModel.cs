using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Benchmarker
{
    [Table("test_model")]
    public class TestModel
    {
        public TestModel()
        {
            TestMeasure = Random.Shared.NextDouble();
            DatetimeInclusion = new DateTime(Random.Shared.Next(), DateTimeKind.Utc);
            Name = "TEST";
            TestFlag = Random.Shared.NextSingle() >= 0.5;
        }

        [Key]
        [Column("id_test_model")]
        public long IdTestModel { get; set; }

        [Column("datetime_inclusion")]
        public DateTime DatetimeInclusion { get; set; }

        [Column("test_measure")]
        public double TestMeasure { get; set; }

        [Column("test_flag")]
        public bool TestFlag { get; set; }

        [Column("name")]
        public string Name { get; set; }
    }


    public class TestDbContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);
        }

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }
        public DbSet<TestModel> ftQueue { get; set; }
    }
}