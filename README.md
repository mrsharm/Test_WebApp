# CPU Test Web Application

This ASP.NET Core Web API application is designed for testing system resource utilization and monitoring scenarios. It provides endpoints to simulate high CPU usage, memory leaks, and other resource-intensive operations for testing purposes.

## Purpose

This application was created to test:
- CPU monitoring and alerting systems
- Memory usage monitoring 
- Application performance under load
- Site Reliability Engineering (SRE) monitoring tools

## API Endpoints

### CPU Intensive Operations

#### `GET /api/app/work?durationInSeconds={seconds}`
**Purpose:** Simulates high CPU usage for testing CPU monitoring and alerting systems.

**Parameters:**
- `durationInSeconds` (optional): Duration to run CPU-intensive tasks. Default is 10 seconds.

**Behavior:**
- Creates multiple threads (half of available CPU cores, minimum 1)
- Performs mathematical calculations: trigonometric functions, square roots, logarithms
- Executes prime number calculations for additional CPU load
- Returns total iterations performed and computed result

**Example:**
```bash
curl "http://localhost:5000/api/app/work?durationInSeconds=30"
```

**Response:**
```
High CPU task completed! Iterations: 65,702,547, Result: 179905662145257.50 for Duration: 30
```

### Memory Operations

#### `GET /api/app/memleak/{kb}`
**Purpose:** Simulates memory leaks for testing memory monitoring systems.

**Parameters:**
- `kb`: Amount of memory to allocate in kilobytes

**Behavior:**
- Creates customer objects and stores them in a static cache
- Memory is not released, simulating a memory leak
- Each customer object holds a GUID string

#### `GET /api/app/crash`
**Purpose:** Simulates out-of-memory conditions by rapidly allocating large memory blocks.

**Behavior:**
- Continuously allocates 10MB blocks until system runs out of memory
- Stores allocations in a static list to prevent garbage collection
- Will eventually cause application to crash or be killed by OOM killer

#### `GET /api/app/appinvoke`
**Purpose:** Creates memory leaks through event handler subscriptions.

**Behavior:**
- Creates multiple subscriber objects that subscribe to publisher events
- Subscribers are not properly unsubscribed, creating memory leaks
- Each subscriber holds 1MB of data

### Health and Connectivity

#### `GET /api/app/test`
**Purpose:** Tests Azure Storage Account connectivity.

**Behavior:**
- Attempts to connect to configured Azure Storage Account
- Returns success message if connection is established
- Throws exception if connection fails or configuration is missing

## Configuration

The application reads configuration from `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "StorageAccount": "your-storage-connection-string"
  }
}
```

## Deployment

This application is deployed to Azure App Service using the included `main.bicep` Infrastructure as Code template:

- **App Service Plan:** `cpu-app20250714124552Plan` (Free tier F1)
- **Web App Name:** `test_webapp`
- **URL:** `https://test_webapp.azurewebsites.net`

## Monitoring Considerations

### Expected Behaviors for SRE Teams

1. **High CPU Usage:** The `/api/app/work` endpoint is designed to consume significant CPU resources
2. **Memory Growth:** Memory leak endpoints will cause steady memory growth over time
3. **Application Crashes:** The `/api/app/crash` endpoint will terminate the application process

### Recommended Monitoring

- Set up CPU usage alerts with thresholds appropriate for testing scenarios
- Monitor memory usage trends to detect memory leaks
- Configure application restart policies for crash testing
- Track endpoint response times and success rates

## Development

### Building
```bash
dotnet build
```

### Running Locally
```bash
dotnet run --urls http://localhost:5000
```

### Testing Endpoints
```bash
# Test CPU load for 5 seconds
curl "http://localhost:5000/api/app/work?durationInSeconds=5"

# Create 100KB memory leak
curl "http://localhost:5000/api/app/memleak/100"

# Test connectivity
curl "http://localhost:5000/api/app/test"
```

## Safety Warning

⚠️ **Warning:** This application contains endpoints that can consume significant system resources or crash the application. Use with caution and only in appropriate test environments.

- Do not run crash endpoints on production systems
- Monitor system resources when testing CPU and memory endpoints  
- Ensure appropriate resource limits are configured in deployment environments