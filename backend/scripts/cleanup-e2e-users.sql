-- Remove disposable E2E users created during Playwright runs (@example.test domain).
-- Seed users use @marketplace.test and are not affected.
\set ON_ERROR_STOP on

BEGIN;

DO $$
DECLARE
    uid uuid;
BEGIN
    FOR uid IN
        SELECT "Id" FROM "AspNetUsers" WHERE "Email" LIKE '%@example.test'
    LOOP
        IF to_regclass('public.refresh_tokens') IS NOT NULL THEN
            DELETE FROM refresh_tokens WHERE "UserId" = uid;
        END IF;
        IF to_regclass('public.push_subscriptions') IS NOT NULL THEN
            DELETE FROM push_subscriptions WHERE "UserId" = uid;
        END IF;
        IF to_regclass('public.notifications') IS NOT NULL THEN
            DELETE FROM notifications WHERE "UserId" = uid;
        END IF;
        DELETE FROM "AspNetUserRoles" WHERE "UserId" = uid;
        DELETE FROM "AspNetUserClaims" WHERE "UserId" = uid;
        DELETE FROM "AspNetUserLogins" WHERE "UserId" = uid;
        DELETE FROM "AspNetUserTokens" WHERE "UserId" = uid;
        IF to_regclass('public.marketplace_users') IS NOT NULL THEN
            DELETE FROM marketplace_users WHERE "Id" = uid;
        END IF;
    END LOOP;

    DELETE FROM "AspNetUsers" WHERE "Email" LIKE '%@example.test';
END $$;

COMMIT;
