# Clean Architecture .NET Web API

A production-ready, enterprise-level .NET 8 Web API showcasing Clean Architecture principles, SOLID design, and cloud-ready patterns.

## ??? Architecture Overview

```
???????????????????????????????????????????????????????????
?                   API Layer (Controllers)               ?
???????????????????????????????????????????????????????????
?         Application Layer (Commands, Queries, DTOs)     ?
???????????????????????????????????????????????????????????
?     Domain Layer (Entities, Value Objects, Interfaces)  ?
???????????????????????????????????????????????????????????
?   Infrastructure (EF Core, Redis, External Services)    ?
???????????????????????????????????????????????????????????
```

## ?? Project Structure

```
clean-architecture-dotnet-api/
??? src/
?   ??? Domain/                          # Core business logic
?   ?   ??? Entities/
?   ?   ??? ValueObjects/
?   ?   ??? Interfaces/
?   ?   ??? Exceptions/
?   ??? Application/                     # Use cases & orchestration
?   ?   ??? Commands/
?   ?   ??? Queries/
?   ?   ??? DTOs/
?   ?   ??? Behaviors/
?   ?   ??? Mappers/
?   ?   ??? Exceptions/
?   ??? Infrastructure/                  # Data access & external services
?   ?   ??? Persistence/
?   ?   ??? Caching/
?   ?   ??? Email/
?   ?   ??? BackgroundJobs/
?   ?   ??? Configuration/
?   ??? API/                             # HTTP layer
?       ??? Controllers/
?       ??? Middleware/
?       ??? Extensions/
?       ??? appsettings.json
??? tests/
?   ??? Domain.Tests/
?   ??? Application.Tests/
?   ??? Integration.Tests/
??? docker-compose.yml
??? Dockerfile
??? README.md
```

## ?? Key Features Implemented

### 1. **Domain-Driven Design (DDD)**
   - Rich domain entities with business logic encapsulation
   - Value objects for immutable concepts
   - Domain events (future extensibility)

### 2. **Clean Architecture Layers**
   - **Domain**: Zero dependencies, pure business rules
   - **Application**: Orchestration using MediatR commands/queries
   - **Infrastructure**: Data access, external integrations, caching
   - **API**: HTTP concerns only (controllers, middleware)

### 3. **Advanced Async Patterns**
   - Full async/await implementation
   - Cancellation token support throughout
   - Pipeline behaviors for cross-cutting concerns

### 4. **Data Access**
   - Entity Framework Core with DbContext
   - Raw SQL queries for complex operations (Dapper integration ready)
   - Database migrations included

### 5. **Security**
   - JWT authentication with refresh tokens
   - Role-based authorization
   - Secure token refresh flow with token rotation

### 6. **Caching Strategy**
   - Redis integration for high-frequency queries
   - Get-by-id pattern with invalidation
   - Cache warming capabilities

### 7. **Error Handling**
   - Polymorphic middleware for consistent error responses
   - Domain exceptions with proper HTTP status mapping
   - Detailed error logging

### 8. **API Standards**
   - OpenAPI/Swagger documentation
   - API versioning (v1, v2 ready)
   - Health checks endpoint
   - Metrics endpoint for monitoring

### 9. **Background Processing**
   - Hosted service for async email dispatch
   - Job queue pattern
   - Graceful shutdown support

### 10. **Testing**
   - Unit tests (xUnit, Moq)
   - Integration tests with test containers
   - Repository pattern for testability

## ?? Quick Start

### Prerequisites
- .NET 8 SDK
- SQL Server 2019+
- Redis 6.0+
- Docker & Docker Compose

### Local Development (Without Docker)

1. **Clone & Restore**
   ```bash
   dotnet restore
   ```

2. **Configure Database**
   - Update connection string in `appsettings.Development.json`
   - Run migrations:
   ```bash
   cd src/API
   dotnet ef database update --project ../Infrastructure
   ```

3. **Configure Redis**
   - Ensure Redis is running on localhost:6379
   - Or update connection in appsettings

4. **Run API**
   ```bash
   cd src/API
   dotnet run
   ```

5. **Access Swagger**
   - Navigate to: `https://localhost:7001/swagger`

### Docker Compose (Recommended)

```bash
docker-compose up -d
```

Services:
- **API**: `http://localhost:5000`
- **Swagger**: `http://localhost:5000/swagger`
- **SQL Server**: `localhost:1433`
- **Redis**: `localhost:6379`
- **Health Check**: `http://localhost:5000/health`

## ?? Design Decisions & Rationale

### Why MediatR?
- **Decouples** command/query handlers from controllers
- **Enables** pipeline behaviors (logging, validation, caching)
- **Facilitates** testing without HTTP context
- **Scales** with features without modifying controllers

### Why Repository Pattern?
- **Abstracts** data access details
- **Enables** swapping implementations (SQL ? NoSQL)
- **Improves** testability with mock repositories

