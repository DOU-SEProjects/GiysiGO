using System.Data.Common;

namespace GiysiGo.BuildingBlocks.Application.Contracts
{
    public interface ITransactionalCustomerAddressReader
    {
        Task<CustomerAddressSnapshot?> GetOwnedAddressAsync(Guid customerId, Guid addressId, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
    }
}
