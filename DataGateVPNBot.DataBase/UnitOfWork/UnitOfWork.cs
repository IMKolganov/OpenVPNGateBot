using DataGateVPNBot.DataBase.Contexts;
using DataGateVPNBot.DataBase.Repositories.Interfaces;
using DataGateVPNBot.DataBase.Repositories.Queries.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DataGateVPNBot.DataBase.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext? _context;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IRepositoryFactory _repositoryFactory;
    private readonly IQueryFactory _queryFactory;

    public UnitOfWork(ApplicationDbContext? context, IDbContextFactory<ApplicationDbContext> dbContextFactory, IRepositoryFactory repositoryFactory, IQueryFactory queryFactory)
    {
        _context = context; // for API (Scoped)
        _dbContextFactory = dbContextFactory; // for BackgroundService
        _repositoryFactory = repositoryFactory;
        _queryFactory = queryFactory;
    }

    public IRepository<T> GetRepository<T>() where T : class
    {
        return _repositoryFactory.GetRepository<T>();
    }

    public IQuery<T> GetQuery<T>() where T : class
    {
        return _queryFactory.GetQuery<T>();
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_context != null)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.SaveChangesAsync(cancellationToken);
    }

    public void SaveChanges()
    {
        _context?.SaveChanges();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_context != null)
        {
            return await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
