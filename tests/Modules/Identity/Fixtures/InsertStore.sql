DECLARE @Id uniqueidentifier = NEWID();
INSERT [catalog].stores (id, corporate_id, name, address_text, latitude, longitude, service_radius_km)
VALUES (@Id, @CorporateId, N'Identity test store', N'Test address', 41, 29, 5);
SELECT @Id;
