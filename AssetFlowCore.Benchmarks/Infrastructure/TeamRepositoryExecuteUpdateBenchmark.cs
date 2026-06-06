using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Infrastructure.Persistence;
using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.VSDiagnostics;

namespace AssetFlowCore.Benchmarks.Infrastructure
{
    [CPUUsageDiagnoser]
    public class TeamRepositoryExecuteUpdateBenchmark
    {
        private readonly DbContextOptions<AssetFlowDbContext> _options = null!;
        private Guid _teamId;
        private int _counter;
        [GlobalSetup]
        public async Task GlobalSetup()
        {
            var _options = new DbContextOptionsBuilder<AssetFlowDbContext>()
                .UseInMemoryDatabase($"Bench_Cache_Test")
                .ConfigureWarnings(w =>
                    w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics
                                       .InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var ctx = new AssetFlowDbContext(_options);

            await ctx.Database.EnsureCreatedAsync();
            var existing = await ctx.Teams.FirstOrDefaultAsync();
            if (existing == null)
            {
                var team = new Team("Benchmark-Team", "Servers", "High");
                await ctx.Teams.AddAsync(team);
                await ctx.SaveChangesAsync();
                _teamId = team.Id;
            }
            else
            {
                _teamId = existing.Id;
                // normalize name
                existing.Update("Benchmark-Team", null, null, null);
                ctx.Teams.Update(existing);
                await ctx.SaveChangesAsync();
            }

            _counter = 0;
        }

        [IterationSetup]
        public async Task IterationSetup()
        {
            // reset name before each iteration to reduce skew
            await using var ctx = new AssetFlowDbContext(_options);
            var t = await ctx.Teams.FirstAsync(x => x.Id == _teamId);
            t.Update("Benchmark-Team", null, null, null);
            ctx.Teams.Update(t);
            await ctx.SaveChangesAsync();
        }

        [Benchmark]
        public async Task AttachAndSaveChanges()
        {
            await using var ctx = new AssetFlowDbContext(_options);
            var team = await ctx.Teams.AsNoTracking().FirstAsync(t => t.Id == _teamId);
            // apply change
            var newName = $"Attach-{Interlocked.Increment(ref _counter)}";
            team.Update(newName, null, null, null);
            // attach and mark modified
            ctx.Teams.Attach(team);
            ctx.Entry(team).State = EntityState.Modified;
            await ctx.SaveChangesAsync();
        }

        [Benchmark]
        public async Task ExecuteUpdateAsync()
        {
            await using var ctx = new AssetFlowDbContext(_options);
            var newName = $"Exec-{Interlocked.Increment(ref _counter)}";
            await ctx.Teams.Where(t => t.Id == _teamId).ExecuteUpdateAsync(s => s.SetProperty(t => t.Name, _ => newName));
        }
    }
}