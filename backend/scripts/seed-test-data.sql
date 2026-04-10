-- Test data seed for Marketplace (PostgreSQL)
-- Run inside docker via db-seed service.
--
-- Known test password for ALL seeded users:
--   Admin123!
-- ASP.NET Identity hash (PasswordHasher v3 / Identity):
--   AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==
--
-- Includes: global roles (Admin, Seller, Buyer, Moderator, User), Tech Store team roles,
-- products + inventory + sample reservation, second approved company (Home Comfort).

\set ON_ERROR_STOP on

BEGIN;

-- 1) Clean dependent data (order: children first; skip tables missing on older schemas)
DO $$
BEGIN
    IF to_regclass('public.inventory_reservations') IS NOT NULL THEN DELETE FROM inventory_reservations; END IF;
    IF to_regclass('public.stock_movements') IS NOT NULL THEN DELETE FROM stock_movements; END IF;
    IF to_regclass('public.warehouse_stocks') IS NOT NULL THEN DELETE FROM warehouse_stocks; END IF;
    IF to_regclass('public.warehouses') IS NOT NULL THEN DELETE FROM warehouses; END IF;
    IF to_regclass('public.product_images') IS NOT NULL THEN DELETE FROM product_images; END IF;
    IF to_regclass('public.product_details') IS NOT NULL THEN DELETE FROM product_details; END IF;
    IF to_regclass('public.products') IS NOT NULL THEN DELETE FROM products; END IF;
    IF to_regclass('public.company_members') IS NOT NULL THEN DELETE FROM company_members; END IF;
END $$;

DELETE FROM refresh_tokens;
DELETE FROM marketplace_users;
DELETE FROM companies;
DELETE FROM categories;
DELETE FROM "AspNetUserRoles";
DELETE FROM "AspNetRoles";
DELETE FROM "AspNetUsers";

-- 2) Reset categories identity sequence (if exists)
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_class WHERE relname = 'categories_Id_seq') THEN
        EXECUTE 'ALTER SEQUENCE "categories_Id_seq" RESTART WITH 1';
    END IF;
END $$;