### Why Value Objects?
- **Encapsulates** business rules (e.g., email validation)
- **Immutable** by design
- **Type-safe** instead of primitives

### Why CQRS-Light Pattern?
- **Separates** read/write concerns
- **Optimizes** queries independently
- **Scales** read models separately

### Redis Caching Strategy
```
GET User by ID:
  1. Check Redis (O(1))
  2. If miss, query DB
  3. Populate cache with 15min TTL
  
UPDATE User:
  1. Update database
  2. Invalidate cache key
  3. Return updated entity
```

## ?? Scalability Considerations

### Horizontal Scaling
- **Stateless API**: No session affinity needed
- **Cache layer**: Redis for shared state
- **Database**: EF Core supports read replicas
- **Load balancing**: Deploy multiple API instances behind LB

### Performance Optimizations
- **Lazy loading disabled** (prevent N+1 queries)
- **Explicit includes** in queries
- **Redis caching** for hot data
- **Background jobs** for async work
- **Health checks** for LB routing decisions

### Security at Scale
- **JWT tokens** (no session state)
- **Token rotation** in refresh flow
- **Rate limiting** ready (add Polly)
- **Audit logging** for compliance

### Monitoring & Observability
- **Health endpoints** for diagnostics
- **Structured logging** (Serilog ready)
- **Metrics endpoint** for Prometheus
- **Correlation IDs** for distributed tracing

## ?? Authentication Flow

```
1. Login (POST /auth/login)
   ? Validate credentials
   ? Generate JWT + Refresh token
   ? Return tokens

2. Authenticated Request
   ? Include JWT in Authorization header
   ? Validate token signature & expiry
   ? Extract claims

3. Token Refresh (POST /auth/refresh)
   ? Validate refresh token
   ? Revoke old tokens (optional)
   ? Issue new JWT + Refresh token
```

## ?? Sample Workflows

### Creating a Feature (e.g., User Management)

1. **Domain Layer**
   ```csharp
   // Domain/Entities/User.cs
   public class User : AggregateRoot
   {
       public string Email { get; private set; }
       public void UpdateEmail(Email newEmail) { ... }
   }
   ```

2. **Application Layer**
   ```csharp
   // Application/Commands/CreateUserCommand.cs
   public record CreateUserCommand(string Email, string Password) : IRequest<UserDto>;
   
   // Application/Commands/CreateUserCommandHandler.cs
   public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
   {
       public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken ct)
       {
           var user = User.Create(request.Email, request.Password);
           await _userRepository.AddAsync(user, ct);
           return _mapper.Map<UserDto>(user);
       }
   }
   ```

3. **API Layer**
   ```csharp
   // API/Controllers/UsersController.cs
   [HttpPost]
   public async Task<ActionResult<UserDto>> CreateUser(
       CreateUserCommand command,
       CancellationToken cancellationToken)
   {
       var result = await _mediator.Send(command, cancellationToken);
       return CreatedAtAction(nameof(GetUser), new { id = result.Id }, result);
   }
   ```

4. **Tests**
   ```csharp
   // Tests/Application.Tests/Commands/CreateUserCommandHandlerTests.cs
   [Fact]
   public async Task Handle_ValidEmail_CreatesUser()
   {
       // Arrange
       var command = new CreateUserCommand("test@example.com", "password");
       var handler = new CreateUserCommandHandler(_repository, _mapper);
       
       // Act
       var result = await handler.Handle(command, CancellationToken.None);
       
       // Assert
       Assert.NotNull(result);
       Assert.Equal("test@example.com", result.Email);
   }
   ```

## ?? Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test tests/Domain.Tests

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

## ?? Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| Connection refused (SQL Server) | Ensure SQL Server is running; check appsettings connection string |
| Redis connection error | Verify Redis service is running on localhost:6379 |
| Migration errors | Delete `Migrations` folder, run `dotnet ef migrations add Initial` |
| Swagger not loading | Check API is running; navigate to `/swagger` |

## ?? Technology Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Runtime** | .NET 8 | Modern, performant framework |
| **Web Framework** | ASP.NET Core | HTTP server, dependency injection |
| **ORM** | Entity Framework Core | Data access abstraction |
| **Caching** | Redis | Distributed caching |
| **Messaging** | MediatR | Command/query bus |
| **Authentication** | JWT | Stateless authentication |
| **Documentation** | Swagger/OpenAPI | API documentation |
| **Testing** | xUnit, Moq | Unit & integration tests |
| **Containerization** | Docker | Deployment consistency |
| **Database** | SQL Server | Relational data store |

## ?? References

- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CQRS Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [Entity Framework Core Docs](https://docs.microsoft.com/en-us/ef/core/)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)

## ?? License

MIT License - See LICENSE file for details.

---

**Built with ?? following SOLID principles and Cloud-Native design patterns**
