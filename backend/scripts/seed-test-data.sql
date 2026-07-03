-- Test data seed for Marketplace (PostgreSQL)
-- Run inside docker via db-seed service.
--
-- Known test password for ALL seeded users:
--   Admin123!
-- E2E helpers:
--   unverified@marketplace.test  (EmailConfirmed=false)
--   twofa@marketplace.test       (TwoFactorEnabled=true)
-- ASP.NET Identity hash (PasswordHasher v3 / Identity):
--   AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==
--
-- Includes: Identity + marketplace_users, categories, companies (uuid), members, contracts,
-- products (active + pending moderation), inventory, carts + cart_stock_watches,
-- orders + status history + payments/refunds, reviews,
-- P4 multi-warehouse (allocations, split stock, order 4), shipments,
-- finance (order_financials, seller_ledger, settlement_batches, seller_payouts, payout IBAN),
-- coupons, returns, chats,
-- notifications (in-app), push_subscriptions, outbox/inbox samples.

\set ON_ERROR_STOP on

BEGIN;

-- 1) Clean dependent data (order: children first; skip tables missing on older schemas)
DO $$
BEGIN
    IF to_regclass('public.notifications') IS NOT NULL THEN DELETE FROM notifications; END IF;
    IF to_regclass('public.push_subscriptions') IS NOT NULL THEN DELETE FROM push_subscriptions; END IF;
    IF to_regclass('public.cart_stock_watches') IS NOT NULL THEN DELETE FROM cart_stock_watches; END IF;
    IF to_regclass('public.http_idempotency_requests') IS NOT NULL THEN DELETE FROM http_idempotency_requests; END IF;
    IF to_regclass('public.inbox_messages') IS NOT NULL THEN DELETE FROM inbox_messages; END IF;
    IF to_regclass('public.outbox_messages') IS NOT NULL THEN DELETE FROM outbox_messages; END IF;
    IF to_regclass('public.order_status_history') IS NOT NULL THEN DELETE FROM order_status_history; END IF;
    IF to_regclass('public.inventory_reservations') IS NOT NULL THEN DELETE FROM inventory_reservations; END IF;
    IF to_regclass('public.review_replies') IS NOT NULL THEN DELETE FROM review_replies; END IF;
    IF to_regclass('public.company_reviews') IS NOT NULL THEN DELETE FROM company_reviews; END IF;
    IF to_regclass('public.product_reviews') IS NOT NULL THEN DELETE FROM product_reviews; END IF;
    IF to_regclass('public.refunds') IS NOT NULL THEN DELETE FROM refunds; END IF;
    IF to_regclass('public.seller_payouts') IS NOT NULL THEN DELETE FROM seller_payouts; END IF;
    IF to_regclass('public.seller_ledger_entries') IS NOT NULL THEN DELETE FROM seller_ledger_entries; END IF;
    IF to_regclass('public.settlement_batches') IS NOT NULL THEN DELETE FROM settlement_batches; END IF;
    IF to_regclass('public.order_financials') IS NOT NULL THEN DELETE FROM order_financials; END IF;
    IF to_regclass('public.shipping_events') IS NOT NULL THEN DELETE FROM shipping_events; END IF;
    IF to_regclass('public.shipment_items') IS NOT NULL THEN DELETE FROM shipment_items; END IF;
    IF to_regclass('public.shipments') IS NOT NULL THEN DELETE FROM shipments; END IF;
    IF to_regclass('public.order_fulfillment_allocations') IS NOT NULL THEN DELETE FROM order_fulfillment_allocations; END IF;
    IF to_regclass('public.return_line_items') IS NOT NULL THEN DELETE FROM return_line_items; END IF;
    IF to_regclass('public.return_requests') IS NOT NULL THEN DELETE FROM return_requests; END IF;
    IF to_regclass('public.chat_messages') IS NOT NULL THEN DELETE FROM chat_messages; END IF;
    IF to_regclass('public.chat_read_states') IS NOT NULL THEN DELETE FROM chat_read_states; END IF;
    IF to_regclass('public.chat_participants') IS NOT NULL THEN DELETE FROM chat_participants; END IF;
    IF to_regclass('public.chats') IS NOT NULL THEN DELETE FROM chats; END IF;
    IF to_regclass('public.coupon_usages') IS NOT NULL THEN DELETE FROM coupon_usages; END IF;
    IF to_regclass('public.coupons') IS NOT NULL THEN DELETE FROM coupons; END IF;
    IF to_regclass('public.payments') IS NOT NULL THEN DELETE FROM payments; END IF;
    IF to_regclass('public.order_addresses') IS NOT NULL THEN DELETE FROM order_addresses; END IF;
    IF to_regclass('public.order_items') IS NOT NULL THEN DELETE FROM order_items; END IF;
    IF to_regclass('public.orders') IS NOT NULL THEN DELETE FROM orders; END IF;
    IF to_regclass('public.favorites') IS NOT NULL THEN DELETE FROM favorites; END IF;
    IF to_regclass('public.cart_items') IS NOT NULL THEN DELETE FROM cart_items; END IF;
    IF to_regclass('public.carts') IS NOT NULL THEN DELETE FROM carts; END IF;
    IF to_regclass('public.stock_movements') IS NOT NULL THEN DELETE FROM stock_movements; END IF;
    IF to_regclass('public.warehouse_stocks') IS NOT NULL THEN DELETE FROM warehouse_stocks; END IF;
    IF to_regclass('public.warehouses') IS NOT NULL THEN DELETE FROM warehouses; END IF;
    IF to_regclass('public.product_images') IS NOT NULL THEN DELETE FROM product_images; END IF;
    IF to_regclass('public.product_details') IS NOT NULL THEN DELETE FROM product_details; END IF;
    IF to_regclass('public.products') IS NOT NULL THEN DELETE FROM products; END IF;
    IF to_regclass('public.company_commission_rates') IS NOT NULL THEN DELETE FROM company_commission_rates; END IF;
    IF to_regclass('public.company_contracts') IS NOT NULL THEN DELETE FROM company_contracts; END IF;
    IF to_regclass('public.company_legal_profiles') IS NOT NULL THEN DELETE FROM company_legal_profiles; END IF;
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
),
(
    'b0000001-0000-4000-8000-000000000001',
    0,
    'seed-unverified-concurrency',
    'unverified@marketplace.test',
    FALSE,
    FALSE,
    FALSE,
    NULL,
    'UNVERIFIED@MARKETPLACE.TEST',
    'UNVERIFIED',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000010',
    FALSE,
    'seed-unverified-security',
    NULL,
    NULL,
    FALSE,
    FALSE,
    'unverified'
),
(
    'b0000002-0000-4000-8000-000000000002',
    0,
    'seed-twofa-concurrency',
    'twofa@marketplace.test',
    TRUE,
    FALSE,
    FALSE,
    NULL,
    'TWOFA@MARKETPLACE.TEST',
    'TWOFA',
    'AQAAAAIAAYagAAAAECifUtBVLtNXahAxIRvpSE9DViQu9iH/f+mFrgdFrX99M/obrHlbrg1yXAS9PqjeFw==',
    '+380000000011',
    FALSE,
    'seed-twofa-security',
    NULL,
    NULL,
    FALSE,
    TRUE,
    'twofa'
);

