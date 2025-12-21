# Database Schema, Sample Data, and Setup Instructions

This document provides:
- A **complete SQL schema script** for all resource types
- **Sample data** for testing
- **Database setup instructions**

Target DBMS: **Microsoft SQL Server** (based on `.mdf/.ldf` usage)

---

## 1. Database Setup Instructions

### Option A – New Database
```sql
CREATE DATABASE SOMOID_ResourceDB;
GO
USE SOMOID_ResourceDB;
GO
```

### Option B – Existing MDF/LDF Files
1. Open **SQL Server Management Studio (SSMS)**
2. Right-click **Databases → Attach**
3. Add the existing `.mdf` file (the `.ldf` should be detected automatically)
4. Click **OK**
5. Select the database:
```sql
USE SOMOID_ResourceDB;
GO
```

---

## 2. Schema Definition (Complete SQL Script)

> This script is idempotent and can be version-controlled.

```sql
-- ================================
-- APPLICATION
-- ================================
IF OBJECT_ID('dbo.application', 'U') IS NOT NULL
    DROP TABLE dbo.application;
GO

CREATE TABLE dbo.application (
    [resource-name] NVARCHAR(255) NOT NULL,
    [creation-datetime] DATETIME NOT NULL CONSTRAINT DF_application_created DEFAULT (GETDATE()),
    CONSTRAINT PK_application PRIMARY KEY CLUSTERED ([resource-name])
);
GO

-- ================================
-- CONTAINER
-- ================================
IF OBJECT_ID('dbo.container', 'U') IS NOT NULL
    DROP TABLE dbo.container;
GO

CREATE TABLE dbo.container (
    [resource-name] NVARCHAR(255) NOT NULL,
    [creation-datetime] DATETIME NOT NULL CONSTRAINT DF_container_created DEFAULT (GETDATE()),
    [application-resource-name] NVARCHAR(255) NOT NULL,

    CONSTRAINT PK_container_scoped PRIMARY KEY CLUSTERED (
        [resource-name],
        [application-resource-name]
    ),
    CONSTRAINT FK_container_application FOREIGN KEY ([application-resource-name])
        REFERENCES dbo.application ([resource-name])
        ON DELETE CASCADE
);
GO

-- ================================
-- CONTENT INSTANCE
-- ================================
IF OBJECT_ID('dbo.[content-instance]', 'U') IS NOT NULL
    DROP TABLE dbo.[content-instance];
GO

CREATE TABLE dbo.[content-instance] (
    [resource-name] NVARCHAR(255) NOT NULL,
    [creation-datetime] DATETIME NOT NULL CONSTRAINT DF_content_instance_created DEFAULT (GETDATE()),
    [container-resource-name] NVARCHAR(255) NOT NULL,
    [application-resource-name] NVARCHAR(255) NOT NULL,
    [content-type] NVARCHAR(255) NOT NULL,
    [content] NVARCHAR(MAX) NOT NULL,

    CONSTRAINT PK_content_instance_scoped PRIMARY KEY CLUSTERED (
        [resource-name],
        [container-resource-name],
        [application-resource-name]
    ),
    CONSTRAINT FK_content_instance_container FOREIGN KEY (
        [container-resource-name],
        [application-resource-name]
    ) REFERENCES dbo.container (
        [resource-name],
        [application-resource-name]
    ) ON DELETE CASCADE
);
GO

-- ================================
-- SUBSCRIPTION
-- ================================
IF OBJECT_ID('dbo.subscription', 'U') IS NOT NULL
    DROP TABLE dbo.subscription;
GO

CREATE TABLE dbo.subscription (
    [resource-name] NVARCHAR(255) NOT NULL,
    [creation-datetime] DATETIME NOT NULL CONSTRAINT DF_subscription_created DEFAULT (GETDATE()),
    [container-resource-name] NVARCHAR(255) NOT NULL,
    [application-resource-name] NVARCHAR(255) NOT NULL,
    [evt] INT NOT NULL,
    [endpoint] NVARCHAR(255) NOT NULL,

    CONSTRAINT PK_subscription_scoped PRIMARY KEY CLUSTERED (
        [resource-name],
        [container-resource-name],
        [application-resource-name]
    ),
    CONSTRAINT FK_subscription_container FOREIGN KEY (
        [container-resource-name],
        [application-resource-name]
    ) REFERENCES dbo.container (
        [resource-name],
        [application-resource-name]
    ) ON DELETE CASCADE
);
GO
```

---

## 3. Sample Data for Testing

```sql
-- ================================
-- APPLICATIONS
-- ================================
INSERT INTO dbo.application ([resource-name]) VALUES
('app-smartcity'),
('app-healthcare');

-- ================================
-- CONTAINERS
-- ================================
INSERT INTO dbo.container ([resource-name], [application-resource-name]) VALUES
('cnt-sensors', 'app-smartcity'),
('cnt-traffic', 'app-smartcity'),
('cnt-patients', 'app-healthcare');

-- ================================
-- CONTENT INSTANCES
-- ================================
INSERT INTO dbo.[content-instance]
([resource-name], [container-resource-name], [application-resource-name], [content-type], [content])
VALUES
('ci-001', 'cnt-sensors', 'app-smartcity', 'application/json', '{"temperature": 22.5}'),
('ci-002', 'cnt-traffic', 'app-smartcity', 'application/json', '{"vehicles": 120}'),
('ci-003', 'cnt-patients', 'app-healthcare', 'application/xml', '<patient><id>42</id></patient>');

-- ================================
-- SUBSCRIPTIONS
-- ================================
INSERT INTO dbo.subscription
([resource-name], [container-resource-name], [application-resource-name], [evt], [endpoint])
VALUES
('sub-001', 'cnt-sensors', 'app-smartcity', 1, 'http://example.com/notify/sensors'),
('sub-002', 'cnt-traffic', 'app-smartcity', 2, 'http://example.com/notify/traffic'),
('sub-003', 'cnt-patients', 'app-healthcare', 1, 'http://example.com/notify/patients');
GO
```

---

## 4. Notes & Recommendations

- All tables include:
  - `resource-name`
  - `creation-datetime`
  - Type-specific fields
- Scoped primary keys enforce **hierarchical resource uniqueness**
- `ON DELETE CASCADE` ensures referential cleanup
- Script is suitable for:
  - Git versioning
  - CI/CD database provisioning
  - Automated testing

---

