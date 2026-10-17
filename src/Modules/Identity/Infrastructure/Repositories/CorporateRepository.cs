using GiysiGo.BuildingBlocks.Infrastructure;
using GiysiGo.Modules.Identity.Application.Interfaces.Repositories;
using GiysiGo.Modules.Identity.Domain.Entities;

namespace GiysiGo.Modules.Identity.Infrastructure.Repositories
{
    public sealed class CorporateRepository(SqlConnectionFactory connectionFactory) : RepositoryBase<Corporate>(connectionFactory), ICorporateRepository
    {
        protected override string SpGetAll => "[identity].[spGetAllCorporates]";
        protected override string SpGetById => "[identity].[spGetCorporateById]";
        protected override string SpInsert => "[identity].[spInsertCorporate]";
        protected override string SpUpdate => "[identity].[spUpdateCorporate]";
        protected override string SpDelete => "[identity].[spDeleteCorporate]";

        protected override object InsertParameters(Corporate entity) => new
        {
            Id = entity.Id == Guid.Empty ? (Guid?)null : entity.Id,
            entity.Name,
            entity.IsActive
        };

        protected override object UpdateParameters(Corporate entity) => new
        {
            entity.Id,
            entity.Name,
            entity.IsActive
        };
    }
}
