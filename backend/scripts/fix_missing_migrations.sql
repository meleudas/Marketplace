-- Idempotent repair for migrations that were not registered/applied in __EFMigrationsHistory.
-- Safe to re-run.

ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS "DeadLetterCategory" character varying(64);
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS "DeadLetteredAtUtc" timestamp with time zone;
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS "DeadLetterReason" character varying(2000);

CREATE INDEX IF NOT EXISTS "IX_outbox_messages_DeadLetteredAtUtc" ON outbox_messages ("DeadLetteredAtUtc");

CREATE TABLE IF NOT EXISTS http_idempotency_requests (
    "Scope" character varying(256) NOT NULL,
    "IdempotencyKey" character varying(128) NOT NULL,
    "RequestHash" character varying(128) NOT NULL,
    "Status" character varying(32) NOT NULL,
    "ResponseStatusCode" integer,
    "ResponseBodyJson" character varying(16000),
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "ExpiresAtUtc" timestamp with time zone NOT NULL,
    "CompletedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_http_idempotency_requests" PRIMARY KEY ("Scope", "IdempotencyKey")
);

CREATE INDEX IF NOT EXISTS "IX_http_idempotency_requests_CompletedAtUtc" ON http_idempotency_requests ("CompletedAtUtc");
CREATE INDEX IF NOT EXISTS "IX_http_idempotency_requests_ExpiresAtUtc" ON http_idempotency_requests ("ExpiresAtUtc");

ALTER TABLE products ADD COLUMN IF NOT EXISTS "ModerationRejectionReason" character varying(2000);
ALTER TABLE products ADD COLUMN IF NOT EXISTS "SubmittedByUserId" uuid;

ALTER TABLE "AspNetUsers" ADD COLUMN IF NOT EXISTS "NotifyAppByEmail" boolean NOT NULL DEFAULT TRUE;
ALTER TABLE "AspNetUsers" ADD COLUMN IF NOT EXISTS "NotifyAppByTelegram" boolean NOT NULL DEFAULT TRUE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES
    ('20260417120000_AddCompanyContractsAndCommissions', '10.0.5'),
    ('20260417153000_AddPaymentsAndRefunds', '10.0.5'),
    ('20260427103000_AddHttpIdempotencyAndOutboxDeadLetter', '10.0.5'),
    ('20260510200000_AddProductModerationColumns', '10.0.5'),
    ('20260511120000_AddAppNotificationUserPreferences', '10.0.5')
ON CONFLICT ("MigrationId") DO NOTHING;
