using System.Collections.Concurrent;
using DataGateVPNBot.DataBase.Contexts;
using DataGateVPNBot.DataBase.Repositories.Interfaces;
using DataGateVPNBot.DataBase.Repositories.Queries.Interfaces;

namespace DataGateVPNBot.DataBase.Repositories.Queries;

public class QueryFactory : IQueryFactory
{
    private readonly ApplicationDbContext _context;
    private readonly ConcurrentDictionary<Type, object> _queries = new();

    public QueryFactory(ApplicationDbContext context)
    {
        _context = context;
    }

    public IQuery<T> GetQuery<T>() where T : class
    {
        return (IQuery<T>)_queries.GetOrAdd(typeof(T), _ => new Query<T>(_context));
    }
}