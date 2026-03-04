using DataGateVPNBot.DataBase.Repositories.Interfaces;
using DataGateVPNBot.DataBase.Repositories.Queries.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace DataGateVPNBot.DataBase.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IRepository<T> GetRepository<T>() where T : class;
    IQuery<T> GetQuery<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    void SaveChanges();
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}