-- 3) Seed Identity users (same PasswordHash = Admin123!)
INSERT INTO "AspNetUsers" (
    "Id",
    "AccessFailedCount",
    "ConcurrencyStamp",
    "Email",
    "EmailConfirmed",
    "IsDeleted",
    "LockoutEnabled",
    "LockoutEnd",
    "NormalizedEmail",
    "NormalizedUserName",
    "PasswordHash",
    "PhoneNumber",
    "PhoneNumberConfirmed",
    "SecurityStamp",
    "TelegramChatId",
    "TelegramLinkedAtUtc",
    "TelegramTwoFactorEnabled",
    "TwoFactorEnabled",
    "UserName"
) VALUES
(
    '11111111-1111-1111-1111-111111111111',
    0,
    'seed-admin-concurrency',
    'admin@marketplace.test',
    TRUE,
    FALSE,
    FALSE,
    NULL,
    'ADMIN@MARKETPLACE.TEST',
    'ADMIN',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000001',
    FALSE,
    'seed-admin-security',
    NULL,
    NULL,
    FALSE,
    FALSE,
    'admin'
),
(
    '22222222-2222-2222-2222-222222222222',
    0,
    'seed-seller-concurrency',
    'seller@marketplace.test',
    TRUE,
    FALSE,
    FALSE,
    NULL,
    'SELLER@MARKETPLACE.TEST',
    'SELLER',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000002',
    FALSE,
    'seed-seller-security',
    NULL,
    NULL,
    FALSE,
    FALSE,
    'seller'
),
(
    '33333333-3333-3333-3333-333333333333',
    0,
    'seed-buyer-concurrency',
    'buyer@marketplace.test',
    TRUE,
    FALSE,
    FALSE,
    NULL,
    'BUYER@MARKETPLACE.TEST',
    'BUYER',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000003',
    FALSE,
    'seed-buyer-security',
    NULL,
    NULL,
    FALSE,
    FALSE,
    'buyer'
),
(
    '44444444-4444-4444-4444-444444444444',
    0,
    'seed-mod-concurrency',
    'moderator@marketplace.test',
    TRUE,
    FALSE,
    FALSE,
    NULL,
    'MODERATOR@MARKETPLACE.TEST',
    'MODERATOR',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000004',
    FALSE,
    'seed-mod-security',
    NULL,
    NULL,
    FALSE,
    FALSE,
    'moderator'
),
(
    '55555555-5555-5555-5555-555555555555',
    0,
    'seed-plainuser-concurrency',
    'user@marketplace.test',
    TRUE,
    FALSE,
    FALSE,
    NULL,
    'USER@MARKETPLACE.TEST',
    'PLAINUSER',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000005',
    FALSE,
    'seed-plainuser-security',
    NULL,
    NULL,
    FALSE,
    FALSE,
    'plainuser'
),
(
    '66666666-6666-6666-6666-666666666666',
    0,
    'seed-manager-concurrency',
    'manager@marketplace.test',
    TRUE,
    FALSE,
    FALSE,
    NULL,
    'MANAGER@MARKETPLACE.TEST',
    'MANAGER',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000006',
    FALSE,
    'seed-manager-security',
    NULL,
    NULL,
    FALSE,
    FALSE,
    'manager'
),
(
    '77777777-7777-7777-7777-777777777777',
    0,
    'seed-seller2-concurrency',
    'seller2@marketplace.test',
    TRUE,
    FALSE,
    FALSE,
    NULL,
    'SELLER2@MARKETPLACE.TEST',
    'SELLER2',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000007',
    FALSE,
    'seed-seller2-security',
    NULL,
    NULL,
    FALSE,
    FALSE,
    'seller2'
),
(
    '88888888-8888-8888-8888-888888888888',
    0,
    'seed-support-concurrency',
    'support@marketplace.test',
    TRUE,
    FALSE,
    FALSE,
    NULL,
    'SUPPORT@MARKETPLACE.TEST',
    'SUPPORT',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000008',
    FALSE,
    'seed-support-security',
    NULL,
    NULL,
    FALSE,
    FALSE,
    'support'
),
(
    '99999999-9999-9999-9999-999999999999',
    0,
    'seed-logistics-concurrency',
    'logistics@marketplace.test',
    TRUE,
    FALSE,
    FALSE,
    NULL,
    'LOGISTICS@MARKETPLACE.TEST',
    'LOGISTICS',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000009',
    FALSE,
    'seed-logistics-security',
    NULL,
    NULL,
    FALSE,
    FALSE,
    'logistics'
);

-- 3.1) Seed Identity roles
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES
('aaaaaaaa-0000-0000-0000-000000000001', 'Admin', 'ADMIN', 'seed-role-admin'),
('aaaaaaaa-0000-0000-0000-000000000002', 'Seller', 'SELLER', 'seed-role-seller'),
('aaaaaaaa-0000-0000-0000-000000000003', 'Buyer', 'BUYER', 'seed-role-buyer'),
('aaaaaaaa-0000-0000-0000-000000000004', 'Moderator', 'MODERATOR', 'seed-role-moderator'),
('aaaaaaaa-0000-0000-0000-000000000005', 'User', 'USER', 'seed-role-user');

-- 3.2) Global role mapping (JWT / [Authorize(Roles=...)])
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
VALUES
('11111111-1111-1111-1111-111111111111', 'aaaaaaaa-0000-0000-0000-000000000001'),
('22222222-2222-2222-2222-222222222222', 'aaaaaaaa-0000-0000-0000-000000000002'),
('33333333-3333-3333-3333-333333333333', 'aaaaaaaa-0000-0000-0000-000000000003'),
('44444444-4444-4444-4444-444444444444', 'aaaaaaaa-0000-0000-0000-000000000004'),
('55555555-5555-5555-5555-555555555555', 'aaaaaaaa-0000-0000-0000-000000000005'),
('66666666-6666-6666-6666-666666666666', 'aaaaaaaa-0000-0000-0000-000000000002'),
('77777777-7777-7777-7777-777777777777', 'aaaaaaaa-0000-0000-0000-000000000002'),
('88888888-8888-8888-8888-888888888888', 'aaaaaaaa-0000-0000-0000-000000000003'),
('99999999-9999-9999-9999-999999999999', 'aaaaaaaa-0000-0000-0000-000000000003');

