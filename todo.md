# SOMOID Project - Todo List

## Middleware Implementation (60% of grade)

### Core Notification Features
- [x] **Implement MQTT notification support** ✅ COMPLETED (2025-01-03)
  - ✅ Added MQTT endpoint handling for subscriptions
  - ✅ Implemented MQTT client (M2Mqtt library) to publish notifications when content-instances are created/deleted
  - ✅ Publishing to channels matching the container path: `api/somiod/{appName}/{containerName}`
  - ✅ QoS 1 (at least once delivery)
  - ✅ Connection pooling and broker management via `MqttHelper.cs`
  - ✅ Support for `mqtt://` endpoint format (e.g., `mqtt://localhost:1883`)
  - ✅ Tested with MQTTX client - both creation and deletion events working

- [x] **Implement HTTP notification delivery** ✅ COMPLETED
  - ✅ HTTP endpoint notification mechanism implemented
  - ✅ When a subscription event is triggered (creation or deletion of content-instance), sends HTTP POST request to the endpoint specified in the subscription with full resource details and event type
  - ✅ Tested with webhook.site

### Discovery Operations
- [x] **Implement discovery for nested content-instances** ✅ COMPLETED
  - ✅ Discovery at `/api/somiod` returns all content-instances recursively across all applications and containers
  - ✅ Discovery at `/api/somiod/{appName}` returns all content-instances within that application recursively

### API Compliance
- [x] **Prevent unsupported operations on content-instances and subscriptions** ✅ COMPLETED
  - ✅ Content-instance and subscription resources do NOT support UPDATE/PUT operations
  - ✅ Only CRUD (without U) + discovery allowed
  - ✅ Validation returns appropriate HTTP error if update is attempted
  - ✅ PUT operations exist only for applications and containers

- [x] **Disable global GET all operation** ✅ COMPLETED
  - ✅ The GET all operation (without discovery header) is not supported
  - ✅ Only GET for specific resources or GET with `somiod-discovery` header
  - ✅ Enforced in all controllers

---

## Test Applications (15% of grade)** ⚠️Verifiquem se é mesmo isto e que está correto!⚠️
--
To test:
Use MQTTX client to subscribe to the topic `api/somiod/door/door`.
Verify that you receive notifications from both HTTP and MQTT endpoints.
Should work and see 2 Posts in MQTTX client. 
In the notificationList in application B Form should have 3 because it makes for HTTP, MQTT and door_status. 
--

- [ ] **Implement Test Application A (IoT Device)**
  - Create a functional test application (e.g., light bulb IoT device) that:
    1. Creates an application resource on startup
    2. Creates a container for the device
    3. Creates a subscription for creation events (HTTP or MQTT)
    4. Receives and processes notifications
  - Currently Form1.cs in Application_A is empty

- [ ] **Implement Test Application B (Control App)**
  - Create a functional test application (e.g., mobile control app) that:
    1. Creates an application resource
    2. Has UI buttons to create content-instances in Application A's container
    3. Tests the notification mechanism (both HTTP and MQTT)
  - Currently Form1.cs in Application_B has empty button handlers

---

## Special Requirements (15% of grade)

- [x] **Implement XML file serialization for notifications** ✅ COMPLETED 
******
To test everything end-to-end, do:

  1.Trigger both HTTP and MQTT subscriptions by creating or deleting contentInstances that point to the subscribed endpoints.
    Confirm that the JSON notifications still arrive as expected.

  2.After each notification, check the directory:
    App_Data/Notifications/<application>/
      You should see new XML files. Open one and verify that it contains the correct payload and passes validation.

  Full workflow:
    Create Application → Create Container → Create Subscriptions (sub1 for MQTT and sub2 for HTTP) → Create a ContentInstance with data → Verify both endpoints receive the notification and check the files in App_Data.
******
  - Add mandatory feature: All incoming notifications for each application should be serialized into XML files and validated against a predefined XML schema
  - Create XSD schema and implement validation logic
  - Store XML files organized by application
  - Apply to both HTTP and MQTT notifications
---

## Documentation & Delivery (40% of grade)

- [ ] **Create project report with cURL examples**
  - Write comprehensive project report (in PDF/DOCX format) following the provided template
  - Include complete cURL command examples for CRUD+Discovery operations for all SOMOID resources:
    - Application
    - Container
    - Content-instance
    - Subscription (including MQTT subscriptions)
  - Document the custom implementation scenario
  - Document MQTT implementation details

- [ ] **Database schema and initial data**
  - Ensure database has proper schema for all resource types with:
    - creation-datetime
    - resource-name
    - type-specific fields
  - Create SQL script (.sql file) with complete schema and sample data for testing
  - Include instructions for database setup
  - **PARTIALLY IMPLEMENTED**: Database exists (.mdf/.ldf files), but no SQL schema script

- [ ] **Create delivery package structure**
  - Prepare final delivery: ZIP/7Z file with folders:
    - identification.txt (team info)
    - Project/ (source code)
    - Report/ (PDF/DOCX report)
    - Other/ (additional files)
    - Data/ (SQL scripts and .mdf/.ldf database files)
  - Ensure all deliverables are included

---

## Summary

**Total Items:** 11  
**Completed:** 6 ✅  
**Remaining:** 5 ⏳  

**Completion Progress:** 55% (Middleware complete, Test Apps and Documentation pending)

### Priority Areas (Remaining):
1. ⚠️ **XML Serialization** (15% of grade) - Critical missing feature, mandatory requirement
2. 🔧 **Test Applications** (15% of grade) - Application A & B implementations
3. 📄 **Documentation** (40% of grade) - Report, cURL examples, and database schema script
4. 📦 **Delivery Package** - Final packaging and submission

### Completed Areas:
- ✅ **Notification System** - Both HTTP and MQTT fully implemented and tested
- ✅ **Discovery Operations** - Nested content-instance discovery working
- ✅ **API Compliance** - All constraints properly enforced