-- 3.0) App notification preferences + Telegram (columns from AddAppNotificationUserPreferences)
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
         WHERE table_schema = 'public' AND table_name = 'AspNetUsers' AND column_name = 'NotifyAppByEmail'
    ) THEN
        UPDATE "AspNetUsers"
           SET "NotifyAppByEmail" = TRUE,
               "NotifyAppByTelegram" = TRUE;
        UPDATE "AspNetUsers"
           SET "TelegramChatId" = '900000001', "TelegramLinkedAtUtc" = NOW()
         WHERE "Id" = '11111111-1111-1111-1111-111111111111';
        UPDATE "AspNetUsers"
           SET "TelegramChatId" = '900000002', "TelegramLinkedAtUtc" = NOW()
         WHERE "Id" = '22222222-2222-2222-2222-222222222222';
        UPDATE "AspNetUsers"
           SET "TelegramChatId" = '900000004', "TelegramLinkedAtUtc" = NOW()
         WHERE "Id" = '44444444-4444-4444-4444-444444444444';
    END IF;
END $$;

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
('99999999-9999-9999-9999-999999999999', 'aaaaaaaa-0000-0000-0000-000000000003'),
('b0000001-0000-4000-8000-000000000001', 'aaaaaaaa-0000-0000-0000-000000000003'),
('b0000002-0000-4000-8000-000000000002', 'aaaaaaaa-0000-0000-0000-000000000003');

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
('99999999-9999-9999-9999-999999999999', 'Logistics', 'Lead', 1, NULL, NULL, TRUE, NULL, NOW(), NOW(), NOW(), FALSE, NULL),
('b0000001-0000-4000-8000-000000000001', 'Unverified', 'User', 1, NULL, NULL, FALSE, NULL, NOW(), NOW(), NOW(), FALSE, NULL),
('b0000002-0000-4000-8000-000000000002', 'TwoFactor', 'User', 1, NULL, NULL, TRUE, NULL, NOW(), NOW(), NOW(), FALSE, NULL);