-- 4) marketplace_users (Role: Buyer=1, Seller=2, Moderator=3, Admin=4)
INSERT INTO marketplace_users (
    "Id",
    "FirstName",
    "LastName",
    "Role",
    "Birthday",
    "Avatar",
    "IsVerified",
    "VerificationDocument",
    "LastLoginAt",
    "CreatedAt",
    "UpdatedAt",
    "IsDeleted",
    "DeletedAt"
) VALUES
('11111111-1111-1111-1111-111111111111', 'Admin', 'User', 4, NULL, NULL, TRUE, NULL, NOW(), NOW(), NOW(), FALSE, NULL),
('22222222-2222-2222-2222-222222222222', 'Seller', 'One', 2, NULL, NULL, TRUE, NULL, NOW(), NOW(), NOW(), FALSE, NULL),
('33333333-3333-3333-3333-333333333333', 'Buyer', 'One', 1, NULL, NULL, TRUE, NULL, NOW(), NOW(), NOW(), FALSE, NULL),
('44444444-4444-4444-4444-444444444444', 'Mod', 'Erator', 3, NULL, NULL, TRUE, NULL, NOW(), NOW(), NOW(), FALSE, NULL),
('55555555-5555-5555-5555-555555555555', 'Plain', 'User', 1, NULL, NULL, TRUE, NULL, NOW(), NOW(), NOW(), FALSE, NULL),
('66666666-6666-6666-6666-666666666666', 'Company', 'Manager', 2, NULL, NULL, TRUE, NULL, NOW(), NOW(), NOW(), FALSE, NULL),
('77777777-7777-7777-7777-777777777777', 'Company', 'Seller', 2, NULL, NULL, TRUE, NULL, NOW(), NOW(), NOW(), FALSE, NULL),
('88888888-8888-8888-8888-888888888888', 'Support', 'Agent', 1, NULL, NULL, TRUE, NULL, NOW(), NOW(), NOW(), FALSE, NULL),
('99999999-9999-9999-9999-999999999999', 'Logistics', 'Lead', 1, NULL, NULL, TRUE, NULL, NOW(), NOW(), NOW(), FALSE, NULL);

-- 5) Categories (ProductCount removed in migration RemoveCountersFromDbModels)
INSERT INTO categories (
    "Name",
    "Slug",
    "ImageUrl",
    "ParentId",
    "Description",
    "MetaRaw",
    "SortOrder",
    "IsActive",
    "CreatedAt",
    "UpdatedAt",
    "IsDeleted",
    "DeletedAt"
) VALUES
(
    'Electronics',
    'electronics',
    NULL,
    NULL,
    'Devices and gadgets',
    '{"seed":true}',
    1,
    TRUE,
    NOW(),
    NOW(),
    FALSE,
    NULL
),
(
    'Smartphones',
    'smartphones',
    NULL,
    1,
    'Smartphones and accessories',
    '{"seed":true}',
    2,
    TRUE,
    NOW(),
    NOW(),
    FALSE,
    NULL
),
(
    'Home',
    'home',
    NULL,
    NULL,
    'Home and kitchen',
    '{"seed":true}',
    3,
    TRUE,
    NOW(),
    NOW(),
    FALSE,
    NULL
);

-- 6) Companies (legacy bigint + current uuid)
DO $$
DECLARE
    company_id_type text;
    has_contact_name_phone boolean;
