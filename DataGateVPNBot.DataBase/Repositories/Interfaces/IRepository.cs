namespace DataGateVPNBot.DataBase.Repositories.Interfaces;

public interface IRepository<T> where T : class
{
    IQueryable<T> Query { get; } 
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Delete(T entity);
}