namespace DataGateVPNBot.DataBase.Repositories.Queries.Interfaces;

public interface IQuery<T> where T : class
{
    // IQueryable<T> AsQueryable { get; }
    IQueryable<T> AsQueryable();
}