using Dapper;

namespace NutsShop_Server.Shop;

public sealed class MigrationRunner
{
    private readonly PgStore _store;
    private readonly IHostEnvironment _env;

    public MigrationRunner(PgStore store, IHostEnvironment env)
    {
        _store = store;
        _env = env;
    }

    public async Task ApplyAsync(CancellationToken ct)
    {
        using var conn = await _store.OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition("""
            create table if not exists __migrations (
              id serial primary key,
              name text not null,
              applied_at timestamptz not null
            );
        """, cancellationToken: ct));

        var migrationsDir = Path.Combine(_env.ContentRootPath, "Migrations");
        if (!Directory.Exists(migrationsDir)) return;

        var files = Directory.GetFiles(migrationsDir, "*.sql")
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var file in files)
        {
            var name = Path.GetFileName(file);
            var exists = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                "select count(1) from __migrations where name = @Name;",
                new { Name = name },
                cancellationToken: ct));

            if (exists > 0) continue;

            var sql = await File.ReadAllTextAsync(file, ct);
            if (string.IsNullOrWhiteSpace(sql)) continue;

            await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
            await conn.ExecuteAsync(new CommandDefinition("""
                insert into __migrations(name, applied_at)
                values (@Name, @AppliedAt);
            """, new { Name = name, AppliedAt = DateTimeOffset.UtcNow }, cancellationToken: ct));
        }
    }
}
