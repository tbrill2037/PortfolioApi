using Microsoft.Extensions.Configuration;
using Portfolio.Domain.Repositories.Query.Base;
using Portfolio.Infra.Data;

namespace Portfolio.Infra.Repository.Query.Base;

/// <summary>
/// Provides a base implementation for query repositories.
/// </summary>
public class QueryRepository<T> : DbConnector, IQueryRepository<T> where T : class
{
    public QueryRepository(IConfiguration configuration)
        : base(configuration)
    {
    }
}