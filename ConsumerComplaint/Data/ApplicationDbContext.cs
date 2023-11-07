using Microsoft.EntityFrameworkCore;
using ConsumerComplaint.Models;
using System.Collections.Generic;

namespace ConsumerComplaint.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<Company> CompanyData { get; set; }
        public DbSet<Product> ProductData { get; set; }
        public DbSet<Complaint> ComplaintData { get; set; }
    }
}
