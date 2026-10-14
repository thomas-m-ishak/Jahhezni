using Microsoft.EntityFrameworkCore;
using Jahhezni.Models;

namespace Jahhezni.Models
{
    public class JahhezniContext : DbContext
    {
        public JahhezniContext(DbContextOptions<JahhezniContext> options)
            : base(options)
        {
        }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Merchant> Merchants { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<PreparationItem> PreparationItems { get; set; }
        public DbSet<PreparationList> PreparationLists { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Payment> Payments { get; set; }

    }
}