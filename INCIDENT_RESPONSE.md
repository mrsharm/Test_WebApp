# Incident Response Guide for contoso-chat-net

## Resource Details
- **Name**: contoso-chat-net
- **Type**: Azure Web App
- **Location**: westus
- **Resource Group**: mrsharm-operations-agent-3p-rg
- **Subscription ID**: be8d491e-109c-4ee1-aaee-dc7615af0a42

## Diagnostic Endpoints

### Health Checks
- `GET /health` - Basic health check
- `GET /health/ready` - Readiness probe
- `GET /health/live` - Liveness probe

### System Diagnostics
- `GET /api/diagnostics/system-info` - Comprehensive system information
- `GET /api/diagnostics/memory-pressure` - Memory usage analysis
- `GET /api/diagnostics/incident-logs` - Incident-specific information
- `POST /api/diagnostics/force-gc` - Manual garbage collection (emergency use)

## Known Problematic Endpoints ⚠️

### CRITICAL - Application Crash Risk
- **Endpoint**: `GET /api/app/crash`
- **Risk**: Causes OutOfMemory exception and application crash
- **Description**: Infinite loop allocating 10MB blocks until memory exhausted
- **Response**: Immediately disable endpoint, restart application if crashed

### HIGH - Memory Leak Risk
- **Endpoint**: `GET /api/app/memleak/{kb}`
- **Risk**: Accumulates objects in static cache causing memory leaks
- **Description**: Adds customer objects to never-cleared static cache
- **Monitoring**: Watch memory usage trends, GC pressure
- **Response**: Restart application, monitor `/api/diagnostics/memory-pressure`

- **Endpoint**: `GET /api/app/appinvoke`
- **Risk**: Creates 2100 event subscribers with 1MB each (~2.1GB leak)
- **Description**: Event handlers never unsubscribed, objects remain in memory
- **Monitoring**: Memory usage spikes after calls
- **Response**: Monitor memory, restart if excessive memory usage

### MEDIUM - Performance Impact
- **Endpoint**: `GET /api/app/work?durationInSeconds={n}`
- **Risk**: High CPU usage for specified duration
- **Description**: CPU-intensive mathematical operations across multiple threads
- **Monitoring**: CPU usage, response times
- **Response**: Consider rate limiting, monitor system load

- **Endpoint**: `GET /api/app/test`
- **Risk**: Configuration-dependent failures
- **Description**: Requires Azure Storage connection string
- **Monitoring**: Exception logs, configuration validation
- **Response**: Verify connection string configuration

## Incident Investigation Process

### 1. Initial Assessment
```bash
# Check application health
curl https://contoso-chat-net.azurewebsites.net/health

# Get system information
curl https://contoso-chat-net.azurewebsites.net/api/diagnostics/system-info

# Check memory pressure
curl https://contoso-chat-net.azurewebsites.net/api/diagnostics/memory-pressure
```

### 2. Memory Issues Investigation
- Monitor memory usage trends in Azure portal
- Check GC collection frequency and pressure
- Use `/api/diagnostics/memory-pressure` to assess current state
- Review recent calls to `/api/app/memleak/*` or `/api/app/appinvoke`

### 3. Performance Issues Investigation
- Check CPU utilization in Azure portal
- Review calls to `/api/app/work` endpoint
- Monitor response times and concurrent requests

### 4. Application Crashes
- Check application logs for OutOfMemory exceptions
- Verify if `/api/app/crash` endpoint was called
- Monitor application restart events

## Emergency Response Actions

### Memory Pressure
1. Force garbage collection: `POST /api/diagnostics/force-gc`
2. Monitor improvement via `/api/diagnostics/memory-pressure`
3. If no improvement, restart the application
4. Scale up instance size if recurring issue

### Application Crash
1. Check Azure portal for crash details
2. Review application logs for OutOfMemory exceptions
3. Restart the application
4. Disable problematic endpoints if necessary

### High CPU Usage
1. Monitor current CPU load
2. Check for active calls to `/api/app/work`
3. Implement rate limiting if needed
4. Scale out with additional instances

## Monitoring and Alerting

### Key Metrics to Monitor
- Memory usage (Working Set, Private Memory)
- CPU utilization
- Request response times
- Error rates and exceptions
- GC collection frequency

### Recommended Alerts
- Memory usage > 80% of available
- CPU usage > 85% for >5 minutes
- Error rate > 5% for >2 minutes
- OutOfMemory exceptions (immediate alert)
- Application restart events

## Prevention Measures

1. **Endpoint Safety**: Disable or rate-limit dangerous endpoints in production
2. **Resource Limits**: Implement memory and CPU limits
3. **Health Checks**: Use health check endpoints for monitoring
4. **Logging**: Comprehensive logging for all operations
5. **Monitoring**: Continuous monitoring of key metrics

## Contact Information
- **Assigned to**: mrsharm (for triage and resolution)
- **SRE Agent Link**: [Azure Portal Link](https://portal.azure.com/?feature.customPortal=false&feature.canmodifystamps=true&feature.fastmanifest=false&nocdn=force&websitesextension_loglevel=verbose&Microsoft_Azure_PaasServerless=beta&microsoft_azure_paasserverless_assettypeoptions=%7B%22SreAgentCustomMenu%22%3A%7B%22options%22%3A%22%22%7D%7D#view/Microsoft_Azure_PaasServerless/AgentFrameBlade.ReactView/id/%2Fsubscriptions%2F%2FresourceGroups%2F%2Fproviders%2FMicrosoft.App%2Fagents%2F/sreLink/%2Fviews%2Factivities%2Fthreads%2F22499ac3-4d0d-4bd9-8a0d-d55dca0b0ca7)