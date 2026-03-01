using AIMS.BackendServer.Data;
using Microsoft.EntityFrameworkCore;

namespace AIMS.BackendServer.UnitTests.Helpers;

public static class TestDbContextFactory
{
    // Tạo InMemory DbContext — mỗi test dùng 1 DB riêng biệt
    public static AimsDbContext Create(string dbName = "")
    {
        var name = string.IsNullOrEmpty(dbName)
            ? Guid.NewGuid().ToString()  // Unique cho mỗi test
            : dbName;

        var options = new DbContextOptionsBuilder<AimsDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

        var context = new AimsDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}