-- 4.1) Refresh tokens
INSERT INTO refresh_tokens ("Id", "UserId", "TokenHash", "ExpiresAt", "CreatedAt", "RevokedAt", "ReplacedByTokenHash")
VALUES
(
    'aaaaaaaa-1111-1111-1111-111111111111',
    '33333333-3333-3333-3333-333333333333',
    '1111111111111111111111111111111111111111111111111111111111111111',
    NOW() + INTERVAL '30 days',
    NOW(),
    NULL,
    NULL
),
(
    'bbbbbbbb-2222-2222-2222-222222222222',
    '22222222-2222-2222-2222-222222222222',
    '2222222222222222222222222222222222222222222222222222222222222222',
    NOW() + INTERVAL '30 days',
    NOW(),
    NULL,
    NULL
);

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

        IF EXISTS (
            SELECT 1 FROM information_schema.columns
             WHERE table_schema = 'public' AND table_name = 'products' AND column_name = 'SubmittedByUserId'
        ) THEN
            INSERT INTO products (
                "Id", "CompanyId", "Name", "Slug", "Description", "Price", "OldPrice", "Stock", "MinStock", "CategoryId", "Status",
                "Rating", "ReviewCount", "ViewCount", "SalesCount", "HasVariants", "SubmittedByUserId", "ModerationRejectionReason",
                "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
            ) VALUES
            (5, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Seed Smartwatch Pending', 'seed-watch-pending',
             'Awaiting moderator approval', 5999.00, NULL, 0, 2, 2, 3, 0, 0, 0, 0, FALSE,
             '22222222-2222-2222-2222-222222222222', NULL, NOW(), NOW(), FALSE, NULL),
            (6, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Seed Tablet Rejected', 'seed-tablet-rejected',
             'Rejected listing example', 8999.00, NULL, 0, 2, 1, 0, 0, 0, 0, 0, FALSE,
             '77777777-7777-7777-7777-777777777777', 'Photos do not match product description.', NOW(), NOW(), FALSE, NULL);
        ELSE
            INSERT INTO products (
                "Id", "CompanyId", "Name", "Slug", "Description", "Price", "OldPrice", "Stock", "MinStock", "CategoryId", "Status",
                "Rating", "ReviewCount", "ViewCount", "SalesCount", "HasVariants", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
            ) VALUES
            (5, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Seed Smartwatch Pending', 'seed-watch-pending',
             'Awaiting moderator approval', 5999.00, NULL, 0, 2, 2, 3, 0, 0, 0, 0, FALSE, NOW(), NOW(), FALSE, NULL),
            (6, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Seed Tablet Rejected', 'seed-tablet-rejected',
             'Rejected listing example', 8999.00, NULL, 0, 2, 1, 0, 0, 0, 0, 0, FALSE, NOW(), NOW(), FALSE, NULL);
        END IF;

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
         ARRAY['kitchen']::text[], ARRAY['HomeSeed']::text[], NOW(), NOW(), FALSE, NULL),
        (5, 5, 'seed-watch-pending', '{"seed":true,"moderation":"pending"}'::jsonb, '{}'::jsonb, '{}'::jsonb, '{}'::jsonb, '{}'::jsonb,
         ARRAY['wearable']::text[], ARRAY['SeedBrand']::text[], NOW(), NOW(), FALSE, NULL),
        (6, 6, 'seed-tablet-rejected', '{"seed":true,"moderation":"rejected"}'::jsonb, '{}'::jsonb, '{}'::jsonb, '{}'::jsonb, '{}'::jsonb,
         ARRAY['tablet']::text[], ARRAY['SeedBrand']::text[], NOW(), NOW(), FALSE, NULL);

        INSERT INTO product_images (
            "Id", "ProductId", "ImageUrl", "ThumbnailUrl", "AltText", "SortOrder", "IsMain", "Width", "Height", "FileSize",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 1, 'https://placehold.co/600x400/png?text=Phone', 'https://placehold.co/200x200/png?text=Phone', 'Seed phone', 0, TRUE, 600, 400, NULL, NOW(), NOW(), FALSE, NULL),
        (2, 2, 'https://placehold.co/600x400/png?text=Laptop', 'https://placehold.co/200x200/png?text=Laptop', 'Seed laptop', 0, TRUE, 600, 400, NULL, NOW(), NOW(), FALSE, NULL),
        (3, 3, 'https://placehold.co/600x400/png?text=Earbuds', 'https://placehold.co/200x200/png?text=Earbuds', 'Seed earbuds', 0, TRUE, 600, 400, NULL, NOW(), NOW(), FALSE, NULL),
        (4, 4, 'https://placehold.co/600x400/png?text=Kettle', 'https://placehold.co/200x200/png?text=Kettle', 'Seed kettle', 0, TRUE, 600, 400, NULL, NOW(), NOW(), FALSE, NULL),
        (5, 5, 'https://placehold.co/600x400/png?text=Watch', 'https://placehold.co/200x200/png?text=Watch', 'Seed watch pending', 0, TRUE, 600, 400, NULL, NOW(), NOW(), FALSE, NULL),
        (6, 6, 'https://placehold.co/600x400/png?text=Tablet', 'https://placehold.co/200x200/png?text=Tablet', 'Seed tablet rejected', 0, TRUE, 600, 400, NULL, NOW(), NOW(), FALSE, NULL);

        IF EXISTS (
            SELECT 1 FROM information_schema.columns
             WHERE table_schema = 'public' AND table_name = 'product_images' AND column_name = 'ImageObjectKey'
        ) THEN
            UPDATE product_images SET
                "ImageObjectKey" = 'seed/products/' || "ProductId"::text || '/main.jpg',
                "OriginalObjectKey" = 'seed/products/' || "ProductId"::text || '/original.jpg',
                "ThumbnailObjectKey" = 'seed/products/' || "ProductId"::text || '/thumb.jpg'
             WHERE "ImageObjectKey" = '';
        END IF;

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
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 1, 50, 6, 10, 1, NOW(), NOW(), FALSE, NULL),
        (2, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 2, 0, 0, 3, 1, NOW(), NOW(), FALSE, NULL),
        (3, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2, 3, 200, 0, 20, 1, NOW(), NOW(), FALSE, NULL),
        (4, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 3, 4, 30, 2, 5, 1, NOW(), NOW(), FALSE, NULL),
        (5, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2, 1, 30, 1, 10, 1, NOW(), NOW(), FALSE, NULL);

        INSERT INTO stock_movements (
            "Id", "CompanyId", "WarehouseId", "ProductId", "Type", "Quantity", "OperationId", "Reference", "Reason", "ActorUserId",
            "OccurredAt", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 1, 0, 50, 'seed-tech-op-001', 'seed', NULL, '99999999-9999-9999-9999-999999999999', NOW(), NOW(), NOW(), FALSE, NULL),
        (2, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 2, 0, 0, 'seed-tech-op-002', 'seed-receive-zero', NULL, '99999999-9999-9999-9999-999999999999', NOW(), NOW(), NOW(), FALSE, NULL),
        (3, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2, 3, 0, 200, 'seed-tech-op-003', 'seed', NULL, '99999999-9999-9999-9999-999999999999', NOW(), NOW(), NOW(), FALSE, NULL),
        (4, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 3, 4, 0, 30, 'seed-home-op-001', 'seed', NULL, '33333333-3333-3333-3333-333333333333', NOW(), NOW(), NOW(), FALSE, NULL);

        INSERT INTO inventory_reservations (
            "Id", "CompanyId", "WarehouseId", "ProductId", "ReservationCode", "Quantity", "Status", "ExpiresAt", "Reference",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 1, 'SEED-RES-DEMO', 5, 0, NOW() + INTERVAL '1 day', 'seed-demo',
         NOW(), NOW(), FALSE, NULL),
        (2, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 1, 'order-4-product-1-wh-1', 1, 2, NOW() + INTERVAL '7 days', 'order-4',
         NOW(), NOW(), FALSE, NULL),
        (3, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2, 1, 'order-4-product-1-wh-2', 1, 2, NOW() + INTERVAL '7 days', 'order-4',
         NOW(), NOW(), FALSE, NULL);
        END IF;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'company_legal_profiles') THEN
        INSERT INTO company_legal_profiles (
            "Id", "CompanyId", "LegalName", "LegalType", "Edrpou", "Ipn", "CertificateNumber", "IsVatPayer",
            "InitialCommissionPercent", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Tech Store LLC', 1, '12345678', '123456789', 'TS-CERT-001', TRUE, 7.5000, NOW(), NOW(), FALSE, NULL),
        (2, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Home Comfort LLC', 1, '87654321', '987654321', 'HC-CERT-001', FALSE, 6.2500, NOW(), NOW(), FALSE, NULL);

        IF EXISTS (
            SELECT 1 FROM information_schema.columns
             WHERE table_schema = 'public' AND table_name = 'company_legal_profiles' AND column_name = 'PayoutIban'
        ) THEN
            UPDATE company_legal_profiles SET
                "PayoutIban" = 'UA213223130000026007233566001',
                "PayoutRecipientName" = 'Tech Store LLC'
             WHERE "CompanyId" = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
        END IF;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'company_contracts') THEN
        INSERT INTO company_contracts (
            "Id", "CompanyId", "ContractNumber", "Status", "EffectiveFrom", "EffectiveTo", "SignedAt", "Notes",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'CTR-TECH-2026-001', 1, NOW() - INTERVAL '30 days', NULL, NOW() - INTERVAL '35 days', 'Seed contract for Tech Store',
         NOW(), NOW(), FALSE, NULL),
        (2, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'CTR-HOME-2026-001', 1, NOW() - INTERVAL '20 days', NULL, NOW() - INTERVAL '22 days', 'Seed contract for Home Comfort',
         NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'company_commission_rates') THEN
        INSERT INTO company_commission_rates (
            "Id", "CompanyId", "ContractId", "CommissionPercent", "EffectiveFrom", "EffectiveTo", "Reason", "CreatedByUserId",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 7.5000, NOW() - INTERVAL '30 days', NULL, 'Initial seed commission', '11111111-1111-1111-1111-111111111111',
         NOW(), NOW(), FALSE, NULL),
        (2, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 2, 6.2500, NOW() - INTERVAL '20 days', NULL, 'Initial seed commission', '11111111-1111-1111-1111-111111111111',
         NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'carts') THEN
        INSERT INTO carts ("Id", "UserId", "Status", "LastActivityAt", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt")
        VALUES
        (1, '33333333-3333-3333-3333-333333333333', 0, NOW(), NOW(), NOW(), FALSE, NULL),
        (2, '11111111-1111-1111-1111-111111111111', 0, NOW() - INTERVAL '1 hour', NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'cart_items') THEN
        INSERT INTO cart_items ("Id", "CartId", "ProductId", "Quantity", "PriceAtMoment", "Discount", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt")
        VALUES
        (1, 1, 1, 1, 12999.00, 0, NOW(), NOW(), FALSE, NULL),
        (2, 1, 2, 1, 45999.00, 0, NOW(), NOW(), FALSE, NULL),
        (3, 1, 3, 2, 1999.00, 0, NOW(), NOW(), FALSE, NULL),
        (4, 2, 4, 1, 899.00, 0, NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'cart_stock_watches') THEN
        INSERT INTO cart_stock_watches ("Id", "UserId", "ProductId", "CreatedAtUtc", "LastNotifiedAtUtc")
        VALUES
        (1, '33333333-3333-3333-3333-333333333333', 2, NOW() - INTERVAL '2 days', NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'favorites') THEN
        INSERT INTO favorites ("Id", "UserId", "ProductId", "AddedAt", "PriceAtAdd", "IsAvailable", "NotificationsRaw", "MetaRaw", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt")
        VALUES
        (1, '33333333-3333-3333-3333-333333333333', 2, NOW(), 45999.00, FALSE, '{"priceDrop":true,"backInStock":true}', '{"seed":true}', NOW(), NOW(), FALSE, NULL),
        (2, '55555555-5555-5555-5555-555555555555', 4, NOW(), 899.00, TRUE, '{"priceDrop":false,"backInStock":true}', '{"seed":true}', NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'orders') THEN
        INSERT INTO orders (
            "Id", "OrderNumber", "CustomerId", "CompanyId", "Status", "TotalPrice", "Subtotal", "ShippingCost", "DiscountAmount", "TaxAmount",
            "ShippingMethodId", "PaymentMethod", "Notes", "TrackingNumber", "ShippedAt", "DeliveredAt", "CancelledAt", "RefundedAt",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'ORD-SEED-0001', '11111111-1111-1111-1111-111111111111', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2, 26098.00, 25998.00, 100.00, 0.00, 0.00,
         1, 2, 'Admin test order — Processing', NULL, NULL, NULL, NULL, NULL, NOW() - INTERVAL '5 days', NOW(), FALSE, NULL),
        (2, 'ORD-SEED-0002', '33333333-3333-3333-3333-333333333333', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 3, 14997.00, 14997.00, 0.00, 0.00, 0.00,
         1, 2, 'Buyer order — Shipped (phone + earbuds)', 'NP-SEED-0002', NOW() - INTERVAL '1 day', NULL, NULL, NULL, NOW() - INTERVAL '3 days', NOW(), FALSE, NULL),
        (3, 'ORD-SEED-0003', '33333333-3333-3333-3333-333333333333', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 4, 899.00, 799.00, 100.00, 0.00, 0.00,
         1, 2, 'Buyer order from Home Comfort — Delivered', 'NP-SEED-0003', NOW() - INTERVAL '7 days', NOW() - INTERVAL '2 days', NULL, NULL, NOW() - INTERVAL '10 days', NOW(), FALSE, NULL),
        (4, 'ORD-SEED-0004', '33333333-3333-3333-3333-333333333333', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 25998.00, 25998.00, 0.00, 0.00, 0.00,
         1, 2, 'Buyer order — Paid, split fulfillment (2x phone)', NULL, NULL, NULL, NULL, NULL, NOW() - INTERVAL '6 hours', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'order_items') THEN
        INSERT INTO order_items (
            "Id", "OrderId", "ProductId", "ProductName", "ProductImage", "Quantity", "PriceAtMoment", "Discount", "TotalPrice", "CompanyId",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 1, 1, 'Seed Phone Alpha', 'https://placehold.co/200x200/png?text=Phone', 2, 12999.00, 0.00, 25998.00, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
         NOW(), NOW(), FALSE, NULL),
        (2, 2, 1, 'Seed Phone Alpha', 'https://placehold.co/200x200/png?text=Phone', 1, 12999.00, 0.00, 12999.00, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
         NOW(), NOW(), FALSE, NULL),
        (3, 2, 3, 'Seed Earbuds Gamma', 'https://placehold.co/200x200/png?text=Earbuds', 1, 1999.00, 0.00, 1999.00, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
         NOW(), NOW(), FALSE, NULL),
        (4, 3, 4, 'Seed Kettle Home', 'https://placehold.co/200x200/png?text=Kettle', 1, 799.00, 0.00, 799.00, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
         NOW(), NOW(), FALSE, NULL),
        (5, 4, 1, 'Seed Phone Alpha', 'https://placehold.co/200x200/png?text=Phone', 2, 12999.00, 0.00, 25998.00, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
         NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'order_addresses') THEN
        INSERT INTO order_addresses (
            "Id", "OrderId", "Kind", "FirstName", "LastName", "Phone", "Street", "City", "State", "PostalCode", "Country",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 1, 0, 'Admin', 'User', '+380000000001', 'Delivery street 1', 'Kyiv', 'Kyiv', '01001', 'UA', NOW(), NOW(), FALSE, NULL),
        (2, 1, 1, 'Admin', 'User', '+380000000001', 'Billing street 2', 'Kyiv', 'Kyiv', '01002', 'UA', NOW(), NOW(), FALSE, NULL),
        (3, 2, 0, 'Buyer', 'One', '+380000000003', 'Buyer delivery 5', 'Kyiv', 'Kyiv', '01005', 'UA', NOW(), NOW(), FALSE, NULL),
        (4, 2, 1, 'Buyer', 'One', '+380000000003', 'Buyer billing 5', 'Kyiv', 'Kyiv', '01005', 'UA', NOW(), NOW(), FALSE, NULL),
        (5, 3, 0, 'Buyer', 'One', '+380000000003', 'Lviv delivery 12', 'Lviv', 'Lviv', '79001', 'UA', NOW(), NOW(), FALSE, NULL),
        (6, 3, 1, 'Buyer', 'One', '+380000000003', 'Lviv billing 12', 'Lviv', 'Lviv', '79001', 'UA', NOW(), NOW(), FALSE, NULL),
        (7, 4, 0, 'Buyer', 'One', '+380000000003', 'Kyiv delivery 8', 'Kyiv', 'Kyiv', '01008', 'UA', NOW(), NOW(), FALSE, NULL),
        (8, 4, 1, 'Buyer', 'One', '+380000000003', 'Kyiv billing 8', 'Kyiv', 'Kyiv', '01008', 'UA', NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'order_status_history') THEN
        INSERT INTO order_status_history (
            "Id", "OrderId", "OldStatus", "NewStatus", "Comment", "ChangedByUserId", "Source", "CorrelationId",
            "ChangedAt", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 1, 0, 1, 'Payment captured', '11111111-1111-1111-1111-111111111111', 'seed', 'seed-ord-1-paid', NOW() - INTERVAL '5 days', NOW(), NOW(), FALSE, NULL),
        (2, 1, 1, 2, 'Warehouse picked', '99999999-9999-9999-9999-999999999999', 'seed', 'seed-ord-1-processing', NOW() - INTERVAL '4 days', NOW(), NOW(), FALSE, NULL),
        (3, 2, 0, 1, 'Paid via LiqPay', '33333333-3333-3333-3333-333333333333', 'seed', 'seed-ord-2-paid', NOW() - INTERVAL '3 days', NOW(), NOW(), FALSE, NULL),
        (4, 2, 1, 2, 'Packing', '99999999-9999-9999-9999-999999999999', 'seed', 'seed-ord-2-processing', NOW() - INTERVAL '2 days', NOW(), NOW(), FALSE, NULL),
        (5, 2, 2, 3, 'Handed to carrier', '99999999-9999-9999-9999-999999999999', 'seed', 'seed-ord-2-shipped', NOW() - INTERVAL '1 day', NOW(), NOW(), FALSE, NULL),
        (6, 3, 0, 1, 'Paid', '33333333-3333-3333-3333-333333333333', 'seed', 'seed-ord-3-paid', NOW() - INTERVAL '10 days', NOW(), NOW(), FALSE, NULL),
        (7, 3, 1, 2, 'Processing', '33333333-3333-3333-3333-333333333333', 'seed', 'seed-ord-3-processing', NOW() - INTERVAL '8 days', NOW(), NOW(), FALSE, NULL),
        (8, 3, 2, 3, 'Shipped', '99999999-9999-9999-9999-999999999999', 'seed', 'seed-ord-3-shipped', NOW() - INTERVAL '7 days', NOW(), NOW(), FALSE, NULL),
        (9, 3, 3, 4, 'Delivered', '99999999-9999-9999-9999-999999999999', 'seed', 'seed-ord-3-delivered', NOW() - INTERVAL '2 days', NOW(), NOW(), FALSE, NULL),
        (10, 4, 0, 1, 'Paid via LiqPay', '33333333-3333-3333-3333-333333333333', 'seed', 'seed-ord-4-paid', NOW() - INTERVAL '6 hours', NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'payments') THEN
        INSERT INTO payments (
            "Id", "OrderId", "PaymentMethod", "Amount", "Currency", "TransactionId", "Status", "ProviderResponseRaw", "ProcessedAt",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 1, 2, 26098.00, 'UAH', 'liqpay-seed-transaction-001', 1, '{"provider":"liqpay","seed":true,"status":"success"}', NOW() - INTERVAL '5 days', NOW(), NOW(), FALSE, NULL),
        (2, 2, 2, 14997.00, 'UAH', 'liqpay-seed-transaction-002', 1, '{"provider":"liqpay","seed":true,"status":"success"}', NOW() - INTERVAL '3 days', NOW(), NOW(), FALSE, NULL),
        (3, 3, 2, 899.00, 'UAH', 'liqpay-seed-transaction-003', 1, '{"provider":"liqpay","seed":true,"status":"success"}', NOW() - INTERVAL '10 days', NOW(), NOW(), FALSE, NULL),
        (4, 4, 2, 25998.00, 'UAH', 'liqpay-seed-transaction-004', 1, '{"provider":"liqpay","seed":true,"status":"success"}', NOW() - INTERVAL '6 hours', NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'refunds') THEN
        INSERT INTO refunds (
            "Id", "PaymentId", "OrderId", "Amount", "Reason", "Status", "ProcessedByUserId", "ProcessedAt", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 1, 1, 500.00, 'Seed partial refund', 1, '11111111-1111-1111-1111-111111111111', NOW(), NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'order_fulfillment_allocations') THEN
        INSERT INTO order_fulfillment_allocations (
            "Id", "OrderId", "OrderItemId", "CompanyId", "WarehouseId", "ProductId", "Quantity", "ReservationId", "Status",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 4, 5, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 1, 1, 2, 2, NOW(), NOW(), FALSE, NULL),
        (2, 4, 5, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2, 1, 1, 3, 2, NOW(), NOW(), FALSE, NULL),
        (3, 2, 2, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 1, 1, NULL, 3, NOW() - INTERVAL '1 day', NOW(), FALSE, NULL),
        (4, 2, 3, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2, 3, 1, NULL, 3, NOW() - INTERVAL '1 day', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'shipments') THEN
        INSERT INTO shipments (
            "Id", "OrderId", "CustomerId", "ShipmentNumber", "ShippingMethodId", "WarehouseId", "CarrierCode", "Status",
            "TrackingNumber", "LastSyncedAtUtc", "RawPayload", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 2, '33333333-3333-3333-3333-333333333333', 1, 1, 1, 0, 2,
         'NP-SEED-0002-A', NOW() - INTERVAL '1 day', '{"seed":true,"warehouse":"MAIN-KYIV"}', NOW() - INTERVAL '1 day', NOW(), FALSE, NULL),
        (2, 2, '33333333-3333-3333-3333-333333333333', 2, 1, 2, 0, 2,
         'NP-SEED-0002-B', NOW() - INTERVAL '1 day', '{"seed":true,"warehouse":"SEC-LVIV"}', NOW() - INTERVAL '1 day', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'shipment_items') THEN
        INSERT INTO shipment_items ("Id", "ShipmentId", "OrderItemId", "Quantity", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt")
        VALUES
        (1, 1, 2, 1, NOW() - INTERVAL '1 day', NOW(), FALSE, NULL),
        (2, 2, 3, 1, NOW() - INTERVAL '1 day', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'shipping_events') THEN
        INSERT INTO shipping_events (
            "Id", "CarrierCode", "EventKey", "PayloadHash", "RawPayload", "ReceivedAtUtc", "ShipmentId", "OrderId",
            "TrackingNumber", "DeliveryStatus", "OccurredAtUtc", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 0, 'seed-np-0002-a-shipped', 'hash-seed-np-0002-a', '{"status":"in_transit","seed":true}', NOW() - INTERVAL '1 day',
         1, 2, 'NP-SEED-0002-A', 2, NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day', NOW(), FALSE, NULL),
        (2, 0, 'seed-np-0002-b-shipped', 'hash-seed-np-0002-b', '{"status":"in_transit","seed":true}', NOW() - INTERVAL '1 day',
         2, 2, 'NP-SEED-0002-B', 2, NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'order_financials') THEN
        INSERT INTO order_financials (
            "Id", "OrderId", "PaymentId", "CompanyId", "Currency", "MerchandiseSubtotal", "DiscountAmount", "MerchandiseBase",
            "CommissionPercent", "PlatformFee", "SellerMerchandiseNet", "ShippingAmount", "SellerPayoutEligible",
            "PostedAtUtc", "CreatedAt", "UpdatedAt"
        ) VALUES
        (1, 2, 2, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'UAH', 14997.00, 0.00, 14997.00, 7.5000, 1124.78, 13872.22, 0.00, 13872.22,
         NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days', NOW()),
        (2, 3, 3, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'UAH', 799.00, 0.00, 799.00, 6.2500, 49.94, 749.06, 100.00, 849.06,
         NOW() - INTERVAL '10 days', NOW() - INTERVAL '10 days', NOW());
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'settlement_batches') THEN
        INSERT INTO settlement_batches (
            "Id", "CompanyId", "PeriodStartUtc", "PeriodEndUtc", "Status", "TotalAmount", "Currency",
            "ClosedAtUtc", "PaidAtUtc", "CreatedAt", "UpdatedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', NOW() - INTERVAL '14 days', NOW() - INTERVAL '7 days', 2, 13872.22, 'UAH',
         NOW() - INTERVAL '7 days', NULL, NOW() - INTERVAL '14 days', NOW()),
        (2, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', NOW() - INTERVAL '21 days', NOW() - INTERVAL '14 days', 4, 849.06, 'UAH',
         NOW() - INTERVAL '14 days', NOW() - INTERVAL '13 days', NOW() - INTERVAL '21 days', NOW()),
        (3, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', NOW() - INTERVAL '7 days', NOW() - INTERVAL '1 day', 5, 0.00, 'UAH',
         NOW() - INTERVAL '1 day', NULL, NOW() - INTERVAL '7 days', NOW());
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'seller_payouts') THEN
        INSERT INTO seller_payouts (
            "Id", "CompanyId", "SettlementBatchId", "Status", "Amount", "Currency", "ProviderReference",
            "Iban", "RecipientName", "InitiatedAtUtc", "CompletedAtUtc", "FailureReason", "CreatedAt", "UpdatedAt"
        ) VALUES
        (1, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 2, 3, 849.06, 'UAH', 'MANUAL-SEED-001',
         NULL, 'Home Comfort LLC', NOW() - INTERVAL '14 days', NOW() - INTERVAL '13 days', NULL, NOW() - INTERVAL '14 days', NOW());
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'seller_ledger_entries') THEN
        INSERT INTO seller_ledger_entries (
            "Id", "CompanyId", "OrderId", "OrderFinancialsId", "SettlementBatchId", "SellerPayoutId",
            "EntryType", "Status", "Amount", "Currency", "Description", "AvailableAtUtc", "SettledAtUtc", "CreatedAt", "UpdatedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2, 1, 1, NULL, 1, 2, 13872.22, 'UAH', 'Sale for order ORD-SEED-0002',
         NOW() - INTERVAL '1 day', NULL, NOW() - INTERVAL '3 days', NOW()),
        (2, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2, 1, 1, NULL, 2, 2, 1124.78, 'UAH', 'Platform fee for order ORD-SEED-0002',
         NOW() - INTERVAL '1 day', NULL, NOW() - INTERVAL '3 days', NOW()),
        (3, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 3, 2, 2, 1, 1, 3, 849.06, 'UAH', 'Sale for order ORD-SEED-0003',
         NOW() - INTERVAL '2 days', NOW() - INTERVAL '13 days', NOW() - INTERVAL '10 days', NOW()),
        (4, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 3, 2, 2, 1, 2, 3, 49.94, 'UAH', 'Platform fee for order ORD-SEED-0003',
         NOW() - INTERVAL '2 days', NOW() - INTERVAL '13 days', NOW() - INTERVAL '10 days', NOW()),
        (5, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, NULL, NULL, NULL, 3, 2, -500.00, 'UAH', 'Seed partial refund',
         NOW(), NULL, NOW(), NOW());
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'coupons') THEN
        INSERT INTO coupons (
            "Id", "Code", "Description", "DiscountAmount", "DiscountType", "MinOrderAmount", "UsageLimit", "UsageCount",
            "UserUsageLimit", "ExpiresAtUtc", "StartsAtUtc", "ApplicableCategoriesRaw", "ApplicableProductsRaw",
            "ApplicableCompaniesRaw", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'SEED10', 'Seed 10% off Tech Store', 10.00, 0, NULL, 100, 0, 1,
         NOW() + INTERVAL '365 days', NOW() - INTERVAL '30 days', NULL, NULL,
         '["aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"]'::jsonb, TRUE, NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'return_requests') THEN
        INSERT INTO return_requests (
            "Id", "OrderId", "CustomerId", "CompanyId", "Status", "ReasonCode", "Comment",
            "ApprovedByUserId", "RejectedReason", "ReceivedAtUtc", "RefundId", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 3, '33333333-3333-3333-3333-333333333333', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 0, 2,
         'Received wrong kettle model', NULL, NULL, NULL, NULL, NOW() - INTERVAL '1 day', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'return_line_items') THEN
        INSERT INTO return_line_items ("Id", "ReturnRequestId", "OrderItemId", "Quantity", "Reason", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt")
        VALUES (1, 1, 4, 1, 'WrongItem', NOW() - INTERVAL '1 day', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'chats') THEN
        INSERT INTO chats (
            "Id", "Type", "Status", "InitiatorUserId", "OrderId", "ProductId", "LastMessageText", "LastMessageSenderId",
            "LastMessageCreatedAt", "IsActive", "Meta", "ParticipantsSnapshot", "RawPayload", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        ('c1000001-0000-4000-8000-000000000001', 2, 0, '33333333-3333-3333-3333-333333333333', 2, NULL,
         'When will the earbuds shipment arrive?', '33333333-3333-3333-3333-333333333333', NOW() - INTERVAL '20 hours',
         TRUE, '{"seed":true}', NULL, NULL, NOW() - INTERVAL '1 day', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'chat_participants') THEN
        INSERT INTO chat_participants (
            "ChatId", "UserId", "Role", "CompanyId", "JoinedAt", "LeftAt", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        ('c1000001-0000-4000-8000-000000000001', '33333333-3333-3333-3333-333333333333', 0, NULL, NOW() - INTERVAL '1 day', NULL, NOW() - INTERVAL '1 day', NOW(), FALSE, NULL),
        ('c1000001-0000-4000-8000-000000000001', '22222222-2222-2222-2222-222222222222', 1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', NOW() - INTERVAL '1 day', NULL, NOW() - INTERVAL '1 day', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'chat_messages') THEN
        INSERT INTO chat_messages (
            "Id", "ChatId", "SenderId", "Text", "Attachments", "Status", "ReadAt", "DeletedBy", "ReplyToMessageId",
            "RawPayload", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'c1000001-0000-4000-8000-000000000001', '33333333-3333-3333-3333-333333333333',
         'Hi, I see two tracking numbers for my order ORD-SEED-0002.', '[]', 0, NULL, '{}', NULL, NULL,
         NOW() - INTERVAL '22 hours', NOW(), FALSE, NULL),
        (2, 'c1000001-0000-4000-8000-000000000001', '33333333-3333-3333-3333-333333333333',
         'When will the earbuds shipment arrive?', '[]', 0, NULL, '{}', NULL, NULL,
         NOW() - INTERVAL '20 hours', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'chat_read_states') THEN
        INSERT INTO chat_read_states ("ChatId", "UserId", "LastReadMessageId", "UpdatedAt")
        VALUES
        ('c1000001-0000-4000-8000-000000000001', '22222222-2222-2222-2222-222222222222', 1, NOW() - INTERVAL '18 hours');
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'product_reviews') THEN
        INSERT INTO product_reviews (
            "Id", "ProductId", "UserId", "UserName", "UserAvatar", "Rating", "Title", "Comment", "ImagesRaw", "ProsRaw", "ConsRaw",
            "IsVerifiedPurchase", "OrderId", "HelpfulRaw", "Status", "ModeratedByUserId", "ModeratedAt", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 1, '11111111-1111-1111-1111-111111111111', 'Admin User', NULL, 5, 'Great demo phone', 'Works well in tests',
         '[]'::jsonb, '["battery"]'::jsonb, '[]'::jsonb, TRUE, 1, '{"up":3,"down":0}'::jsonb, 1, '44444444-4444-4444-4444-444444444444', NOW(), NOW(), NOW(), FALSE, NULL),
        (2, 4, '55555555-5555-5555-5555-555555555555', 'Plain User', NULL, 4, 'Good kettle', 'Heats quickly',
         '[]'::jsonb, '["fast"]'::jsonb, '["short-cable"]'::jsonb, FALSE, NULL, '{"up":1,"down":0}'::jsonb, 1, '44444444-4444-4444-4444-444444444444', NOW(), NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'company_reviews') THEN
        INSERT INTO company_reviews (
            "Id", "CompanyId", "UserId", "UserName", "OrderId", "IsVerifiedPurchase", "RatingsRaw", "OverallRating", "Comment",
            "Status", "ModeratedByUserId", "ModeratedAt", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', 'Admin User', 1, TRUE,
         '{"service":5,"shipping":4}'::jsonb, 4.50, 'Fast support and delivery', 1, '44444444-4444-4444-4444-444444444444', NOW(), NOW(), NOW(), FALSE, NULL),
        (2, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '55555555-5555-5555-5555-555555555555', 'Plain User', NULL, FALSE,
         '{"service":4,"quality":4}'::jsonb, 4.00, 'Nice assortment', 1, '44444444-4444-4444-4444-444444444444', NOW(), NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'review_replies') THEN
        INSERT INTO review_replies (
            "Id", "ProductReviewId", "CompanyReviewId", "CompanyId", "AuthorUserId", "Body", "IsEdited",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, 1, NULL, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', 'Thanks for your feedback on the phone!', FALSE,
         NOW(), NOW(), FALSE, NULL),
        (2, NULL, 1, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', 'We appreciate your review and will keep improving.', FALSE,
         NOW(), NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'notifications') THEN
        INSERT INTO notifications (
            "Id", "UserId", "Type", "Title", "Message", "Data", "ActionUrl", "IsRead", "ReadAt", "ExpiresAt", "RawPayload", "CorrelationId",
            "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
        ) VALUES
        (1, '33333333-3333-3333-3333-333333333333', 0, 'Замовлення відправлено',
         'Ваше замовлення ORD-SEED-0002 передано перевізнику.',
         '{"templateKey":"UserOrderStatus","jobCorrelationId":"b1000001-0000-4000-8000-000000000001","orderId":2,"status":"Shipped"}'::jsonb,
         '/me/orders/2', FALSE, NULL, NOW() + INTERVAL '90 days',
         '{"templateKey":"UserOrderStatus","orderId":2}'::jsonb,
         'b1000001-0000-4000-8000-000000000001', NOW() - INTERVAL '1 day', NOW(), FALSE, NULL),
        (2, '33333333-3333-3333-3333-333333333333', 0, 'Товар знову в наявності',
         'Seed Laptop Beta знову доступний для замовлення.',
         '{"templateKey":"CartProductBackInStock","jobCorrelationId":"b1000002-0000-4000-8000-000000000002","productId":2,"slug":"seed-laptop-beta"}'::jsonb,
         '/catalog/products/seed-laptop-beta', TRUE, NOW() - INTERVAL '12 hours', NOW() + INTERVAL '90 days',
         '{"templateKey":"CartProductBackInStock","productId":2}'::jsonb,
         'b1000002-0000-4000-8000-000000000002', NOW() - INTERVAL '2 days', NOW(), FALSE, NULL),
        (3, '11111111-1111-1111-1111-111111111111', 0, 'Нове замовлення',
         'Надійшло замовлення ORD-SEED-0001 у Tech Store.',
         '{"templateKey":"AdminNewOrder","jobCorrelationId":"a1000001-0000-4000-8000-000000000001","orderId":1}'::jsonb,
         '/admin/orders/1', FALSE, NULL, NOW() + INTERVAL '90 days',
         '{"templateKey":"AdminNewOrder","orderId":1}'::jsonb,
         'a1000001-0000-4000-8000-000000000001', NOW() - INTERVAL '5 days', NOW(), FALSE, NULL),
        (4, '44444444-4444-4444-4444-444444444444', 3, 'Товар на модерації',
         'Seed Smartwatch Pending очікує перевірки.',
         '{"templateKey":"AdminProductPendingReview","jobCorrelationId":"a1000002-0000-4000-8000-000000000002","productId":5}'::jsonb,
         '/admin/products/pending', FALSE, NULL, NOW() + INTERVAL '90 days',
         '{"templateKey":"AdminProductPendingReview","productId":5}'::jsonb,
         'a1000002-0000-4000-8000-000000000002', NOW() - INTERVAL '6 hours', NOW(), FALSE, NULL),
        (5, '22222222-2222-2222-2222-222222222222', 3, 'Товар схвалено',
         'Ваш товар Seed Smartwatch Pending опубліковано.',
         '{"templateKey":"UserProductApproved","jobCorrelationId":"a1000003-0000-4000-8000-000000000003","productId":5}'::jsonb,
         '/companies/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/products/5', TRUE, NOW() - INTERVAL '1 hour', NOW() + INTERVAL '90 days',
         '{"templateKey":"UserProductApproved","productId":5}'::jsonb,
         'a1000003-0000-4000-8000-000000000003', NOW() - INTERVAL '3 hours', NOW(), FALSE, NULL),
        (6, '77777777-7777-7777-7777-777777777777', 3, 'Товар відхилено',
         'Seed Tablet Rejected не пройшов модерацію.',
         '{"templateKey":"UserProductRejected","jobCorrelationId":"a1000004-0000-4000-8000-000000000004","productId":6,"reason":"Photos do not match product description."}'::jsonb,
         NULL, FALSE, NULL, NOW() + INTERVAL '90 days',
         '{"templateKey":"UserProductRejected","productId":6}'::jsonb,
         'a1000004-0000-4000-8000-000000000004', NOW() - INTERVAL '1 day', NOW(), FALSE, NULL),
        (7, '22222222-2222-2222-2222-222222222222', 0, 'Нове замовлення у вашій компанії',
         'Покупець оформив ORD-SEED-0002.',
         '{"templateKey":"CompanyNewOrder","jobCorrelationId":"c1000001-0000-4000-8000-000000000005","orderId":2,"companyId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"}'::jsonb,
         '/companies/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/orders/2', FALSE, NULL, NOW() + INTERVAL '90 days',
         '{"templateKey":"CompanyNewOrder","orderId":2}'::jsonb,
         'c1000001-0000-4000-8000-000000000005', NOW() - INTERVAL '3 days', NOW(), FALSE, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'push_subscriptions') THEN
        INSERT INTO push_subscriptions ("Id", "UserId", "Endpoint", "P256dh", "Auth", "AudienceFlags", "UserAgent", "CreatedAtUtc", "LastUsedAtUtc")
        VALUES
        (1, '33333333-3333-3333-3333-333333333333',
         'https://fcm.googleapis.com/fcm/send/seed-buyer-endpoint-0001',
         'BNcRdreALRieniP3kFbOqLzc2v6Ip5z0eNc0iR12fHcKzv',
         'tBHIjz7k0DFYKBJ6FFeq2g',
         1, 'Mozilla/5.0 (Seed Browser)', NOW() - INTERVAL '7 days', NOW() - INTERVAL '1 day'),
        (2, '11111111-1111-1111-1111-111111111111',
         'https://fcm.googleapis.com/fcm/send/seed-admin-endpoint-0001',
         'BKxRdreALRieniP3kFbOqLzc2v6Ip5z0eNc0iR12fHcKzv',
         'x8Yz7k0DFYKBJ6FFeq2g',
         3, 'Mozilla/5.0 (Seed Admin)', NOW() - INTERVAL '14 days', NOW() - INTERVAL '2 hours'),
        (3, '44444444-4444-4444-4444-444444444444',
         'https://updates.push.services.mozilla.com/wpush/v2/seed-mod-endpoint',
         'BModeratorPushKeyExample0000000000000000001',
         'authModeratorSeed0001',
         2, 'Mozilla/5.0 (Seed Moderator)', NOW() - INTERVAL '3 days', NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'outbox_messages') THEN
        INSERT INTO outbox_messages (
            "Id", "AggregateType", "AggregateId", "EventType", "Payload", "OccurredAtUtc", "ProcessedAtUtc",
            "Attempts", "LastError", "NextAttemptAtUtc", "DeadLetteredAtUtc", "DeadLetterReason", "DeadLetterCategory"
        ) VALUES
        ('f0000001-0000-0000-0000-000000000001', 'Order', '2', 'OrderStatusChanged',
         '{"orderId":2,"oldStatus":2,"newStatus":3,"seed":true}', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day',
         1, NULL, NULL, NULL, NULL, NULL),
        ('f0000002-0000-0000-0000-000000000002', 'Inventory', '1', 'StockReceived',
         '{"productId":1,"warehouseId":1,"quantity":10,"seed":true}', NOW() - INTERVAL '30 minutes', NULL,
         0, NULL, NULL, NULL, NULL, NULL);
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'inbox_messages') THEN
        INSERT INTO inbox_messages ("MessageId", "Consumer", "ProcessedAtUtc", "Metadata")
        VALUES
        ('f0000001-0000-0000-0000-000000000001', 'OutboxEventProcessor', NOW() - INTERVAL '1 day', '{"seed":true}');
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'http_idempotency_requests') THEN
        INSERT INTO http_idempotency_requests (
            "Scope", "IdempotencyKey", "RequestHash", "Status", "ResponseStatusCode", "ResponseBodyJson",
            "CreatedAtUtc", "ExpiresAtUtc", "CompletedAtUtc"
        ) VALUES
        ('POST:/me/cart/checkout', 'seed-checkout-key-001', 'hash-seed-checkout-001', 'Completed', 200,
         '{"orderIds":[2],"seed":true}', NOW() - INTERVAL '3 days', NOW() + INTERVAL '24 hours', NOW() - INTERVAL '3 days');
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
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'company_legal_profiles') THEN
        seq_name := pg_get_serial_sequence('company_legal_profiles', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM company_legal_profiles), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'company_contracts') THEN
        seq_name := pg_get_serial_sequence('company_contracts', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM company_contracts), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'company_commission_rates') THEN
        seq_name := pg_get_serial_sequence('company_commission_rates', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM company_commission_rates), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'carts') THEN
        seq_name := pg_get_serial_sequence('carts', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM carts), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'cart_items') THEN
        seq_name := pg_get_serial_sequence('cart_items', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM cart_items), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'favorites') THEN
        seq_name := pg_get_serial_sequence('favorites', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM favorites), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'orders') THEN
        seq_name := pg_get_serial_sequence('orders', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM orders), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'order_items') THEN
        seq_name := pg_get_serial_sequence('order_items', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM order_items), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'order_addresses') THEN
        seq_name := pg_get_serial_sequence('order_addresses', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM order_addresses), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'payments') THEN
        seq_name := pg_get_serial_sequence('payments', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM payments), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'refunds') THEN
        seq_name := pg_get_serial_sequence('refunds', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM refunds), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'product_reviews') THEN
        seq_name := pg_get_serial_sequence('product_reviews', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM product_reviews), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'company_reviews') THEN
        seq_name := pg_get_serial_sequence('company_reviews', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM company_reviews), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'review_replies') THEN
        seq_name := pg_get_serial_sequence('review_replies', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM review_replies), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'order_status_history') THEN
        seq_name := pg_get_serial_sequence('order_status_history', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM order_status_history), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'notifications') THEN
        seq_name := pg_get_serial_sequence('notifications', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM notifications), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'push_subscriptions') THEN
        seq_name := pg_get_serial_sequence('push_subscriptions', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM push_subscriptions), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'cart_stock_watches') THEN
        seq_name := pg_get_serial_sequence('cart_stock_watches', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM cart_stock_watches), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'order_fulfillment_allocations') THEN
        seq_name := pg_get_serial_sequence('order_fulfillment_allocations', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM order_fulfillment_allocations), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'shipments') THEN
        seq_name := pg_get_serial_sequence('shipments', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM shipments), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'shipment_items') THEN
        seq_name := pg_get_serial_sequence('shipment_items', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM shipment_items), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'shipping_events') THEN
        seq_name := pg_get_serial_sequence('shipping_events', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM shipping_events), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'order_financials') THEN
        seq_name := pg_get_serial_sequence('order_financials', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM order_financials), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'settlement_batches') THEN
        seq_name := pg_get_serial_sequence('settlement_batches', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM settlement_batches), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'seller_payouts') THEN
        seq_name := pg_get_serial_sequence('seller_payouts', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM seller_payouts), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'seller_ledger_entries') THEN
        seq_name := pg_get_serial_sequence('seller_ledger_entries', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM seller_ledger_entries), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'coupons') THEN
        seq_name := pg_get_serial_sequence('coupons', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM coupons), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'return_requests') THEN
        seq_name := pg_get_serial_sequence('return_requests', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM return_requests), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'return_line_items') THEN
        seq_name := pg_get_serial_sequence('return_line_items', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM return_line_items), true);
        END IF;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'chat_messages') THEN
        seq_name := pg_get_serial_sequence('chat_messages', 'Id');
        IF seq_name IS NOT NULL THEN
            PERFORM setval(seq_name, (SELECT COALESCE(MAX("Id"), 1) FROM chat_messages), true);
        END IF;
    END IF;
END $$;

COMMIT;
