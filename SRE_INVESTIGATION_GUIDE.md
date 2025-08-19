# Azure Web App cpu-app - SRE Investigation Guide

## Overview
This document provides guidance for investigating performance issues with the cpu-app Azure Web App.

## Diagnostic Endpoints

### Health Check
- **Endpoint**: `GET /api/app/health`
- **Purpose**: Quick health status and memory warnings
- **Usage**: Check if the app is experiencing high memory usage or other issues

### Detailed Diagnostics
- **Endpoint**: `GET /api/app/diagnostics`
- **Purpose**: Comprehensive system and process information
- **Returns**: Process info, memory statistics, GC data, system information

### Memory Cleanup
- **Endpoint**: `POST /api/app/cleanup`
- **Purpose**: Emergency memory cleanup for recovery
- **Usage**: Call when memory usage is high to clear accumulated allocations

## Problematic Endpoints

### High CPU Usage
- **Endpoint**: `GET /api/app/work?durationInSeconds={seconds}`
- **Impact**: Consumes significant CPU across multiple threads
- **Default Duration**: 10 seconds
- **Issue**: Can cause 100% CPU usage during execution

### Memory Leaks
1. **Memory Leak via Static Processor**
   - **Endpoint**: `GET /api/app/memleak/{kb}`
   - **Issue**: Creates objects stored in static cache, never released
   
2. **Event Handler Memory Leak**
   - **Endpoint**: `GET /api/app/appinvoke`
   - **Issue**: Creates 2,100 event handlers that are never unsubscribed
   - **Memory Impact**: ~2GB allocation

3. **Large Memory Allocation**
   - **Endpoint**: `GET /api/app/crash`
   - **Issue**: Allocates up to 1GB in 10MB chunks
   - **Fixed**: Now has limits (100 allocations max, 1GB limit)

### Storage Dependency
- **Endpoint**: `GET /api/app/test`
- **Issue**: Requires Azure Storage connection string
- **Potential Problem**: May throw exceptions if connection string missing

## Investigation Steps

1. **Check Health Status**
   ```
   GET /api/app/health
   ```

2. **Get Detailed Diagnostics**
   ```
   GET /api/app/diagnostics
   ```

3. **Review Application Logs**
   - Look for WARNING logs indicating problematic endpoint usage
   - Check for ERROR logs from failed operations

4. **Monitor Resource Usage**
   - CPU usage during `/work` endpoint calls
   - Memory growth from memory leak endpoints
   - Check for out-of-memory conditions

5. **Emergency Recovery**
   ```
   POST /api/app/cleanup
   ```

## Common Issues and Solutions

### High CPU Usage
- **Cause**: Multiple calls to `/api/app/work`
- **Solution**: Monitor endpoint usage, consider rate limiting

### High Memory Usage  
- **Cause**: Calls to memory leak endpoints
- **Solution**: Call cleanup endpoint, restart app if needed

### Application Crashes
- **Cause**: Out of memory from repeated leak endpoint calls
- **Solution**: Use cleanup endpoint before restart

## Preventive Measures

1. **Monitor endpoint usage patterns**
2. **Set up alerts for high CPU/memory usage**
3. **Consider rate limiting on problematic endpoints**
4. **Regular memory cleanup during maintenance windows**

## Azure Portal Monitoring

Monitor these metrics in Azure Portal:
- CPU Percentage
- Memory Percentage  
- Http Server Errors
- Response Time

## Contact

For code changes or additional investigation tools, contact the development team.