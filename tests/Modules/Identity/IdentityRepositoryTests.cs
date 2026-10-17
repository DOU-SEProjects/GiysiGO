using GiysiGo.Modules.Identity.Application.Interfaces.Repositories;
using GiysiGo.Modules.Identity.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace GiysiGo.Modules.Identity.Tests
{
    public sealed class IdentityRepositoryTests
    {
        [SqlServerFact]
        public async Task Oversized_text_is_rejected_without_truncating_or_matching_another_user()
        {
            using var db = new IdentityDatabase();
            var email = new string('a', 275) + Guid.NewGuid().ToString("N") + "@example.test";
            Assert.Equal(320, email.Length);
            await db.CreateUserAsync(email);
            var oversized = email + "x";
            Assert.Equal(51000, (await Assert.ThrowsAsync<SqlException>(() => db.CreateUserAsync(oversized))).Number);
            Assert.Equal(51000, (await Assert.ThrowsAsync<SqlException>(() => db.Transactions.ExecuteAsync((c, t, ct) =>
                db.Get<IUserRepository>().GetByEmailAsync(oversized, c, t, ct)))).Number);
            Assert.Equal(51000, (await Assert.ThrowsAsync<SqlException>(() => db.Get<IRoleRepository>().InsertAsync(
                new Role { Code = new string('r', 101), Name = "Test", Scope = "GLOBAL" }))).Number);
        }

        [SqlServerFact]
        public async Task Generic_repositories_round_trip_update_and_soft_delete()
        {
            using var db = new IdentityDatabase();
            var corporates = db.Get<ICorporateRepository>();
            var roles = db.Get<IRoleRepository>();
            var permissions = db.Get<IPermissionRepository>();
            var corporateId = await corporates.InsertAsync(new Corporate { Name = "Şirket İstanbul" });
            var roleId = await db.CreateRoleAsync("GLOBAL");
            var permissionId = await permissions.InsertAsync(new Permission { Code = Guid.NewGuid().ToString("N"), Description = "İzin açıklaması" });

            var corporate = Assert.IsType<Corporate>(await corporates.GetByIdAsync(corporateId));
            Assert.NotEqual(Guid.Empty, corporate.Id);
            Assert.Equal("Şirket İstanbul", corporate.Name);
            Assert.True(corporate.IsActive);
            Assert.Null(corporate.UpdateDate);
            Assert.Null(corporate.DeleteDate);
            Assert.InRange(corporate.CreateDate, DateTime.UtcNow.AddMinutes(-2), DateTime.UtcNow.AddMinutes(1));
            corporate.Name = "Updated";
            await corporates.UpdateAsync(corporate);
            Assert.NotNull((await corporates.GetByIdAsync(corporateId))!.UpdateDate);
            Assert.Equal("Updated", (await corporates.GetByIdAsync(corporateId))!.Name);

            var role = Assert.IsType<Role>(await roles.GetByIdAsync(roleId));
            Assert.Equal("GLOBAL", role.Scope);
            var originalCode = role.Code;
            role.Code = "ignored-change";
            role.Scope = "STORE";
            role.Name = "Updated role";
            await roles.UpdateAsync(role);
            Assert.Equal(originalCode, (await roles.GetByIdAsync(roleId))!.Code);
            Assert.Equal("GLOBAL", (await roles.GetByIdAsync(roleId))!.Scope);
            Assert.Equal("Updated role", (await roles.GetByIdAsync(roleId))!.Name);
            var permission = Assert.IsType<Permission>(await permissions.GetByIdAsync(permissionId));
            permission.Description = "Updated permission";
            await permissions.UpdateAsync(permission);
            Assert.Equal("Updated permission", (await permissions.GetByIdAsync(permissionId))!.Description);

            await corporates.DeleteAsync(corporateId);
            await roles.DeleteAsync(roleId);
            await permissions.DeleteAsync(permissionId);
            Assert.Null(await corporates.GetByIdAsync(corporateId));
            Assert.Null(await roles.GetByIdAsync(roleId));
            Assert.Null(await permissions.GetByIdAsync(permissionId));
            Assert.DoesNotContain(await corporates.GetAllAsync(), x => x.Id == corporateId);
            Assert.DoesNotContain(await roles.GetAllAsync(), x => x.Id == roleId);
            Assert.DoesNotContain(await permissions.GetAllAsync(), x => x.Id == permissionId);
        }

        [SqlServerFact]
        public async Task Users_have_case_insensitive_email_and_explicit_sensitive_commands()
        {
            using var db = new IdentityDatabase();
            var users = db.Get<IUserRepository>();
            var email = $"Person-{Guid.NewGuid():N}@example.test";
            var userId = await db.CreateUserAsync("  " + email + "  ");
            await db.Transactions.ExecuteAsync(async (connection, transaction, token) =>
            {
                var user = await users.GetByEmailAsync(email.ToUpperInvariant(), connection, transaction, token);
                Assert.Equal(userId, user!.Id);
                Assert.Equal(email, user.Email);
                await users.SetPasswordHashAsync(userId, "changed-hash", connection, transaction, token);
                await users.SetActiveAsync(userId, false, connection, transaction, token);
                var updated = await users.GetByIdAsync(userId, connection, transaction, token);
                Assert.Equal("changed-hash", updated!.PasswordHash);
                Assert.False(updated.IsActive);
                return true;
            });
            var duplicate = await Assert.ThrowsAsync<SqlException>(() => db.CreateUserAsync(email.ToUpperInvariant()));
            Assert.Contains(duplicate.Number, new[] { 2601, 2627 });
            await db.Transactions.ExecuteAsync(async (connection, transaction, token) =>
            {
                await users.DeleteAsync(userId, connection, transaction, token);
                Assert.Null(await users.GetByIdAsync(userId, connection, transaction, token));
                Assert.Null(await users.GetByEmailAsync(email, connection, transaction, token));
                return true;
            });
        }

        [SqlServerFact]
        public async Task Address_reads_and_writes_enforce_owner_and_preserve_decimal_values()
        {
            using var db = new IdentityDatabase();
            var owner = await db.CreateUserAsync();
            var other = await db.CreateUserAsync();
            var addresses = db.Get<ICustomerAddressRepository>();
            var address = new CustomerAddress { Label = "Ev", AddressText = "İstanbul", ContactName = "Test", ContactPhone = "000", Latitude = 41.123456m, Longitude = 29.654321m };
            address.Id = await db.Transactions.ExecuteAsync((c, t, ct) => addresses.InsertAsync(owner, address, c, t, ct));
            await db.Transactions.ExecuteAsync(async (c, t, ct) =>
            {
                var saved = await addresses.GetOwnedAsync(owner, address.Id, c, t, ct);
                Assert.Equal(owner, saved!.UserId);
                Assert.Equal(address.Latitude, saved.Latitude);
                Assert.Equal(address.Longitude, saved.Longitude);
                Assert.Equal("Ev", saved.Label);
                Assert.Equal("İstanbul", saved.AddressText);
                Assert.Equal("Test", saved.ContactName);
                Assert.Equal("000", saved.ContactPhone);
                Assert.Null(await addresses.GetOwnedAsync(other, address.Id, c, t, ct));
                Assert.Empty(await addresses.GetOwnedListAsync(other, c, t, ct));
                return true;
            });
            var updateError = await Assert.ThrowsAsync<SqlException>(() => db.Transactions.ExecuteAsync(async (c, t, ct) => { await addresses.UpdateOwnedAsync(other, address, c, t, ct); return true; }));
            Assert.Equal(51001, updateError.Number);
            var deleteError = await Assert.ThrowsAsync<SqlException>(() => db.Transactions.ExecuteAsync(async (c, t, ct) => { await addresses.DeleteOwnedAsync(other, address.Id, c, t, ct); return true; }));
            Assert.Equal(51001, deleteError.Number);
            await db.Transactions.ExecuteAsync(async (c, t, ct) =>
            {
                address.UserId = other;
                address.Label = "Yeni";
                await addresses.UpdateOwnedAsync(owner, address, c, t, ct);
                Assert.Equal(owner, (await addresses.GetOwnedAsync(owner, address.Id, c, t, ct))!.UserId);
                Assert.Equal("Yeni", (await addresses.GetOwnedAsync(owner, address.Id, c, t, ct))!.Label);
                await addresses.DeleteOwnedAsync(owner, address.Id, c, t, ct);
                Assert.Null(await addresses.GetOwnedAsync(owner, address.Id, c, t, ct));
                Assert.Empty(await addresses.GetOwnedListAsync(owner, c, t, ct));
                return true;
            });
        }

        [SqlServerFact]
        public async Task Invalid_coordinates_roll_back_and_inactive_users_cannot_create_addresses()
        {
            using var db = new IdentityDatabase();
            var owner = await db.CreateUserAsync();
            var addresses = db.Get<ICustomerAddressRepository>();
            var address = new CustomerAddress { Label = "Home", AddressText = "Test", ContactName = "Test", ContactPhone = "000", Latitude = 91, Longitude = 29 };
            var invalid = await Assert.ThrowsAsync<SqlException>(() => db.Transactions.ExecuteAsync((c, t, ct) => addresses.InsertAsync(owner, address, c, t, ct)));
            Assert.Equal(547, invalid.Number);
            await db.Transactions.ExecuteAsync(async (c, t, ct) => { await db.Get<IUserRepository>().SetActiveAsync(owner, false, c, t, ct); return true; });
            address.Latitude = 41;
            var inactive = await Assert.ThrowsAsync<SqlException>(() => db.Transactions.ExecuteAsync((c, t, ct) => addresses.InsertAsync(owner, address, c, t, ct)));
            Assert.Equal(51001, inactive.Number);
        }

        [SqlServerFact]
        public async Task All_access_relationships_round_trip_and_revoke_without_duplicates()
        {
            using var db = new IdentityDatabase();
            var user = await db.CreateUserAsync();
            var globalRole = await db.CreateRoleAsync("GLOBAL");
            var corporateRole = await db.CreateRoleAsync("CORPORATE");
            var storeRole = await db.CreateRoleAsync("STORE");
            var corporate = await db.Get<ICorporateRepository>().InsertAsync(new Corporate { Name = "Access test" });
            var store = await db.CreateStoreAsync(corporate);
            var permission = await db.Get<IPermissionRepository>().InsertAsync(new Permission { Code = Guid.NewGuid().ToString("N"), Description = "Test" });
            var access = db.Get<IIdentityAccessRepository>();
            await db.Transactions.ExecuteAsync(async (c, t, ct) =>
            {
                var rp = await access.GrantRolePermissionAsync(globalRole, permission, c, t, ct);
                Assert.Equal(rp, await access.GrantRolePermissionAsync(globalRole, permission, c, t, ct));
                var ur = await access.GrantUserRoleAsync(user, globalRole, c, t, ct);
                Assert.Equal(ur, await access.GrantUserRoleAsync(user, globalRole, c, t, ct));
                var cm = await access.GrantCorporateMembershipAsync(user, corporate, corporateRole, c, t, ct);
                Assert.Equal(cm, await access.GrantCorporateMembershipAsync(user, corporate, corporateRole, c, t, ct));
                var sa = await access.GrantStoreAssignmentAsync(user, store, storeRole, c, t, ct);
                Assert.Equal(sa, await access.GrantStoreAssignmentAsync(user, store, storeRole, c, t, ct));
                Assert.Equal(permission, Assert.Single(await access.GetRolePermissionListAsync(globalRole, c, t, ct)).PermissionId);
                Assert.Equal("GLOBAL", Assert.Single(await access.GetUserRoleListAsync(user, c, t, ct)).RoleScope);
                Assert.Equal(corporate, Assert.Single(await access.GetCorporateMembershipListAsync(user, c, t, ct)).CorporateId);
                Assert.Equal("CORPORATE", Assert.Single(await access.GetCorporateMembershipListAsync(user, c, t, ct)).RoleScope);
                Assert.Equal(store, Assert.Single(await access.GetStoreAssignmentListAsync(user, c, t, ct)).StoreId);
                Assert.Equal("STORE", Assert.Single(await access.GetStoreAssignmentListAsync(user, c, t, ct)).RoleScope);
                await access.RevokeRolePermissionAsync(globalRole, permission, c, t, ct);
                await access.RevokeUserRoleAsync(user, globalRole, c, t, ct);
                await access.RevokeCorporateMembershipAsync(user, corporate, corporateRole, c, t, ct);
                await access.RevokeStoreAssignmentAsync(user, store, storeRole, c, t, ct);
                Assert.Empty(await access.GetRolePermissionListAsync(globalRole, c, t, ct));
                Assert.Empty(await access.GetUserRoleListAsync(user, c, t, ct));
                Assert.Empty(await access.GetCorporateMembershipListAsync(user, c, t, ct));
                Assert.Empty(await access.GetStoreAssignmentListAsync(user, c, t, ct));
                Assert.NotEqual(ur, await access.GrantUserRoleAsync(user, globalRole, c, t, ct));
                return true;
            });
        }

        [SqlServerFact]
        public async Task Wrong_role_scope_is_rejected_for_each_assignment_kind()
        {
            using var db = new IdentityDatabase();
            var user = await db.CreateUserAsync();
            var global = await db.CreateRoleAsync("GLOBAL");
            var storeRole = await db.CreateRoleAsync("STORE");
            var corporate = await db.Get<ICorporateRepository>().InsertAsync(new Corporate { Name = "Scope test" });
            var store = await db.CreateStoreAsync(corporate);
            var access = db.Get<IIdentityAccessRepository>();
            Assert.Equal(51002, (await Assert.ThrowsAsync<SqlException>(() => db.Transactions.ExecuteAsync((c, t, ct) => access.GrantUserRoleAsync(user, storeRole, c, t, ct)))).Number);
            Assert.Equal(51002, (await Assert.ThrowsAsync<SqlException>(() => db.Transactions.ExecuteAsync((c, t, ct) => access.GrantCorporateMembershipAsync(user, corporate, global, c, t, ct)))).Number);
            Assert.Equal(51002, (await Assert.ThrowsAsync<SqlException>(() => db.Transactions.ExecuteAsync((c, t, ct) => access.GrantStoreAssignmentAsync(user, store, global, c, t, ct)))).Number);
        }

        [SqlServerFact]
        public async Task Transaction_rollback_removes_both_user_and_role_assignment()
        {
            using var db = new IdentityDatabase();
            var role = await db.CreateRoleAsync("GLOBAL");
            var userId = Guid.NewGuid();
            var access = db.Get<IIdentityAccessRepository>();
            var users = db.Get<IUserRepository>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.Transactions.ExecuteAsync<bool>(async (c, t, ct) =>
            {
                await users.InsertAsync(new User { Id = userId, Email = $"{userId:N}@example.test", PasswordHash = "test-only-hash" }, c, t, ct);
                await access.GrantUserRoleAsync(userId, role, c, t, ct);
                throw new InvalidOperationException("Simulated failure before commit.");
            }));
            await db.Transactions.ExecuteAsync(async (c, t, ct) =>
            {
                Assert.Null(await users.GetByIdAsync(userId, c, t, ct));
                Assert.Empty(await access.GetUserRoleListAsync(userId, c, t, ct));
                return true;
            });
        }

        [SqlServerFact]
        public async Task Concurrent_grants_return_one_live_relationship()
        {
            using var db = new IdentityDatabase();
            var user = await db.CreateUserAsync();
            var role = await db.CreateRoleAsync("GLOBAL");
            var access = db.Get<IIdentityAccessRepository>();
            var grants = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => db.Transactions.ExecuteAsync((c, t, ct) => access.GrantUserRoleAsync(user, role, c, t, ct))));
            Assert.Single(grants.Distinct());
        }

        [SqlServerFact]
        public async Task Repository_rejects_a_transaction_from_another_connection()
        {
            using var db = new IdentityDatabase();
            await using var first = await db.Factory.OpenAsync();
            await using var second = await db.Factory.OpenAsync();
            await using var transaction = await first.BeginTransactionAsync();
            await Assert.ThrowsAsync<ArgumentException>(() => db.Get<IUserRepository>().GetByIdAsync(Guid.NewGuid(), second, transaction));
        }
    }
}
