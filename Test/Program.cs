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
    public Guid? OtherParentId { get; set; }
    public string Name { get; set; } = default!;

    public ParentEntity? OtherParentEntity = default!;
    public ICollection<ParentEntity> InverseOtherParentEntity = new List<ParentEntity>();
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

            entity.HasOne(e => e.OtherParentEntity)
                .WithMany(e => e.InverseOtherParentEntity)
                .HasForeignKey(e => e.OtherParentId)
                .OnDelete(DeleteBehavior.Cascade);
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

        var parentEntities = new ParentEntity[]
        {
            new() {Name = "Parent 1"},
            new() {Name = "Parent 2"}
        };
        
        parentEntities[0].OtherParentId = parentEntities[0].Id;
        parentEntities[1].OtherParentId = parentEntities[0].Id;
        
        var entity = new Entity {Name = "Entity", ParentId = parentEntities[0].Id};

        dbContext.ParentEntities.AddRange(parentEntities);
        dbContext.Entities.Add(entity);
        await dbContext.SaveChangesAsync();

        var firstEntityWithout = await dbContext.Entities.FirstAsync();
        var firstEntityWith = await dbContext.Entities.Include(e => e.ParentEntity).FirstAsync();
        var firstParentEntityWithout = await dbContext.ParentEntities.FirstAsync();
        var firstParentEntityWith = await dbContext.ParentEntities.Include(e => e.OtherParentEntity).FirstAsync();
        
        Debug.Assert(firstEntityWithout.ParentEntity is null);
        Debug.Assert(firstEntityWith.ParentEntity is not null);
        Debug.Assert(firstParentEntityWithout.OtherParentEntity is null);
        Debug.Assert(firstParentEntityWith.OtherParentEntity is not null);
    }
}