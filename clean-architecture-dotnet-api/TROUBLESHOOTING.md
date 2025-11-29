# Troubleshooting Guide - Application Not Responding

## Issue
Getting "localhost page can't be found" error when trying to access:
- http://localhost:5000
- http://localhost:5000/swagger
- http://localhost:5000/swagger.html

## Root Causes & Solutions

### 1. ? Application Started But Not Listening on Port 5000

**Status from logs:**
```
Application started. Press Ctrl+C to shut down.
Hosting environment: Production
Now listening on: http://localhost:5000
```

The app IS running on port 5000, but the issue is likely one of these:

---

## Solution A: Try HTTPS Instead (Recommended)

The application runs on **HTTPS by default** in development:

```bash
# Try these URLs instead:
https://localhost:7001/swagger
https://localhost:7001/health
https://localhost:7001/api/users
```

**Why?** ASP.NET Core 8 with HTTPS redirection middleware redirects HTTP ? HTTPS.

---

## Solution B: Check if Port is In Use

Port 5000 might be taken by another service:

```bash
# Check what's using port 5000
netstat -ano | findstr :5000

# If port is in use, kill the process or use different port:
dotnet run --project src/API/API.csproj --urls="http://localhost:5555"
```

---

## Solution C: Update Program.cs to Allow HTTP

If you need HTTP (not HTTPS), modify `src/API/Program.cs`:

```csharp
// Find this line:
app.UseHttpsRedirection();

// Comment it out for development:
// app.UseHttpsRedirection();
```

Or configure only for non-development:

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
```

---

## Solution D: Access via Different URL

Try these variations:

```
? https://localhost:7001/swagger
? https://localhost:7001/health
? http://localhost:5000/swagger (if HTTPS redirect is disabled)
? http://localhost:5000 (will redirect to HTTPS)
```

---

## Solution E: Run without HTTPS

```bash
cd clean-architecture-dotnet-api

# Run with HTTP only
dotnet run --project src/API/API.csproj --urls="http://localhost:5000"
```

Or modify `launchSettings.json` if you have one.

---

## Quick Fix - Most Likely

**The app is running and listening, but you need to use HTTPS:**

```
https://localhost:7001/swagger
```

**Then:**
1. Click "Advanced" if you get SSL warning
2. Click "Continue anyway"
3. Swagger UI should load

---

## Verification Steps

### Step 1: Check if app is running
```bash
# Look for process listening on port 5000/7001
netstat -ano | findstr ":5000\|:7001"
```

### Step 2: Try direct HTTP request
```bash
# PowerShell
Invoke-WebRequest -Uri http://localhost:5000/ -SkipCertificateCheck

# CMD
curl http://localhost:5000/
```

### Step 3: Check logs for errors
Look for any error messages in the terminal where app is running

---

## Docker Alternative

If local setup has issues, use Docker Compose:

```bash
cd clean-architecture-dotnet-api

# Make sure Docker Desktop is running, then:
docker-compose up -d

# Access at:
# http://localhost:5000/swagger (no HTTPS needed in Docker)
```

---

## Expected Output When Working

```
???????????????????????????????????????????????????????????
?? Clean Architecture .NET 8 Web API
???????????????????????????????????????????????????????????

? Application started successfully!

?? API Documentation:
   ?? Swagger: https://localhost:7001/swagger
   
?? Health Check:
   ?? https://localhost:7001/health
```

---

## Summary

| Issue | Solution |
|-------|----------|
| **404 on http://localhost:5000** | Use **https://localhost:7001** instead |
| **HTTPS certificate warning** | Click "Continue anyway" (self-signed dev cert) |
| **Connection refused** | App may have crashed - check terminal for errors |
| **Port in use** | Kill process or use `--urls="http://localhost:5555"` |
| **Want HTTP not HTTPS** | Disable `app.UseHttpsRedirection()` in Program.cs |
| **Prefer Docker** | Run `docker-compose up -d` instead |

---

## Access Points Once Working

**Swagger UI** (Interactive API testing)
```
https://localhost:7001/swagger
```

**Health Check**
```
https://localhost:7001/health
```

**Create User** (via curl)
```bash
curl -X POST https://localhost:7001/api/users `
  -H "Content-Type: application/json" `
  -H "Accept-Language: en" `
  -d '{
    "email":"test@example.com",
    "firstName":"John",
    "lastName":"Doe",
    "password":"SecurePassword123!"
  }' `
  -SkipCertificateCheck
```

---

**Most Likely Solution: Change URL to `https://localhost:7001/swagger`**

Try this first! ??

