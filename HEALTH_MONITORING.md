# CPU-App Health Monitoring

This document provides information about the health monitoring endpoints available for the cpu-app Web Application.

## Health Check Endpoints

### 1. Basic Health Check
- **URL**: `/health`
- **Method**: GET
- **Purpose**: Simple health check for load balancers and monitoring systems
- **Response**: `Healthy` or `Unhealthy`

### 2. Status Endpoint
- **URL**: `/api/health/status`
- **Method**: GET
- **Purpose**: JSON status response for monitoring systems
- **Response Format**:
```json
{
  "status": "OK",
  "timestamp": "2025-08-19T21:04:43.9275987Z",
  "service": "cpu-app"
}
```

### 3. Detailed Diagnostics
- **URL**: `/api/health/diagnostics`
- **Method**: GET
- **Purpose**: Comprehensive health and performance metrics for SRE monitoring
- **Response Includes**:
  - Memory usage metrics (Working Set, Private Memory, GC Memory)
  - Garbage collection statistics
  - Performance metrics (CPU time, processor count)
  - Configuration status
  - Application information (uptime, environment, version)

## Application Endpoints

### CPU Intensive Testing
- **URL**: `/api/app/work?durationInSeconds={duration}`
- **Purpose**: Simulates high CPU load for testing performance under stress
- **Parameters**: `durationInSeconds` (optional, defaults to 10)

### Memory Leak Testing
- **URL**: `/api/app/memleak/{kb}`
- **Purpose**: Creates controlled memory leaks for testing memory monitoring
- **Parameters**: `kb` - amount of memory to allocate in KB

### Storage Connectivity Test
- **URL**: `/api/app/test`
- **Purpose**: Tests Azure Storage Account connectivity
- **Note**: Requires `StorageAccount` connection string configuration

## Configuration

### Required Settings
- `StorageAccount` connection string (for storage connectivity tests)

### Environment Variables
- `ASPNETCORE_ENVIRONMENT`: Sets the application environment

## Monitoring Recommendations

1. **Health Monitoring**: Use `/health` endpoint for load balancer health checks
2. **Detailed Monitoring**: Use `/api/health/diagnostics` for comprehensive monitoring
3. **Alerting**: Monitor memory usage and GC pressure through diagnostics endpoint
4. **Performance**: Track CPU time and response times during load testing

## Fixed Issues

- ✅ Fixed infinite loop in crash endpoint that would cause immediate OOM
- ✅ Fixed nullability warnings for better code quality
- ✅ Added proper health check infrastructure
- ✅ Added comprehensive diagnostics and logging