BEGIN
    SELECT data_type
      INTO company_id_type
      FROM information_schema.columns
     WHERE table_schema = 'public'
       AND table_name = 'companies'
       AND column_name = 'Id';

    SELECT EXISTS (
        SELECT 1
          FROM information_schema.columns
         WHERE table_schema = 'public'
           AND table_name = 'companies'
           AND column_name = 'AddressFirstName'
    )
      INTO has_contact_name_phone;

    IF company_id_type = 'bigint' THEN
        IF has_contact_name_phone THEN
            EXECUTE $sql$
                INSERT INTO companies (
                    "Id","Name","Slug","Description","ImageUrl",
                    "ContactEmail","ContactPhone",
                    "AddressFirstName","AddressLastName","AddressStreet","AddressCity","AddressState","AddressPostalCode","AddressCountry","AddressPhone",
                    "IsApproved","ApprovedAt","ApprovedByUserId","Rating","MetaRaw","CreatedAt","UpdatedAt","IsDeleted","DeletedAt"
                ) VALUES
                (
                    1,'Tech Store','tech-store','Electronics seller',NULL,
                    'contact@tech-store.test','+380500000001',
                    'Tech','Store','Main street 1','Kyiv','Kyiv','01001','UA','+380500000001',
                    TRUE,NOW(),'11111111-1111-1111-1111-111111111111',4.7,'{"seed":true,"segment":"electronics"}',NOW(),NOW(),FALSE,NULL
                ),
                (
                    2,'Home Comfort','home-comfort','Home goods seller',NULL,
                    'hello@home-comfort.test','+380500000002',
                    'Home','Comfort','Comfort ave 7','Lviv','Lviv','79000','UA','+380500000002',
                    TRUE,NOW(),'11111111-1111-1111-1111-111111111111',4.2,'{"seed":true,"segment":"home"}',NOW(),NOW(),FALSE,NULL
                );
            $sql$;
        ELSE
            EXECUTE $sql$
                INSERT INTO companies (
                    "Id","Name","Slug","Description","ImageUrl",
                    "ContactEmail","ContactPhone",
                    "AddressStreet","AddressCity","AddressState","AddressPostalCode","AddressCountry",
                    "IsApproved","ApprovedAt","ApprovedByUserId","Rating","MetaRaw","CreatedAt","UpdatedAt","IsDeleted","DeletedAt"
                ) VALUES
                (
                    1,'Tech Store','tech-store','Electronics seller',NULL,
                    'contact@tech-store.test','+380500000001',
                    'Main street 1','Kyiv','Kyiv','01001','UA',
                    TRUE,NOW(),'11111111-1111-1111-1111-111111111111',4.7,'{"seed":true,"segment":"electronics"}',NOW(),NOW(),FALSE,NULL
                ),
                (
                    2,'Home Comfort','home-comfort','Home goods seller',NULL,
                    'hello@home-comfort.test','+380500000002',
                    'Comfort ave 7','Lviv','Lviv','79000','UA',
                    TRUE,NOW(),'11111111-1111-1111-1111-111111111111',4.2,'{"seed":true,"segment":"home"}',NOW(),NOW(),FALSE,NULL
                );
            $sql$;
        END IF;
    ELSE
        EXECUTE $sql$
            INSERT INTO companies (
                "Id","Name","Slug","Description","ImageUrl",
                "ContactEmail","ContactPhone",
                "AddressStreet","AddressCity","AddressState","AddressPostalCode","AddressCountry",
                "IsApproved","ApprovedAt","ApprovedByUserId","Rating","MetaRaw","CreatedAt","UpdatedAt","IsDeleted","DeletedAt"
            ) VALUES
            (
                'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','Tech Store','tech-store','Electronics seller',NULL,
                'contact@tech-store.test','+380500000001',
                'Main street 1','Kyiv','Kyiv','01001','UA',
                TRUE,NOW(),'11111111-1111-1111-1111-111111111111',4.7,'{"seed":true,"segment":"electronics"}',NOW(),NOW(),FALSE,NULL
            ),
            (
                'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb','Home Comfort','home-comfort','Home goods seller',NULL,
                'hello@home-comfort.test','+380500000002',
                'Comfort ave 7','Lviv','Lviv','79000','UA',
                TRUE,NOW(),'11111111-1111-1111-1111-111111111111',4.2,'{"seed":true,"segment":"home"}',NOW(),NOW(),FALSE,NULL
            );
        $sql$;
    END IF;
END $$;

-- 7) Members, catalog, inventory (uuid companies + migrated schema only)
DO $$
DECLARE
    cid_type text;
    has_company_members boolean;
