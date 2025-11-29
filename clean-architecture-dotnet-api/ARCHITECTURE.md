# Architecture Documentation

## Overview

Clean Architecture .NET 8 Web API is a production-ready, enterprise-grade backend solution demonstrating modern software architecture principles, SOLID design, and cloud-native patterns.

**Framework**: .NET 8  
**Architecture Pattern**: Clean Architecture (4-Layer)  
**Design Approach**: Domain-Driven Design (DDD) with CQRS-Light  
**Status**: Production Ready ?

---

## Table of Contents

1. [Architecture Layers](#architecture-layers)
2. [Project Structure](#project-structure)
3. [Core Patterns & Principles](#core-patterns--principles)
4. [Dependency Flow](#dependency-flow)
5. [Data Flow](#data-flow)
6. [Authentication & Authorization](#authentication--authorization)
7. [Caching Strategy](#caching-strategy)
8. [Error Handling](#error-handling)
9. [Testing Strategy](#testing-strategy)
10. [Deployment Architecture](#deployment-architecture)
11. [Design Decisions](#design-decisions)
12. [Scalability Considerations](#scalability-considerations)

---

## Architecture Layers

### Layer 1: Domain Layer (`src/Domain/`)

**Purpose**: Core business logic with zero framework dependencies  
**Responsibility**: Define business rules, entities, and domain models  
**Dependencies**: None (framework-independent)

#### Key Components

**Entities**
```
AggregateRoot (base class)
??? User (aggregate root)
    ??? Business methods: Create(), UpdateProfile(), SetRefreshToken(), Deactivate()
    ??? Properties: Id, Email, FirstName, LastName, PasswordHash, etc.
    ??? Invariants: Validates email on creation, enforces password requirements
```

**Value Objects** (Implicit)
- Email (validated, immutable concept)
- PasswordHash (encrypted, immutable)

**Interfaces**
- `IRepository<T>` - Generic data access contract
- `IUserRepository` - User-specific queries
- `ICacheService` - Cache abstraction
- `IPasswordService` - Password hashing contract

**Exceptions**
- `DomainException` - Base domain exception with error code
- `NotFoundException` - Resource not found

**Design Principles Applied**
- ? Aggregate Root pattern (User encapsulates business rules)
- ? Entity responsibility (Create, Update, Delete operations)
- ? Invariant enforcement (All business rules in entity)
- ? No framework dependencies (Pure C# classes)

---

### Layer 2: Application Layer (`src/Application/`)

**Purpose**: Use case orchestration and application logic  
**Responsibility**: Implement use cases, coordinate domain objects, define DTOs  
**Dependencies**: Domain layer only (depends inward)

#### Key Components

**Commands & Queries (CQRS-Light)**
```
CreateUserCommand
??? Request: Email, FirstName, LastName, Password
??? Handler: CreateUserCommandHandler
?   ??? Validates email uniqueness
?   ??? Hashes password
?   ??? Creates User aggregate
?   ??? Persists via repository
?   ??? Returns UserDto
??? Result: UserDto

GetUserByIdQuery
??? Request: UserId
??? Handler: GetUserByIdQueryHandler
?   ??? Checks Redis cache
?   ??? Queries database if cache miss
?   ??? Populates cache (15 min TTL)
?   ??? Returns UserDto
??? Result: UserDto
```

**Data Transfer Objects (DTOs)**
- `UserDto` - User representation for API responses
- `AuthTokenDto` - Authentication token response

**Behaviors (Pipeline Middleware)**
- `ValidationBehavior<TRequest, TResponse>` - Validates all requests using FluentValidation rules
  - Runs before handler execution
  - Aggregates all validation errors
  - Throws on validation failure

**Mappers**
- `MappingProfile` - AutoMapper configuration for entity ? DTO mapping

**Design Principles Applied**
- ? CQRS-Light pattern (Separate read/write operations)
- ? Pipeline behaviors (Cross-cutting concerns)
- ? Input validation (FluentValidation)
- ? DTO pattern (API contract)
- ? Mapping abstraction (Entity isolation)

---

### Layer 3: Infrastructure Layer (`src/Infrastructure/`)

**Purpose**: Handle external concerns and dependencies  
**Responsibility**: Data access, caching, authentication, email, background jobs  
**Dependencies**: Domain + Application layers

#### Key Components

**Persistence**
```
CleanArchDbContext (EF Core DbContext)
??? DbSet<User>
??? OnModelCreating()
?   ??? Configures User entity mapping
?   ??? Sets indexes (Email unique, IsActive)
?   ??? Sets default values
?   ??? Sets constraints
??? Connection: SQL Server

Repository<T> (Generic implementation)
??? GetByIdAsync(id)
??? GetAllAsync()
??? AddAsync(entity)
??? UpdateAsync(entity)
??? DeleteAsync(entity)
??? ExistsAsync(id)

UserRepository (Specialized)
??? GetByEmailAsync(email)
??? EmailExistsAsync(email)
??? Inherits from Repository<User>
```

**Caching**
```
RedisCacheService
??? Uses: StackExchange.Redis
??? Pattern: Cache-Aside (check cache ? DB ? populate)
??? Serialization: System.Text.Json
??? Methods:
?   ??? GetAsync<T>(key)
?   ??? SetAsync<T>(key, value, TTL)
?   ??? RemoveAsync(key)
?   ??? ExistsAsync(key)
??? TTL: 15 minutes (configurable)
```

**Authentication**
```
TokenService
??? GenerateAccessToken(userId, email, role)
?   ??? Creates JWT with claims, expires in 15 min
??? GenerateRefreshToken()
?   ??? Creates random 64-byte token
??? ValidateToken(token)
    ??? Validates signature, issuer, audience, lifetime

JwtSettings (Configuration)
??? Issuer
??? Audience
??? SecretKey (256+ bits minimum)
??? AccessTokenExpirationMinutes (15)
??? RefreshTokenExpirationDays (7)
```

**Security**
```
PasswordService
??? HashPassword(password) ? BCrypt hash
??? VerifyPassword(password, hash) ? boolean
```

**Email (Extensible)**
```
IEmailService (Interface)
??? MockEmailService (Development implementation)
    ??? Logs email instead of sending
```

**Background Processing**
```
EmailDispatcherBackgroundService (HostedService)
??? Runs on interval (5 seconds)
??? Checks for pending emails
??? Sends asynchronously
??? Supports graceful shutdown
??? Logging integrated
```

**Design Principles Applied**
- ? Repository pattern (Data access abstraction)
- ? Dependency injection (Loose coupling)
- ? Interface abstraction (Swappable implementations)
- ? Cache-aside pattern (Performance optimization)
- ? Background job pattern (Async processing)

---

### Layer 4: API Layer (`src/API/`)

**Purpose**: HTTP entry point and external interface  
**Responsibility**: Handle HTTP requests, apply middleware, present endpoints  
**Dependencies**: All other layers

#### Key Components

**Controllers**
```
UsersController
??? POST /api/users ? CreateUserCommand
??? GET /api/users/{id} ? GetUserByIdQuery [Requires JWT]
??? Dependencies: IMediator

AuthController
??? POST /api/auth/login ? Generate JWT + Refresh token
??? POST /api/auth/refresh ? Issue new JWT
??? Dependencies: ITokenService, IUserRepository, IPasswordService
```

**Middleware**
```
ErrorHandlingMiddleware
??? Catches all exceptions
??? Maps to consistent error response
??? Exception types:
?   ??? NotFoundException ? 404
?   ??? DomainException ? 400
?   ??? ValidationException ? 400 (with details)
?   ??? Unhandled ? 500
??? Response format:
    {
      "message": "...",
      "code": "ERROR_CODE",
      "errors": { "field": ["error1", "error2"] },
      "timestamp": "2025-11-30T00:00:00Z"
    }
```

**Dependency Injection Configuration** (`Program.cs`)
```
Services registered:
??? DbContext (SQL Server)
??? Redis (StackExchange.Redis)
??? Repositories (IUserRepository, IRepository<T>)
??? Security (IPasswordService)
??? Caching (ICacheService)
??? MediatR (Command/Query bus)
??? AutoMapper (Object mapping)
??? FluentValidation (Input validation)
??? JWT (Token service)
??? Email (IEmailService)
??? Background Services (HostedServices)
??? Health Checks
??? Swagger (OpenAPI docs)
```

**Configuration Files**
- `appsettings.json` - Local development
- `appsettings.Docker.json` - Container environment

**Design Principles Applied**
- ? Separation of concerns (Controllers only handle HTTP)
- ? Middleware pattern (Cross-cutting concerns)
- ? Dependency injection (Loose coupling)
- ? Configuration externalization
- ? Health checks (Operational readiness)

---

## Project Structure

```
clean-architecture-dotnet-api/
??? src/
?   ??? Domain/                              # Core business logic (NO dependencies)
?   ?   ??? Domain.csproj
?   ?   ??? Entities/
?   ?   ?   ??? AggregateRoot.cs
?   ?   ?   ??? User.cs                      # Aggregate root with business logic
?   ?   ??? Interfaces/
?   ?   ?   ??? IRepository.cs               # Generic repository contract
?   ?   ?   ??? IUserRepository.cs
?   ?   ?   ??? ICacheService.cs
?   ?   ?   ??? IPasswordService.cs
?   ?   ??? Exceptions/
?   ?       ??? DomainException.cs
?   ?       ??? NotFoundException.cs
?   ?
?   ??? Application/                         # Use cases & orchestration
?   ?   ??? Application.csproj
?   ?   ??? Commands/
?   ?   ?   ??? CreateUser/
?   ?   ?       ??? CreateUserCommand.cs     # Request model
?   ?   ?       ??? CreateUserCommandHandler.cs
?   ?   ??? Queries/
?   ?   ?   ??? GetUserById/
?   ?   ?       ??? GetUserByIdQuery.cs
?   ?   ?       ??? GetUserByIdQueryHandler.cs
?   ?   ??? DTOs/
?   ?   ?   ??? UserDto.cs
?   ?   ?   ??? AuthTokenDto.cs
?   ?   ??? Behaviors/
?   ?   ?   ??? ValidationBehavior.cs        # Pipeline validation
?   ?   ??? Mappers/
?   ?       ??? MappingProfile.cs            # AutoMapper configuration
?   ?
?   ??? Infrastructure/                      # External dependencies
?   ?   ??? Infrastructure.csproj
?   ?   ??? Persistence/
?   ?   ?   ??? CleanArchDbContext.cs        # EF Core context
?   ?   ?   ??? Repositories/
?   ?   ?       ??? Repository.cs
?   ?   ?       ??? UserRepository.cs
?   ?   ??? Caching/
?   ?   ?   ??? RedisCacheService.cs
?   ?   ??? Authentication/
?   ?   ?   ??? JwtSettings.cs
?   ?   ?   ??? TokenService.cs
?   ?   ??? Security/
?   ?   ?   ??? PasswordService.cs
?   ?   ??? Email/
?   ?   ?   ??? EmailService.cs
?   ?   ??? BackgroundJobs/
?   ?       ??? EmailDispatcherBackgroundService.cs
?   ?
?   ??? API/                                 # HTTP layer
?       ??? API.csproj
?       ??? Controllers/
?       ?   ??? UsersController.cs
?       ?   ??? AuthController.cs
?       ??? Middleware/
?       ?   ??? ErrorHandlingMiddleware.cs
?       ??? Program.cs                       # DI & middleware setup
?       ??? appsettings.json
?       ??? appsettings.Docker.json
?
??? tests/
?   ??? Domain.Tests/
?       ??? Domain.Tests.csproj
?       ??? Entities/
?       ?   ??? UserTests.cs                 # 7 unit tests (all passing)
?       ??? Domain.Tests.csproj
?
??? docker-compose.yml                       # Local dev stack (API + SQL + Redis)
??? Dockerfile                               # Production container image
??? clean-architecture-dotnet-api.sln
??? [Documentation files...]
```

---

## Core Patterns & Principles

### 1. Clean Architecture

**Layers** (Dependency direction: inward only)
```
                   API Layer
                       ?
            Application Layer (MediatR)
                       ?
                Domain Layer
                       ?
            Infrastructure Layer
```

**Rules**
- ? Domain layer: Zero dependencies (pure C#)
- ? Application layer: Depends on domain only
- ? Infrastructure layer: Implements domain interfaces
- ? API layer: Orchestrates all layers
- ? No circular dependencies

### 2. Domain-Driven Design (DDD)

**Aggregate Root**: `User`
- Encapsulates business logic
- Enforces invariants (e.g., email validation)
- Methods: `Create()`, `UpdateProfile()`, `SetRefreshToken()`, `Deactivate()`
- Only aggregate root can be modified

**Value Objects** (Implicit)
- Email (validated, immutable)
- PasswordHash (hashed, immutable)
- RefreshToken (random, immutable)

### 3. CQRS-Light Pattern

**Commands** (Write operations)
- `CreateUserCommand` ? CreateUserCommandHandler
- Modifies state
- Returns result after persistence

**Queries** (Read operations)
- `GetUserByIdQuery` ? GetUserByIdQueryHandler
- Cached for performance
- No side effects

### 4. Repository Pattern

**Generic Repository**
- Abstracts EF Core
- Reusable across entities
- Methods: GetById, GetAll, Add, Update, Delete, Exists

**Specialized Repository**
- `IUserRepository` extends `IRepository<User>`
- Email-specific queries: GetByEmail, EmailExists

**Benefits**
- ? Testable (mock-friendly)
- ? Swappable (SQL ? NoSQL)
- ? Consistent (single source of truth)

### 5. Pipeline Behaviors

```
Request
  ?
[ValidationBehavior]  ? Validates before handler
  ?
Handler execution
  ?
Response
```

- Runs before request handler
- Validates all inputs
- Aggregates validation errors
- Throws if invalid

### 6. Dependency Injection

**Registered Services** (Program.cs)
- DbContext (SQL Server)
- Repositories
- Business services
- MediatR handlers
- AutoMapper
- Validators
- Background services

**Benefits**
- ? Loose coupling
- ? Testability
- ? Configuration externalization
- ? Runtime service swapping

### 7. SOLID Principles

| Principle | Implementation |
|-----------|-----------------|
| **S**ingle Responsibility | Each class has one reason to change |
| **O**pen/Closed | Open for extension (new commands), closed for modification |
| **L**iskov Substitution | Repositories implement contracts consistently |
| **I**nterface Segregation | Small, focused interfaces (IRepository, ICacheService) |
| **D**ependency Inversion | Depend on abstractions (IUserRepository, not UserRepository) |

---

## Dependency Flow

### Initialization Flow

```
Program.cs
??? Load configuration
??? Register DbContext (SQL Server)
??? Register Redis (non-blocking)
??? Register repositories
??? Register services (password, cache, email)
??? Register MediatR + handlers + behaviors
??? Register AutoMapper
??? Register validators
??? Register JWT auth
??? Register background services
??? Configure middleware pipeline
??? Start application
```

### Request Flow

```
HTTP Request
     ?
Routing ? Controller Action
     ?
MediatR.Send(Command/Query)
     ?
ValidationBehavior (check input)
     ?
Handler execution
     ??? Query: Check cache ? DB ? populate cache
     ??? Command: Validate ? Create/Update domain object ? Persist
     ?
Exception handling (if error)
     ?
Response (DTO) ? Serialized JSON ? HTTP 200/400/500
```

### Dependency Resolution

```
UsersController
??? IMediator (MediatR bus)
    ??? CreateUserCommandHandler
        ??? IUserRepository
        ?   ??? CleanArchDbContext
        ??? IMapper (AutoMapper)
        ??? IPasswordService
        ?   ??? BCrypt.Net
        ??? ICacheService
            ??? IConnectionMultiplexer (Redis)
```

---

## Data Flow

### User Creation Flow

```
1. API Request
   POST /api/users
   {
     "email": "user@example.com",
     "firstName": "John",
     "lastName": "Doe",
     "password": "SecurePassword123!"
   }

2. Controller
   UsersController.CreateUser(command)
   ? Sends to MediatR

3. Validation
   ValidationBehavior checks:
   - Email format
   - Name length (2-100 chars)
   - Password strength (8+ chars, upper, lower, digit)

4. Handler
   CreateUserCommandHandler
   - Check email uniqueness via IUserRepository
   - Hash password via IPasswordService (BCrypt)
   - Create User aggregate via User.Create()
   - Persist via IUserRepository.AddAsync()

5. Mapping
   User aggregate ? UserDto (AutoMapper)

6. Response
   HTTP 201 Created
   {
     "id": "guid",
     "email": "user@example.com",
     "firstName": "John",
     "lastName": "Doe",
     "role": "User",
     "isActive": true,
     "createdAtUtc": "2025-11-30T00:00:00Z"
   }
```

### User Retrieval Flow

```
1. API Request
   GET /api/users/{id}
   Authorization: Bearer <JWT>

2. Controller
   UsersController.GetUser(id)
   ? Sends GetUserByIdQuery to MediatR

3. Query Handler
   GetUserByIdQueryHandler
   - Generate cache key: "user:{id}"
   - Check Redis via ICacheService.GetAsync()

4a. Cache Hit
    ? Return cached UserDto
    ? HTTP 200 OK

4b. Cache Miss
    - Query database via IUserRepository
    - If not found ? throw NotFoundException
    - Populate cache with 15-min TTL
    - Map to UserDto

5. Response
   HTTP 200 OK
   [UserDto]
```

### Authentication Flow

```
1. Login Request
   POST /api/auth/login
   {
     "email": "user@example.com",
     "password": "SecurePassword123!"
   }

2. Handler
   AuthController.Login()
   - Query user via IUserRepository.GetByEmailAsync()
   - Verify password via IPasswordService.VerifyPassword()
   - Check user is active

3. Token Generation
   TokenService.GenerateAccessToken()
   - Create JWT with claims (sub, email, role)
   - Sign with secret key
   - Expire in 15 minutes

   TokenService.GenerateRefreshToken()
   - Generate random 64-byte token
   - Expire in 7 days

4. Persistence
   User.SetRefreshToken(token, expiry)
   - Save to database

5. Response
   HTTP 200 OK
   {
     "accessToken": "eyJ...",
     "refreshToken": "abc123...",
     "expiresInSeconds": 900,
     "issuedAtUtc": "2025-11-30T00:00:00Z"
   }

6. Authenticated Request
   GET /api/users/{id}
   Authorization: Bearer <accessToken>
   ?
   Middleware validates JWT signature, expiry, claims
   ? Grant access if valid
```

---

## Authentication & Authorization

### JWT Implementation

**Token Structure**
```
Header: { "alg": "HS256", "typ": "JWT" }
Payload: {
  "sub": "user-id",
  "email": "user@example.com",
  "role": "User",
  "iat": 1701345600,
  "exp": 1701349200
}
Signature: HMACSHA256(header.payload, secretKey)
```

**Validation Rules**
- ? Signature valid (secret key match)
- ? Issuer matches (configured issuer)
- ? Audience matches (configured audience)
- ? Token not expired (exp claim)
- ? Not skew (ClockSkew = 0 seconds)

**Token Rotation Pattern**
```
1. Login ? Access token + Refresh token
2. Access token expires (15 min)
3. Use refresh token ? New access token + New refresh token
4. Old tokens invalidated
5. Repeat as needed
```

### Role-Based Authorization

**Claims in JWT**
- `sub` (subject) = User ID
- `email` = User email
- `role` = User role (User, Admin, etc.)

**Usage in Controllers**
```csharp
[Authorize]  // Requires valid JWT
public async Task GetUser(Guid id) { ... }

[Authorize(Roles = "Admin")]  // Requires Admin role
public async Task DeleteUser(Guid id) { ... }
```

---

## Caching Strategy

### Cache-Aside Pattern

```
Read request
?? Check cache (Redis)
?  ?? Hit: Return cached value ? (O(1) operation)
?  ?? Miss: Continue ?
?? Query database
?? Cache result (TTL: 15 min)
?? Return to client
```

### Invalidation Strategy

**Get by ID** (Cache-heavy)
```
Query: GetUserByIdQuery
?? Cache key: "user:{id}"
?? TTL: 15 minutes
?? Hit rate: High (same user queried multiple times)
```

**Invalidation on Update**
```
Command: UpdateUserCommand
?? Update database
?? Invalidate cache key "user:{id}"
?? Next query rebuilds cache
```

### Benefits

| Benefit | Implementation |
|---------|-----------------|
| **Reduced DB load** | Most reads hit Redis (O(1)) |
| **Faster responses** | Network latency instead of DB query |
| **Consistency** | Invalidate on write, rebuild on read |
| **Scalability** | Read scaling via Redis cluster |

---

## Error Handling

### Exception Hierarchy

```
Exception
??? DomainException (Domain layer)
?   ??? Code: "ERROR_CODE"
?   ??? Message: Business-friendly description
?   ??? Types: NotFoundException, ValidationException, etc.
?
??? ApplicationException (Application layer)
    ??? ValidationException
        ??? Errors: Dictionary<field, errors[]>
        ??? Aggregates all validation failures
```

### Error Response Format

```json
{
  "message": "User not found",
  "code": "NOT_FOUND",
  "errors": null,
  "timestamp": "2025-11-30T00:00:00Z"
}
```

```json
{
  "message": "Validation failed",
  "code": "VALIDATION_ERROR",
  "errors": {
    "email": ["Invalid email format", "Email already exists"],
    "password": ["Minimum 8 characters"]
  },
  "timestamp": "2025-11-30T00:00:00Z"
}
```

### HTTP Status Mapping

| Exception | HTTP Status |
|-----------|------------|
| NotFoundException | 404 |
| DomainException | 400 |
| ValidationException | 400 |
| Unhandled | 500 |

### Global Middleware

```csharp
ErrorHandlingMiddleware
??? Catches all exceptions
??? Logs errors
??? Formats response
??? Returns consistent error format
```

---

## Testing Strategy

### Unit Testing Approach

**Test Location**: `tests/Domain.Tests/`  
**Framework**: xUnit  
**Mocking**: Moq  
**Assertions**: FluentAssertions

### Test Coverage

**Domain Layer Tests** (7 tests)
```
UserTests
??? Create_WithValidParameters_CreatesUser
??? Create_WithEmptyEmail_ThrowsArgumentException
??? UpdateProfile_WithValidData_UpdatesUser
??? SetRefreshToken_WithValidToken_StoresToken
??? IsRefreshTokenValid_WithValidToken_ReturnsTrue
??? IsRefreshTokenValid_WithExpiredToken_ReturnsFalse
??? Deactivate_WhenCalled_DeactivatesUser
```

### Testing Best Practices

- ? Test domain logic in isolation (no framework)
- ? Arrange-Act-Assert pattern
- ? Descriptive test names
- ? Single responsibility per test
- ? Fast execution (in-memory, no I/O)

### Future Testing Layers

```
Domain.Tests        ? Unit tests (7/7 passing ?)
Application.Tests   ? Handler tests
Integration.Tests   ? Full feature tests
```

---

## Deployment Architecture

### Docker Deployment

**Dockerfile** (Multi-stage build)
```
Stage 1: Build
??? SDK image (mcr.microsoft.com/dotnet/sdk:8.0)
??? Copy project files
??? dotnet restore
??? dotnet build
??? dotnet publish
??? Output: /app/publish

Stage 2: Runtime
??? ASP.NET image (mcr.microsoft.com/dotnet/aspnet:8.0)
??? Copy published files from Stage 1
??? Expose port 8080
??? Start application
```

**Benefits**
- ? Smaller image (SDK not included in final)
- ? Optimized for production
- ? Consistent build environment
- ? Easy deployment

### docker-compose.yml (Local Development)

```yaml
Services:
- API (Port 5000)
  ??? Depends on: sqlserver, redis
  ??? Health check: /health endpoint
  ??? Environment: Docker config
- SQL Server (Port 1433)
  ??? Database: CleanArchDb
  ??? SA Password: (configured)
  ??? Volume: Data persistence
- Redis (Port 6379)
  ??? Cache server
  ??? Volume: Data persistence
```

### Environment Configuration

**Local Development** (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;..."
  }
}
```

**Docker** (appsettings.Docker.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver;..."
  }
}
```

**Production** (Environment variables)
```
ConnectionStrings__DefaultConnection=Server=prod-sql-server;...
Jwt__SecretKey=<secure-key>
```

---

## Design Decisions

### 1. Why Clean Architecture?

| Benefit | Implementation |
|---------|-----------------|
| **Testability** | Domain layer has no dependencies |
| **Maintainability** | Clear separation of concerns |
| **Flexibility** | Swap implementations (SQL ? NoSQL) |
| **Scalability** | Layers scale independently |
| **Team Scaling** | Clear boundaries for feature teams |

### 2. Why MediatR?

| Benefit | Implementation |
|---------|-----------------|
| **Decoupling** | Controllers don't directly call handlers |
| **Cross-cutting** | Pipeline behaviors (validation, logging) |
| **Testability** | Handlers tested without HTTP context |
| **Flexibility** | Easy to add middleware |

### 3. Why Repository Pattern?

| Benefit | Implementation |
|---------|-----------------|
| **Abstraction** | Hide EF Core complexity |
| **Testability** | Mock repository for unit tests |
| **Flexibility** | Swap implementations |
| **Consistency** | Single data access pattern |

### 4. Why Cache-Aside Pattern?

| Benefit | Implementation |
|---------|-----------------|
| **Performance** | O(1) reads from Redis |
| **Flexibility** | Cache optional, app still works |
| **Consistency** | Invalidate on write |
| **Simplicity** | Minimal code required |

### 5. Why Aggregate Root (User)?

| Benefit | Implementation |
|---------|-----------------|
| **Encapsulation** | Business logic in entity |
| **Invariants** | Enforce rules via methods |
| **Identity** | Single root entity per aggregate |
| **Transactions** | One aggregate = one transaction |

### 6. Why JWT Tokens?

| Benefit | Implementation |
|---------|-----------------|
| **Stateless** | No session storage needed |
| **Scalable** | Multiple servers without sync |
| **Secure** | Signature validates integrity |
| **Standard** | Industry-standard format |

### 7. Why BCrypt for Passwords?

| Benefit | Implementation |
|---------|-----------------|
| **Slow hashing** | Resists brute-force attacks |
| **Salted** | Each hash unique |
| **Proven** | Industry standard |
| **Libraries** | Well-maintained .NET package |

---

## Scalability Considerations

### Horizontal Scaling

**API Instances**
```
Load Balancer
??? API Instance 1
??? API Instance 2
??? API Instance 3
??? API Instance N
    ??? All connect to shared DB + Redis
```

**Requirements**
- ? Stateless API (no session affinity needed)
- ? JWT for auth (no session store)
- ? Shared Redis (distributed cache)
- ? Shared database (single source of truth)

### Vertical Scaling

**Database Read Replicas**
```
Write Operations ? Primary database
Read Operations ? Read replica 1/2/3
```

**Implementation**
```csharp
// Can be implemented via EF Core routing
// Current: Single database
// Future: Route reads to replica
```

### Caching Optimization

**Cache Strategy**
```
Read Distribution
- 90% Redis (cache hits)
- 10% Database (cache misses)
```

**Scaling Points**
- ? Redis cluster (distribute cache)
- ? Database replication (distribute reads)
- ? API load balancing (distribute requests)

### Database Optimization

**Current**
- Entity Framework Core
- Lazy loading disabled (prevent N+1)
- Explicit includes
- Parameterized queries (prevent SQL injection)

**Future**
- Query optimization
- Indexing strategy
- Archival strategy
- Partitioning (if large data)

### Performance Tuning Points

| Component | Strategy |
|-----------|----------|
| **API** | Request/response compression, caching headers |
| **Database** | Indexes, query optimization, replication |
| **Cache** | TTL tuning, invalidation strategy |
| **Security** | Token validation (cached?), password hashing |

---

## Monitoring & Observability

### Health Checks

**Endpoint**: `GET /health`

**Checks**
- ? Database connectivity
- ? Redis connectivity
- ? Application status

**Response**
```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "redis": "Healthy"
  }
}
```

### Structured Logging

**Readiness**
- ILogger injected in services
- Structured log format supported
- Async logging (non-blocking)

**Future Integration**
```csharp
// Serilog ready
Log.Information("User {UserId} created", userId);
Log.Error(ex, "Failed to create user");
```

### Metrics & Tracing

**Ready for**
- Prometheus (metrics export)
- Jaeger (distributed tracing)
- Application Insights (Azure monitoring)

---

## API Endpoints

### User Management

```
POST /api/users
??? Create new user
??? No auth required
??? Request: { email, firstName, lastName, password }
??? Response: 201 Created, UserDto

GET /api/users/{id}
??? Get user by ID
??? [Authorize] required
??? Response: 200 OK, UserDto
??? Cached: 15 minutes
```

### Authentication

```
POST /api/auth/login
??? Login with credentials
??? No auth required
??? Request: { email, password }
??? Response: 200 OK, { accessToken, refreshToken, expiresInSeconds }

POST /api/auth/refresh
??? Refresh access token
??? No auth required
??? Request: { refreshToken }
??? Response: 200 OK, { accessToken, refreshToken, expiresInSeconds }
```

### System

```
GET /health
??? Health check
??? No auth required
??? Response: 200 OK, { status, checks }

GET /swagger
??? API documentation
??? No auth required
??? Response: Swagger UI
```

---

## Technology Decisions

| Decision | Rationale |
|----------|-----------|
| **.NET 8** | Latest LTS, performance, features |
| **EF Core** | Standard ORM, productivity |
| **SQL Server** | Enterprise DB, transactions, reliability |
| **Redis** | Fast cache, distributed |
| **MediatR** | CQRS pattern, pipeline support |
| **JWT** | Stateless auth, scalability |
| **Docker** | Consistent deployment |
| **xUnit** | Modern test framework |
| **AutoMapper** | Object mapping, DRY principle |
| **FluentValidation** | Declarative validation |
| **Swagger** | API documentation, testing |

---

## Future Enhancements

### Short Term (Next Sprint)

```
? Application layer tests
? Integration tests
? Logging (Serilog)
? Rate limiting (Polly)
? Pagination
```

### Medium Term (Next Quarter)

```
? Real email service integration
? Audit logging
? Soft deletes
? Event sourcing
? CQRS full pattern
```

### Long Term (Next Year)

```
? Microservices (if needed)
? Message queue (RabbitMQ)
? Graph QL API
? Advanced caching (Redis cluster)
? Database replication
? Read model separation
```

---

## Conclusion

This Clean Architecture .NET 8 Web API demonstrates:

? **Professional structure** - Clear layer separation  
? **Enterprise patterns** - DDD, CQRS-Light, Repository  
? **Production quality** - Error handling, logging, tests  
? **Security** - JWT, BCrypt, input validation  
? **Performance** - Redis caching, async operations  
? **Scalability** - Stateless, distributed-ready  
? **Maintainability** - SOLID principles, documented  
? **Testability** - Framework-independent domain logic  

The architecture is ready for production deployment and team development.

---

**Version**: 1.0  
**Last Updated**: November 30, 2025  
**Status**: ? Production Ready

