using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

public class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ParentId { get; set; }
    public string Name { get; set; } = default!;

    public ParentEntity? ParentEntity { get; set; } = default!;
}

public class ParentEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;

    public ICollection<Entity> Entities = new List<Entity>();
}

public class ApplicationDbContext : DbContext
{
    public DbSet<Entity> Entities { get; set; }
    public DbSet<ParentEntity> ParentEntities { get; set; }
    
    public ApplicationDbContext() : base() { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Server=localhost;User ID=postgres;Password=root;Port=5432;Database=root;Integrated Security=true;Pooling=true");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ParentEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
        
        modelBuilder.Entity<Entity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.ParentEntity)
                .WithMany(e => e.Entities)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        var dbContext = new ApplicationDbContext();
        await dbContext.Database.EnsureCreatedAsync();

        var parentEntity = new ParentEntity {Name = "Parent"};
        var entity = new Entity {Name = "Entity", ParentId = parentEntity.Id};

        dbContext.ParentEntities.Add(parentEntity);
        dbContext.Entities.Add(entity);
        await dbContext.SaveChangesAsync();

        var firstEntityWithout = await dbContext.Entities.FirstAsync();
        var firstEntityWith = await dbContext.Entities.Include(e => e.ParentEntity).FirstAsync();
        
        Debug.Assert(firstEntityWithout.ParentEntity is null);
        Debug.Assert(firstEntityWith.ParentEntity is not null);
    }
}