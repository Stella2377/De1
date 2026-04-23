using De1.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection.Emit;

namespace De1.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories { get; set; }
    public DbSet<Equipment> Equipments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // 1. Cấu hình Shadow Properties [cite: 9]
            // Sử dụng ClrType để đảm bảo lấy đúng Type của Class
            var entityBuilder = modelBuilder.Entity(entityType.ClrType);

            entityBuilder.Property<DateTime>("CreatedAt");
            entityBuilder.Property<DateTime?>("UpdatedAt");
            entityBuilder.Property<bool>("IsDeleted").HasDefaultValue(false);

            // 2. Sửa lỗi CS8917: Tạo Global Query Filter động 
            // Tạo tham số "e" tương ứng với kiểu của Entity hiện tại
            var parameter = Expression.Parameter(entityType.ClrType, "e");

            // Tạo lệnh gọi: EF.Property<bool>(e, "IsDeleted")
            var efPropertyCall = Expression.Call(
                typeof(EF),
                nameof(EF.Property),
                new[] { typeof(bool) },
                parameter,
                Expression.Constant("IsDeleted")
            );

            // Tạo so sánh: EF.Property<bool>(e, "IsDeleted") == false
            var filterExpression = Expression.Equal(efPropertyCall, Expression.Constant(false));

            // Chuyển thành Lambda: e => EF.Property<bool>(e, "IsDeleted") == false
            var lambda = Expression.Lambda(filterExpression, parameter);

            // Áp dụng bộ lọc
            entityBuilder.HasQueryFilter(lambda);

            modelBuilder.Entity<Equipment>()
                .Property(e => e.Status)
                .IsConcurrencyToken(); // kiểm tra tranh chấp
        }
    }

    // 3. Override SaveChanges để gán giá trị tự động
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries();
        foreach (var entry in entries)
        {
            var now = DateTime.Now;
            if (entry.State == EntityState.Added)
            {
                entry.Property("CreatedAt").CurrentValue = now;
            }

            // Gom nhóm logic để đảm bảo UpdatedAt luôn được gán khi có thay đổi
            if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
            {
                entry.Property("UpdatedAt").CurrentValue = now;

                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Property("IsDeleted").CurrentValue = true; // REQ -03: Soft Delete
                }
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}