BEGIN
    SELECT c.data_type
      INTO cid_type
      FROM information_schema.columns c
     WHERE c.table_schema = 'public'
       AND c.table_name = 'companies'
       AND c.column_name = 'Id';

    SELECT EXISTS (
        SELECT 1 FROM information_schema.tables
         WHERE table_schema = 'public' AND table_name = 'company_members'
    ) INTO has_company_members;

    IF cid_type = 'uuid' AND has_company_members THEN
    -- Tech Store: full role set (CompanyMembershipRole: Owner=0..Logistics=4)
    INSERT INTO company_members ("CompanyId", "UserId", "IsOwner", "Role", "PermissionsRaw", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt")
    VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '22222222-2222-2222-2222-222222222222', TRUE, 0, NULL, NOW(), NOW(), FALSE, NULL),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '66666666-6666-6666-6666-666666666666', FALSE, 1, NULL, NOW(), NOW(), FALSE, NULL),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '77777777-7777-7777-7777-777777777777', FALSE, 2, NULL, NOW(), NOW(), FALSE, NULL),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '88888888-8888-8888-8888-888888888888', FALSE, 3, NULL, NOW(), NOW(), FALSE, NULL),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '99999999-9999-9999-9999-999999999999', FALSE, 4, NULL, NOW(), NOW(), FALSE, NULL);

    -- Home Comfort: buyer as owner (second storefront demo)
    INSERT INTO company_members ("CompanyId", "UserId", "IsOwner", "Role", "PermissionsRaw", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt")
    VALUES
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '33333333-3333-3333-3333-333333333333', TRUE, 0, NULL, NOW(), NOW(), FALSE, NULL);

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'products') THEN
        INSERT INTO products (
            "Id", "CompanyId", "Name", "Slug", "Description", "Price", "OldPrice", "Stock", "MinStock", "CategoryId", "Status",
            "Rating", "ReviewCount", "ViewCount", "SalesCount", "HasVariants", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Seed Phone Alpha', 'seed-phone-alpha',
         'Demo smartphone for catalog + stock', 12999.00, 14999.00, 0, 5, 2, 1, 4.6, 12, 100, 3, FALSE, NOW(), NOW(), FALSE, NULL),
        (2, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Seed Laptop Beta', 'seed-laptop-beta',
         'Demo laptop', 45999.00, NULL, 0, 3, 1, 1, 4.8, 40, 500, 10, FALSE, NOW(), NOW(), FALSE, NULL),
        (3, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Seed Earbuds Gamma', 'seed-earbuds-gamma',
         'Demo earbuds', 1999.00, 2499.00, 0, 10, 2, 1, 4.3, 7, 80, 5, FALSE, NOW(), NOW(), FALSE, NULL),
        (4, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Seed Kettle Home', 'seed-kettle-home',
         'Demo kettle (second company)', 899.00, NULL, 0, 4, 3, 1, 4.5, 5, 30, 2, FALSE, NOW(), NOW(), FALSE, NULL);

        INSERT INTO product_details (
            "Id", "ProductId", "Slug", "AttributesRaw", "VariantsRaw", "SpecificationsRaw", "SeoRaw", "ContentBlocksRaw", "Tags", "Brands",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 1, 'seed-phone-alpha', '{"seed":true}'::jsonb, '{}'::jsonb, '{}'::jsonb, '{}'::jsonb, '{}'::jsonb,
         ARRAY['seed','phone']::text[], ARRAY['SeedBrand']::text[], NOW(), NOW(), FALSE, NULL),
        (2, 2, 'seed-laptop-beta', '{"seed":true}'::jsonb, '{}'::jsonb, '{}'::jsonb, '{}'::jsonb, '{}'::jsonb,
         ARRAY['seed','laptop']::text[], ARRAY['SeedBrand']::text[], NOW(), NOW(), FALSE, NULL),
        (3, 3, 'seed-earbuds-gamma', '{"seed":true}'::jsonb, '{}'::jsonb, '{}'::jsonb, '{}'::jsonb, '{}'::jsonb,
         ARRAY['audio']::text[], ARRAY['SeedBrand']::text[], NOW(), NOW(), FALSE, NULL),
        (4, 4, 'seed-kettle-home', '{"seed":true}'::jsonb, '{}'::jsonb, '{}'::jsonb, '{}'::jsonb, '{}'::jsonb,
         ARRAY['kitchen']::text[], ARRAY['HomeSeed']::text[], NOW(), NOW(), FALSE, NULL);

        INSERT INTO product_images (
            "Id", "ProductId", "ImageUrl", "ThumbnailUrl", "AltText", "SortOrder", "IsMain", "Width", "Height", "FileSize",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 1, 'https://placehold.co/600x400/png?text=Phone', 'https://placehold.co/200x200/png?text=Phone', 'Seed phone', 0, TRUE, 600, 400, NULL, NOW(), NOW(), FALSE, NULL),
        (2, 2, 'https://placehold.co/600x400/png?text=Laptop', 'https://placehold.co/200x200/png?text=Laptop', 'Seed laptop', 0, TRUE, 600, 400, NULL, NOW(), NOW(), FALSE, NULL),
        (3, 3, 'https://placehold.co/600x400/png?text=Earbuds', 'https://placehold.co/200x200/png?text=Earbuds', 'Seed earbuds', 0, TRUE, 600, 400, NULL, NOW(), NOW(), FALSE, NULL),
        (4, 4, 'https://placehold.co/600x400/png?text=Kettle', 'https://placehold.co/200x200/png?text=Kettle', 'Seed kettle', 0, TRUE, 600, 400, NULL, NOW(), NOW(), FALSE, NULL);

        IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'warehouses') THEN
        INSERT INTO warehouses (
            "Id", "CompanyId", "Name", "Code", "Street", "City", "State", "PostalCode", "Country", "TimeZone", "Priority",
            "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Kyiv Main', 'MAIN-KYIV', 'Skladna 1', 'Kyiv', 'Kyiv', '01001', 'UA', 'Europe/Kyiv', 10, TRUE, NOW(), NOW(), FALSE, NULL),
        (2, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Lviv Secondary', 'SEC-LVIV', 'Logistychna 2', 'Lviv', 'Lviv', '79000', 'UA', 'Europe/Kyiv', 20, TRUE, NOW(), NOW(), FALSE, NULL),
        (3, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Home WH Lviv', 'HOME-LVIV', 'Comfort 3', 'Lviv', 'Lviv', '79000', 'UA', 'Europe/Kyiv', 10, TRUE, NOW(), NOW(), FALSE, NULL);

        INSERT INTO warehouse_stocks (
            "Id", "CompanyId", "WarehouseId", "ProductId", "OnHand", "Reserved", "ReorderPoint", "Version", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 1, 50, 5, 10, 1, NOW(), NOW(), FALSE, NULL),
        (2, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 2, 15, 0, 3, 1, NOW(), NOW(), FALSE, NULL),
        (3, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2, 3, 200, 0, 20, 1, NOW(), NOW(), FALSE, NULL),
        (4, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 3, 4, 30, 2, 5, 1, NOW(), NOW(), FALSE, NULL);

        INSERT INTO stock_movements (
            "Id", "CompanyId", "WarehouseId", "ProductId", "Type", "Quantity", "OperationId", "Reference", "Reason", "ActorUserId",
            "OccurredAt", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 1, 0, 50, 'seed-tech-op-001', 'seed', NULL, '99999999-9999-9999-9999-999999999999', NOW(), NOW(), NOW(), FALSE, NULL),
        (2, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 2, 0, 15, 'seed-tech-op-002', 'seed', NULL, '99999999-9999-9999-9999-999999999999', NOW(), NOW(), NOW(), FALSE, NULL),
        (3, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2, 3, 0, 200, 'seed-tech-op-003', 'seed', NULL, '99999999-9999-9999-9999-999999999999', NOW(), NOW(), NOW(), FALSE, NULL),
        (4, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 3, 4, 0, 30, 'seed-home-op-001', 'seed', NULL, '33333333-3333-3333-3333-333333333333', NOW(), NOW(), NOW(), FALSE, NULL);

        INSERT INTO inventory_reservations (
            "Id", "CompanyId", "WarehouseId", "ProductId", "ReservationCode", "Quantity", "Status", "ExpiresAt", "Reference",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 1, 'SEED-RES-DEMO', 5, 0, NOW() + INTERVAL '1 day', 'seed-demo',
         NOW(), NOW(), FALSE, NULL);
        END IF;
    END IF;
    END IF;
END $$;

-- 8) Align identity sequences after explicit Ids
DO $$
DECLARE seq_name text;
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'products') THEN
        seq_name := pg_get_serial_sequence('products', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM products), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'product_details') THEN
        seq_name := pg_get_serial_sequence('product_details', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM product_details), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'product_images') THEN
        seq_name := pg_get_serial_sequence('product_images', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM product_images), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'warehouses') THEN
        seq_name := pg_get_serial_sequence('warehouses', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM warehouses), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'warehouse_stocks') THEN
        seq_name := pg_get_serial_sequence('warehouse_stocks', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM warehouse_stocks), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stock_movements') THEN
        seq_name := pg_get_serial_sequence('stock_movements', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM stock_movements), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'inventory_reservations') THEN
        seq_name := pg_get_serial_sequence('inventory_reservations', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM inventory_reservations), true);
        END IF;
    END IF;
END $$;

COMMIT;
