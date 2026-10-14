CREATE TABLE "Actions" (
    "Id" uuid NOT NULL,
    "MandateNumber" integer,
    "Title" text NOT NULL,
    "Description" text NOT NULL,
    "Phase" text NOT NULL,
    "Priority" integer NOT NULL,
    "OwnerId" text NOT NULL,
    "Participants" text NOT NULL,
    "StartDate" date,
    "DueDate" date NOT NULL,
    "OriginalDueDate" date NOT NULL,
    "DeadlineLabel" text NOT NULL,
    "Deliverable" text NOT NULL,
    "Status" text NOT NULL,
    "Progress" integer NOT NULL,
    "CompletedAt" timestamp with time zone,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "MeetingId" uuid,
    "DependsOnId" uuid,
    "KpiId" uuid,
    "Version" integer NOT NULL,
    CONSTRAINT "PK_Actions" PRIMARY KEY ("Id")
);


CREATE TABLE "AdvisorTurns" (
    "Id" uuid NOT NULL,
    "Question" text NOT NULL,
    "Answer" text NOT NULL,
    "At" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_AdvisorTurns" PRIMARY KEY ("Id")
);


CREATE TABLE "AlertRules" (
    "Id" uuid NOT NULL,
    "Kind" text NOT NULL,
    "Label" text NOT NULL,
    "Enabled" boolean NOT NULL,
    "DaysBefore" text NOT NULL,
    "RepeatEveryDays" integer NOT NULL,
    "EscalateAfterDays" integer NOT NULL,
    "Email" boolean NOT NULL,
    "Recipients" text NOT NULL,
    "SendHour" integer NOT NULL,
    CONSTRAINT "PK_AlertRules" PRIMARY KEY ("Id")
);


CREATE TABLE "AspNetRoles" (
    "Id" text NOT NULL,
    "Name" character varying(256),
    "NormalizedName" character varying(256),
    "ConcurrencyStamp" text,
    CONSTRAINT "PK_AspNetRoles" PRIMARY KEY ("Id")
);


CREATE TABLE "AspNetUsers" (
    "Id" text NOT NULL,
    "PersonId" text NOT NULL,
    "DisplayName" text NOT NULL,
    "UserName" character varying(256),
    "NormalizedUserName" character varying(256),
    "Email" character varying(256),
    "NormalizedEmail" character varying(256),
    "EmailConfirmed" boolean NOT NULL,
    "PasswordHash" text,
    "SecurityStamp" text,
    "ConcurrencyStamp" text,
    "PhoneNumber" text,
    "PhoneNumberConfirmed" boolean NOT NULL,
    "TwoFactorEnabled" boolean NOT NULL,
    "LockoutEnd" timestamp with time zone,
    "LockoutEnabled" boolean NOT NULL,
    "AccessFailedCount" integer NOT NULL,
    CONSTRAINT "PK_AspNetUsers" PRIMARY KEY ("Id")
);


CREATE TABLE "Comments" (
    "Id" uuid NOT NULL,
    "ActionId" uuid NOT NULL,
    "Author" text NOT NULL,
    "Text" text NOT NULL,
    "At" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Comments" PRIMARY KEY ("Id")
);


CREATE TABLE "Decisions" (
    "Id" uuid NOT NULL,
    "Request" text NOT NULL,
    "ToPersonId" text NOT NULL,
    "RequestedAt" timestamp with time zone NOT NULL,
    "DecidedAt" timestamp with time zone,
    "Outcome" text NOT NULL,
    "MeetingId" uuid,
    "RequestedBy" text NOT NULL,
    CONSTRAINT "PK_Decisions" PRIMARY KEY ("Id")
);


CREATE TABLE "Gate" (
    "Id" uuid NOT NULL,
    "Number" integer NOT NULL,
    "Title" text NOT NULL,
    "OwnerId" text NOT NULL,
    "DueDate" date NOT NULL,
    "Confirmed" boolean NOT NULL,
    "ConfirmedBy" text NOT NULL,
    "ConfirmedAt" timestamp with time zone,
    CONSTRAINT "PK_Gate" PRIMARY KEY ("Id")
);


CREATE TABLE "Kpis" (
    "Id" uuid NOT NULL,
    "Code" text NOT NULL,
    "Name" text NOT NULL,
    "Unit" text NOT NULL,
    "Target" numeric,
    "TargetDate" date NOT NULL,
    "InterimTarget" numeric,
    "InterimDate" date,
    "OwnerId" text NOT NULL,
    "Calculated" boolean NOT NULL,
    CONSTRAINT "PK_Kpis" PRIMARY KEY ("Id")
);


CREATE TABLE "Meetings" (
    "Id" uuid NOT NULL,
    "Title" text NOT NULL,
    "ScheduledAt" timestamp with time zone NOT NULL,
    "Participants" text NOT NULL,
    "Agenda" text NOT NULL,
    "Minutes" text NOT NULL,
    "MandateAccepted" boolean NOT NULL,
    CONSTRAINT "PK_Meetings" PRIMARY KEY ("Id")
);


CREATE TABLE "Notifications" (
    "Id" uuid NOT NULL,
    "PersonId" text NOT NULL,
    "Key" text NOT NULL,
    "Title" text NOT NULL,
    "Link" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "Read" boolean NOT NULL,
    "WantsEmail" boolean NOT NULL,
    "EmailedAt" timestamp with time zone,
    "LastAttemptAt" timestamp with time zone,
    "Attempts" integer NOT NULL,
    "EmailStatus" text NOT NULL,
    CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id")
);


