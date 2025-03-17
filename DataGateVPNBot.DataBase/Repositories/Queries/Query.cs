using DataGateVPNBot.DataBase.Contexts;
using DataGateVPNBot.DataBase.Repositories.Queries.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataGateVPNBot.DataBase.Repositories.Queries;

public class Query<T> : IQuery<T> where T : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Query(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public IQueryable<T> AsQueryable() => _dbSet;
}