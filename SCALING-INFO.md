# Azure Web App Scaling Information

## Overview
This document provides information about scaling behavior for the `cpu-app` Azure Web App to help SRE agents and operations teams understand expected scaling limitations.

## Current Configuration
- **App Name**: cpu-app (deployed as test_webapp)
- **Service Plan**: cpu-app20250714124552Plan
- **Tier**: Free (F1)
- **Current Instances**: 1
- **Minimum Instances**: 1
- **Maximum Instances**: 1

## Scaling Limitations

### Free Tier (F1) Constraints
- **Auto-scaling**: NOT SUPPORTED
- **Manual scaling**: NOT SUPPORTED  
- **Scale-down below 1 instance**: NOT POSSIBLE
- **Scale-up above 1 instance**: NOT POSSIBLE

### Expected Behavior for SRE Operations
- ✅ **Scale-down requests will ALWAYS fail** - This is expected behavior, not an error
- ✅ **Instance count will always be 1** - This is the minimum and maximum for Free tier
- ✅ **No App Service Plan ID in scaling responses** - Common for Free tier
- ❌ **Scale-down operations should NOT be attempted** on Free tier apps

## API Endpoints for Operations

### Health Check
```
GET /api/app/health
```
Returns overall application health and scaling status.

### Detailed Scaling Status  
```
GET /api/app/scaling-status
```
Returns detailed scaling information specifically for SRE operations:
- Current instance count
- Scaling limits and possibilities
- Recommendations for operations teams
- Status flag for automated systems

## Recommendations

### For SRE Agents
1. **Check tier before attempting scaling operations**
2. **Use the `/api/app/scaling-status` endpoint to determine if scaling is possible**
3. **Treat "already at minimum" as SUCCESS, not FAILURE for Free tier apps**

### For Production Workloads
1. **Upgrade to Basic (B1) or higher tier** to enable scaling
2. **Configure auto-scaling rules** after tier upgrade
3. **Set appropriate minimum instance counts** (≥1 for all tiers)

## Tier Comparison
| Tier | Manual Scaling | Auto-scaling | Min Instances | Max Instances |
|------|----------------|--------------|---------------|---------------|
| Free (F1) | ❌ | ❌ | 1 | 1 |
| Shared (D1) | ❌ | ❌ | 1 | 1 |
| Basic (B1+) | ✅ | ❌ | 1 | 3 |
| Standard (S1+) | ✅ | ✅ | 1 | 10 |
| Premium (P1+) | ✅ | ✅ | 1 | 100 |

## Troubleshooting Scale-Down Issues

### Issue: "Already at minimum instance count"
- **Status**: EXPECTED BEHAVIOR
- **Action**: NO ACTION NEEDED
- **Resolution**: This is normal for Free tier apps

### Issue: "No App Service Plan ID returned"  
- **Status**: EXPECTED BEHAVIOR for Free tier
- **Action**: NO ACTION NEEDED
- **Resolution**: Free tier has limited metadata exposure

### Issue: Scale-down operation fails
- **Status**: EXPECTED BEHAVIOR for Free tier
- **Action**: Update SRE automation to skip scaling for Free tier
- **Resolution**: Check tier before attempting scaling operations

## Contact Information
For questions about scaling configuration or to request tier upgrades, contact the platform team.

---
*Last Updated: 2025-01-19*
*Document Version: 1.0*