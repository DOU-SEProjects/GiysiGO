using System.Data;
using System.Data.Common;
using Dapper;

namespace GiysiGo.Modules.Identity.Infrastructure.Repositories
{
    public abstract class IdentityCommandRepositoryBase
    {
        protected static CommandDefinition Procedure(string name, object parameters, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(transaction);
            if (connection.State != ConnectionState.Open || !ReferenceEquals(transaction.Connection, connection))
            {
                throw new ArgumentException("An open connection and its active transaction are required.", nameof(transaction));
            }
            return new CommandDefinition(name, parameters, transaction, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken);
        }
    }
}
