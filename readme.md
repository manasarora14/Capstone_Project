**Service Management System**


**Overview**

A full-stack web application for managing service requests, technician assignments, billing, and reporting. Backend is ASP.NET Core Web API (.NET 8) with EF Core and JWT auth; frontend is Angular with Angular Material.



**Features**

- Authentication & JWT-based authorization (Admin, Manager, Technician, Customer)
- Service catalog (categories with charges and SLA hours)
- Service requests lifecycle: Requested → Assigned → In Progress → Completed → Closed
- Technician assignment & availability checks
- Work tracking (start/finish), rescheduling, and cancellation
- Billing & invoicing (auto-generate on completion, pay to close)
- Dashboards & reports (status summary, workload, revenue, SLA/avg resolution)


**Tech Stack**

- Backend: ASP.NET Core Web API (.NET 8), EF Core, Identity, JWT, LINQ
- Frontend: Angular, TypeScript, Angular Material, HttpClient, Interceptors
- DB: SQL Server
- Tests: xUnit, Moq


**Backend Setup**

\1) Prereqs: \.NET 8 SDK, SQL Server

\2) Set connection string in ServiceManagementApi/appsettings\.Development\.json

\3) Restore & run migrations:

cd ServiceManagementApi

dotnet restore

dotnet ef database update

\4) Run API:dotnet run

API docs: Swagger at /swagger.








**Frontend Setup**

\1) Prereqs: Node\.js, npm, Angular CLI

\2) Install & run:

cd ServiceManagementUI

npm install

npm start    # or ng serve

Default dev server: `http://localhost:4200`



**Running Tests**

Backend unit tests:

cd ServiceManagementApi/ServiceManagementApi.Tests

dotnet test


**Key Projects**

- `ServiceManagementApi/` - ASP.NET Core backend
- `  `Controllers: Auth, Admin, Category, ServiceRequest, Billing, Dashboard, Technician
- ` `Services: AuthService, AdminService, CategoryService, ServiceRequestService, AvailabilityService, BillingService, DashboardService, TechnicianService
- ` `Data: ApplicationDbContext, migrations
- ` `Models: ServiceRequest, ServiceCategory, Invoice
- `ServiceManagementUI/` - Angular frontend
- `  `Components: login, register, service-catalog, create-request, my-service, service-details, assign-request, monitor-progress, task-list, task-detail, workload, billing, reports, admin-panel
- ` `Services: auth-service, request-service, admin-service, api.service, interceptors/guards



**Scripts** 

dotnet ef migrations add <Name>   # add migration

dotnet ef database update         # apply migration

npm start                         # run Angular dev server






**Authentication & Roles**

- Uses ASP.NET Identity + JWT
- Roles: Admin, Manager, Technician, Customer
- Apply tokens via `Authorization: Bearer <token>`
- Frontend interceptor injects JWT into API calls



**Data Model (Core)**

- ServiceCategory: Name, Description, BaseCharge, SlaHours
- ServiceRequest: IssueDescription, Priority, Status, ScheduledDate, PlannedStartUtc, WorkStartedAt/WorkEndedAt, CompletedAt, EstimatedDurationHours, TotalPrice, CategoryId, CustomerId, TechnicianId
- Invoice: ServiceRequestId, Amount, Status (Pending/Paid), GeneratedDate, PaidAt, PaidBy


**Notes**

Availability checks prevent technician double-booking based on SLA/estimated duration.

Work duration uses WorkStartedAt → WorkEndedAt; negative durations are guarded.

Billing: Invoice generated on completion; paying sets request to Closed.



