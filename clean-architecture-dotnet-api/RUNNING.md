# ?? Running the Clean Architecture .NET 8 Web API

## ? Application Status

**Build Status**: ? **SUCCESS**  
**Tests**: ? **7/7 PASSING**  
**Ready to Run**: ? **YES**

---

## ?? How to Run

### Option 1: Docker Compose (Recommended - Easiest)

```bash
# Requires: Docker Desktop running
cd clean-architecture-dotnet-api
docker-compose up -d

# Services will start:
# - API: http://localhost:5000/swagger
# - SQL Server: localhost:1433
# - Redis: localhost:6379
```

### Option 2: Local .NET Development

```bash
cd clean-architecture-dotnet-api

# Build
dotnet build

# Run the API
dotnet run --project src/API/API.csproj

# The app will start on:
# - HTTP: http://localhost:5000
# - HTTPS: https://localhost:7001
# - Swagger: https://localhost:7001/swagger
```

### Option 3: Using InMemory Database (No SQL Server Needed)

If you want to run without SQL Server, you can modify `appsettings.json` to use an in-memory database:

```json
{
  "ConnectionStrings": {
    "UseInMemoryDatabase": "true"
  }
}
```

Then the app will work without any external database.

---

## ?? What to Expect

When the application starts successfully, you'll see:

```
???????????????????????????????????????????????????????????
?? Clean Architecture .NET 8 Web API
???????????????????????????????????????????????????????????

? Application started successfully!

?? API Documentation:
   ?? Swagger: https://localhost:7001/swagger

?? Health Check:
   ?? https://localhost:7001/health

?? Available Endpoints:
   POST   /api/users              - Create a user
   GET    /api/users/{id}         - Get user (requires JWT)
   POST   /api/auth/login         - Login
   POST   /api/auth/refresh       - Refresh token

???????????????????????????????????????????????????????????
```

---

## ?? Testing the API

### 1. **Create a User**
```bash
curl -X POST https://localhost:7001/api/users \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "password": "SecurePassword123!"
  }'
```

**Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "role": "User",
  "isActive": true,
  "createdAtUtc": "2025-11-30T00:00:00Z",
  "updatedAtUtc": null
}
```

### 2. **Login**
```bash
curl -X POST https://localhost:7001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePassword123!"
  }'
```

**Response:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR...",
  "refreshToken": "abcdef123456...",
  "expiresInSeconds": 900,
  "issuedAtUtc": "2025-11-30T00:00:00Z"
}
```

### 3. **Get User (Requires JWT)**
```bash
# Get the access token from login response, then:
curl -X GET https://localhost:7001/api/users/550e8400-e29b-41d4-a716-446655440000 \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN_HERE"
```

### 4. **Using Swagger UI**
1. Navigate to: **https://localhost:7001/swagger**
2. Click the "Authorize" button
3. Enter your JWT token from login
4. Test endpoints directly in UI

---

## ?? Troubleshooting

### Issue: "Connection refused" when starting app

**Cause**: App is trying to connect to SQL Server which isn't installed

**Solutions**:
1. Use Docker Compose: `docker-compose up -d`
2. Install SQL Server locally
3. Use InMemory database (modify appsettings.json)

### Issue: HTTPS certificate warnings

**Cause**: Development certificate issue

**Solution**:
```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

### Issue: Redis connection error

**Cause**: Redis not running (optional service)

**Solution**: App will continue without Redis - caching will be disabled

### Issue: Port 5000 or 7001 already in use

**Solution**:
```bash
# Use different port
dotnet run --project src/API/API.csproj --urls="https://localhost:8001"
```

---

## ?? API Endpoints Reference

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/users` | ? | Create a new user |
| GET | `/api/users/{id}` | ? | Get user by ID |
| POST | `/api/auth/login` | ? | Login and get tokens |
| POST | `/api/auth/refresh` | ? | Refresh access token |
| GET | `/health` | ? | Health check |
| GET | `/swagger` | ? | Swagger UI documentation |

---

## ?? Authentication

### How JWT Works

1. **Login** ? Get access token (valid 15 minutes)
2. **Request** ? Include token in `Authorization: Bearer <token>` header
3. **Token Expires** ? Use refresh token to get new access token
4. **Repeat** ? Keep using refresh endpoint to maintain session

### Test Token
You can generate test tokens via the `/api/auth/login` endpoint.

---

## ?? Next Steps

1. ? Start the application (choose one method above)
2. ? Open Swagger: **https://localhost:7001/swagger**
3. ? Create a test user via `/api/users`
4. ? Login via `/api/auth/login`
5. ? Get user by ID (requires JWT from login)
6. ? Read documentation files:
   - `README.md` - Architecture details
   - `QUICKSTART.md` - Detailed setup guide
   - `INDEX.md` - Navigation guide

---

## ?? Learning Resources

**In the project:**
- `src/Domain/` - Business logic examples
- `src/Application/` - MediatR patterns
- `src/Infrastructure/` - Data access patterns
- `tests/Domain.Tests/` - Testing examples

**External:**
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [EF Core Docs](https://docs.microsoft.com/en-us/ef/core/)

---

## ? Key Features You Can Test

? **User Management** - Create and retrieve users  
? **JWT Authentication** - Secure login with tokens  
? **Input Validation** - Email, password requirements  
? **Error Handling** - Consistent error responses  
? **API Documentation** - Swagger UI  
? **Health Checks** - System status endpoint  
? **Role-Based Auth** - Authorization ready  
? **Async Operations** - Non-blocking API calls  
? **Caching Ready** - Redis integration (optional)  
? **Database Ready** - EF Core + SQL Server  

---

## ?? Support

| Need | File |
|------|------|
| How to run? | This file (RUNNING.md) |
| Architecture details? | README.md |
| Quick setup? | QUICKSTART.md |
| Project navigation? | INDEX.md |
| Code examples? | src/ folder |
| Tests? | tests/ folder |

---

## ? Checklist Before Running

- [ ] .NET 8 SDK installed
- [ ] You're in the `clean-architecture-dotnet-api/` directory
- [ ] Choose one method: Docker / Local / InMemory
- [ ] Follow the instructions for your chosen method
- [ ] Once started, access the Swagger UI

---

**Status**: ? **Ready to Run**

Pick one method above and get started! ??

---

*Last Updated: November 30, 2025*

