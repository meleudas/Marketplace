-- Test data seed for Marketplace (PostgreSQL)
-- Run inside docker via db-seed service.
--
-- Known test password for seeded users:
--   Admin123!
-- ASP.NET Identity hash generated from this password:
--   AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==

\set ON_ERROR_STOP on

BEGIN;

-- 1) Clean dependent data first
DELETE FROM refresh_tokens;
DELETE FROM marketplace_users;
DELETE FROM companies;
DELETE FROM categories;
DELETE FROM "AspNetUserRoles";
DELETE FROM "AspNetRoles";

-- Remove all app users except technical/system if needed.
-- For local test DB we clear fully.
DELETE FROM "AspNetUsers";

-- 2) Reset categories identity sequence (if exists)
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_class WHERE relname = 'categories_Id_seq') THEN
        EXECUTE 'ALTER SEQUENCE "categories_Id_seq" RESTART WITH 1';
    END IF;
END $$;

-- 3) Seed Identity users (minimal required fields)
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
);

-- 3.1) Seed Identity roles
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES
('aaaaaaaa-0000-0000-0000-000000000001', 'Admin', 'ADMIN', 'seed-role-admin'),
('aaaaaaaa-0000-0000-0000-000000000002', 'Seller', 'SELLER', 'seed-role-seller'),
('aaaaaaaa-0000-0000-0000-000000000003', 'Buyer', 'BUYER', 'seed-role-buyer'),
('aaaaaaaa-0000-0000-0000-000000000004', 'Moderator', 'MODERATOR', 'seed-role-moderator'),
('aaaaaaaa-0000-0000-0000-000000000005', 'User', 'USER', 'seed-role-user');

-- 3.2) Seed Identity user-role mapping (critical for [Authorize(Roles=...)])
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
VALUES
('11111111-1111-1111-1111-111111111111', 'aaaaaaaa-0000-0000-0000-000000000001'), -- admin user -> Admin
('22222222-2222-2222-2222-222222222222', 'aaaaaaaa-0000-0000-0000-000000000002'), -- seller user -> Seller
('33333333-3333-3333-3333-333333333333', 'aaaaaaaa-0000-0000-0000-000000000003'); -- buyer user -> Buyer

-- 4) Seed domain users (linked 1:1 to AspNetUsers by same Id)
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
(
    '11111111-1111-1111-1111-111111111111',
    'Admin',
    'User',
    4, -- Admin
    NULL,
    NULL,
    TRUE,
    NULL,
    NOW(),
    NOW(),
    NOW(),
    FALSE,
    NULL
),
(
    '22222222-2222-2222-2222-222222222222',
    'Seller',
    'One',
    2, -- Seller
    NULL,
    NULL,
    TRUE,
    NULL,
    NOW(),
    NOW(),
    NOW(),
    FALSE,
    NULL
),
(
    '33333333-3333-3333-3333-333333333333',
    'Buyer',
    'One',
    1, -- Buyer
    NULL,
    NULL,
    TRUE,
    NULL,
    NOW(),
    NOW(),
    NOW(),
    FALSE,
    NULL
);

-- 5) Seed categories
INSERT INTO categories (
    "Name",
    "Slug",
    "ImageUrl",
    "ParentId",
    "Description",
    "MetaRaw",
    "SortOrder",
    "IsActive",
    "ProductCount",
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
    12,
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
    5,
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
    3,
    NOW(),
    NOW(),
    FALSE,
    NULL
);

-- 6) Seed companies.
-- Supports both schemas:
--  a) old: bigint Id + AddressFirstName/AddressLastName/AddressPhone
--  b) new: uuid Id + only address fields
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
                    "IsApproved","ApprovedAt","ApprovedByUserId","Rating","ReviewCount","FollowerCount","MetaRaw","CreatedAt","UpdatedAt","IsDeleted","DeletedAt"
                ) VALUES
                (
                    1,'Tech Store','tech-store','Electronics seller',NULL,
                    'contact@tech-store.test','+380500000001',
                    'Tech','Store','Main street 1','Kyiv','Kyiv','01001','UA','+380500000001',
                    TRUE,NOW(),'11111111-1111-1111-1111-111111111111',4.7,42,120,'{"seed":true,"segment":"electronics"}',NOW(),NOW(),FALSE,NULL
                ),
                (
                    2,'Home Comfort','home-comfort','Home goods seller',NULL,
                    'hello@home-comfort.test','+380500000002',
                    'Home','Comfort','Comfort ave 7','Lviv','Lviv','79000','UA','+380500000002',
                    FALSE,NULL,NULL,NULL,0,8,'{"seed":true,"segment":"home"}',NOW(),NOW(),FALSE,NULL
                );
            $sql$;
        ELSE
            EXECUTE $sql$
                INSERT INTO companies (
                    "Id","Name","Slug","Description","ImageUrl",
                    "ContactEmail","ContactPhone",
                    "AddressStreet","AddressCity","AddressState","AddressPostalCode","AddressCountry",
                    "IsApproved","ApprovedAt","ApprovedByUserId","Rating","ReviewCount","FollowerCount","MetaRaw","CreatedAt","UpdatedAt","IsDeleted","DeletedAt"
                ) VALUES
                (
                    1,'Tech Store','tech-store','Electronics seller',NULL,
                    'contact@tech-store.test','+380500000001',
                    'Main street 1','Kyiv','Kyiv','01001','UA',
                    TRUE,NOW(),'11111111-1111-1111-1111-111111111111',4.7,42,120,'{"seed":true,"segment":"electronics"}',NOW(),NOW(),FALSE,NULL
                ),
                (
                    2,'Home Comfort','home-comfort','Home goods seller',NULL,
                    'hello@home-comfort.test','+380500000002',
                    'Comfort ave 7','Lviv','Lviv','79000','UA',
                    FALSE,NULL,NULL,NULL,0,8,'{"seed":true,"segment":"home"}',NOW(),NOW(),FALSE,NULL
                );
            $sql$;
        END IF;
    ELSE
        -- uuid id schema
        EXECUTE $sql$
            INSERT INTO companies (
                "Id","Name","Slug","Description","ImageUrl",
                "ContactEmail","ContactPhone",
                "AddressStreet","AddressCity","AddressState","AddressPostalCode","AddressCountry",
                "IsApproved","ApprovedAt","ApprovedByUserId","Rating","ReviewCount","FollowerCount","MetaRaw","CreatedAt","UpdatedAt","IsDeleted","DeletedAt"
            ) VALUES
            (
                'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','Tech Store','tech-store','Electronics seller',NULL,
                'contact@tech-store.test','+380500000001',
                'Main street 1','Kyiv','Kyiv','01001','UA',
                TRUE,NOW(),'11111111-1111-1111-1111-111111111111',4.7,42,120,'{"seed":true,"segment":"electronics"}',NOW(),NOW(),FALSE,NULL
            ),
            (
                'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb','Home Comfort','home-comfort','Home goods seller',NULL,
                'hello@home-comfort.test','+380500000002',
                'Comfort ave 7','Lviv','Lviv','79000','UA',
                FALSE,NULL,NULL,NULL,0,8,'{"seed":true,"segment":"home"}',NOW(),NOW(),FALSE,NULL
            );
        $sql$;
    END IF;
END $$;

COMMIT;
