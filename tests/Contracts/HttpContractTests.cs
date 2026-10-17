using System.Text.Json;
using System.Text.RegularExpressions;

namespace GiysiGo.Contracts.Tests
{
    public sealed class HttpContractTests
    {
        [Fact]
        public void Every_example_operation_has_resolvable_references_owner_security_and_path_parameters()
        {
            using var document = ReadContract();
            var root = document.RootElement;
            Assert.Equal("3.0.3", root.GetProperty("openapi").GetString());
            Assert.True(root.GetProperty("security")[0].TryGetProperty("bearerAuth", out _));
            AssertReferencesResolve(root, root);
            var operationIds = new HashSet<string>();

            foreach (var path in root.GetProperty("paths").EnumerateObject())
            {
                foreach (var method in path.Value.EnumerateObject())
                {
                    var operation = method.Value;
                    Assert.True(operationIds.Add(operation.GetProperty("operationId").GetString()!));
                    Assert.Contains(operation.GetProperty("x-owner-module").GetString(), new[] { "Identity", "Catalog", "Ordering", "Operations" });
                    if (operation.TryGetProperty("security", out var security) && security.GetArrayLength() == 0)
                    {
                        Assert.Equal("/auth/login", path.Name);
                    }

                    foreach (Match parameter in Regex.Matches(path.Name, @"\{([^}]+)\}"))
                    {
                        Assert.Contains(operation.GetProperty("parameters").EnumerateArray(), p =>
                            p.GetProperty("name").GetString() == parameter.Groups[1].Value &&
                            p.GetProperty("in").GetString() == "path" && p.GetProperty("required").GetBoolean());
                    }

                    foreach (var status in new[] { "400", "401", "403", "404", "409", "500" })
                    {
                        Assert.True(operation.GetProperty("responses").TryGetProperty(status, out _));
                    }
                }
            }
        }

        [Fact]
        public void Customer_commands_cannot_supply_actor_price_or_system_transition_fields()
        {
            using var document = ReadContract();
            var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
            foreach (var name in new[] { "AddressRequest", "CheckoutRequest", "QuoteDecisionRequest", "PaymentRequest" })
            {
                var schema = schemas.GetProperty(name);
                Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
                foreach (var field in new[] { "userId", "customerId", "source", "newStatus", "amount", "totalAmount", "role" })
                {
                    Assert.False(schema.GetProperty("properties").TryGetProperty(field, out _));
                }
            }
        }

        [Fact]
        public void Quote_permissions_are_separate_and_mock_routes_are_environment_limited()
        {
            using var document = ReadContract();
            var paths = document.RootElement.GetProperty("paths");
            Assert.Equal("quote.prepare", paths.GetProperty("/stores/{storeId}/quotes/{quoteId}/submit").GetProperty("post").GetProperty("x-required-permission").GetString());
            Assert.Equal("quote.approve", paths.GetProperty("/stores/{storeId}/quotes/{quoteId}/publish").GetProperty("post").GetProperty("x-required-permission").GetString());
            foreach (var path in paths.EnumerateObject().Where(p => p.Name.Contains("/mock-")))
            {
                var operation = path.Value.GetProperty("post");
                Assert.Equal(new[] { "Development", "Test" }, operation.GetProperty("x-environments").EnumerateArray().Select(v => v.GetString()));
                Assert.Equal("order.fulfill", operation.GetProperty("x-required-permission").GetString());
            }
        }

        private static JsonDocument ReadContract() => JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "mvp-contracts.json")));

        private static void AssertReferencesResolve(JsonElement node, JsonElement root)
        {
            if (node.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in node.EnumerateObject())
                {
                    if (property.Name == "$ref")
                    {
                        var reference = property.Value.GetString()!;
                        Assert.StartsWith("#/", reference);
                        var target = root;
                        foreach (var segment in reference[2..].Split('/'))
                        {
                            target = target.GetProperty(segment.Replace("~1", "/").Replace("~0", "~"));
                        }
                    }
                    else
                    {
                        AssertReferencesResolve(property.Value, root);
                    }
                }
            }
            else if (node.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in node.EnumerateArray()) AssertReferencesResolve(element, root);
            }
        }
    }
}