CREATE TABLE "People" (
    "Id" text NOT NULL,
    "Name" text NOT NULL,
    "Area" text NOT NULL,
    CONSTRAINT "PK_People" PRIMARY KEY ("Id")
);


CREATE TABLE "PrivateNotes" (
    "Id" uuid NOT NULL,
    "Text" text NOT NULL,
    "At" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_PrivateNotes" PRIMARY KEY ("Id")
);


CREATE TABLE "Processes" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "OwnerId" text NOT NULL,
    "Internal" boolean NOT NULL,
    "Mapped" boolean NOT NULL,
    "Automated" boolean NOT NULL,
    "Optimized" boolean NOT NULL,
    "Evidence" text NOT NULL,
    "VerifiedOn" date,
    CONSTRAINT "PK_Processes" PRIMARY KEY ("Id")
);


CREATE TABLE "Reports" (
    "Id" uuid NOT NULL,
    "Month" text NOT NULL,
    "Snapshot" text NOT NULL,
    "ExecutiveSummary" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Reports" PRIMARY KEY ("Id")
);


CREATE TABLE "Settings" (
    "Key" text NOT NULL,
    "Value" text NOT NULL,
    CONSTRAINT "PK_Settings" PRIMARY KEY ("Key")
);


CREATE TABLE "Changes" (
    "Id" uuid NOT NULL,
    "ActionId" uuid NOT NULL,
    "Actor" text NOT NULL,
    "Description" text NOT NULL,
    "At" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Changes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Changes_Actions_ActionId" FOREIGN KEY ("ActionId") REFERENCES "Actions" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Subtasks" (
    "Id" uuid NOT NULL,
    "ActionId" uuid NOT NULL,
    "Title" text NOT NULL,
    "Done" boolean NOT NULL,
    CONSTRAINT "PK_Subtasks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Subtasks_Actions_ActionId" FOREIGN KEY ("ActionId") REFERENCES "Actions" ("Id") ON DELETE CASCADE
);


CREATE TABLE "AspNetRoleClaims" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "RoleId" text NOT NULL,
    "ClaimType" text,
    "ClaimValue" text,
    CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
);


CREATE TABLE "AspNetUserClaims" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "UserId" text NOT NULL,
    "ClaimType" text,
    "ClaimValue" text,
    CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);


CREATE TABLE "AspNetUserLogins" (
    "LoginProvider" text NOT NULL,
    "ProviderKey" text NOT NULL,
    "ProviderDisplayName" text,
    "UserId" text NOT NULL,
    CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
    CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);


CREATE TABLE "AspNetUserRoles" (
    "UserId" text NOT NULL,
    "RoleId" text NOT NULL,
    CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);


CREATE TABLE "AspNetUserTokens" (
    "UserId" text NOT NULL,
    "LoginProvider" text NOT NULL,
    "Name" text NOT NULL,
    "Value" text,
    CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Measurements" (
    "Id" uuid NOT NULL,
    "KpiId" uuid NOT NULL,
    "Value" numeric NOT NULL,
    "MeasuredOn" date NOT NULL,
    "Source" text NOT NULL,
    "Comment" text NOT NULL,
    "RecordedBy" text NOT NULL,
    CONSTRAINT "PK_Measurements" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Measurements_Kpis_KpiId" FOREIGN KEY ("KpiId") REFERENCES "Kpis" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Evidence" (
    "Id" uuid NOT NULL,
    "ActionId" uuid,
    "GateId" uuid,
    "MeasurementId" uuid,
    "Name" text NOT NULL,
    "StoredName" text NOT NULL,
    "MimeType" text NOT NULL,
    "Size" bigint NOT NULL,
    "UploadedBy" text NOT NULL,
    "UploadedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Evidence" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Evidence_Actions_ActionId" FOREIGN KEY ("ActionId") REFERENCES "Actions" ("Id"),
    CONSTRAINT "FK_Evidence_Gate_GateId" FOREIGN KEY ("GateId") REFERENCES "Gate" ("Id"),
    CONSTRAINT "FK_Evidence_Measurements_MeasurementId" FOREIGN KEY ("MeasurementId") REFERENCES "Measurements" ("Id")
);


CREATE UNIQUE INDEX "IX_Actions_MandateNumber" ON "Actions" ("MandateNumber");


CREATE INDEX "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");


CREATE UNIQUE INDEX "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");


CREATE INDEX "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");


CREATE INDEX "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");


CREATE INDEX "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");


CREATE INDEX "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");


CREATE UNIQUE INDEX "IX_AspNetUsers_PersonId" ON "AspNetUsers" ("PersonId");


CREATE UNIQUE INDEX "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");


CREATE INDEX "IX_Changes_ActionId" ON "Changes" ("ActionId");


CREATE INDEX "IX_Evidence_ActionId" ON "Evidence" ("ActionId");


CREATE INDEX "IX_Evidence_GateId" ON "Evidence" ("GateId");


CREATE INDEX "IX_Evidence_MeasurementId" ON "Evidence" ("MeasurementId");


CREATE INDEX "IX_Measurements_KpiId" ON "Measurements" ("KpiId");


CREATE UNIQUE INDEX "IX_Notifications_PersonId_Key" ON "Notifications" ("PersonId", "Key");


CREATE UNIQUE INDEX "IX_Reports_Month" ON "Reports" ("Month");


CREATE INDEX "IX_Subtasks_ActionId" ON "Subtasks" ("ActionId");



