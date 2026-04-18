# Remote Logger Backend API Specification

## Endpoint: POST /api/ar/logs

Receives batched log entries from the Unity mobile app.

### Request Body

```json
{
  "logs": [
    {
      "timestamp": "2026-01-18T14:30:25.123Z",
      "level": "Error",
      "message": "API Error: Connection timeout",
      "stackTrace": "at WorldApi.FetchZone...",
      "deviceId": "abc123unique",
      "deviceModel": "Samsung SM-G998B",
      "osVersion": "Android 14",
      "appVersion": "1.0.0"
    }
  ]
}
```

### Fields

| Field | Type | Description |
|-------|------|-------------|
| `timestamp` | string | ISO 8601 UTC timestamp |
| `level` | string | Log level: Error, Warning, Info, Exception |
| `message` | string | Log message |
| `stackTrace` | string | Stack trace (for errors/exceptions) |
| `deviceId` | string | Unique device identifier |
| `deviceModel` | string | Device model name |
| `osVersion` | string | Operating system version |
| `appVersion` | string | App version string |

### Response

**Success (200):**
```json
{ "received": 5 }
```

### Server Implementation Example (Node.js/Express)

```javascript
const fs = require('fs');
const path = require('path');

// POST /api/ar/logs
app.post('/api/ar/logs', express.json(), (req, res) => {
    const { logs } = req.body;
    
    if (!Array.isArray(logs)) {
        return res.status(400).json({ error: 'Invalid logs format' });
    }
    
    const logFile = path.join(__dirname, 'logs', `ar-${new Date().toISOString().split('T')[0]}.log`);
    
    const lines = logs.map(log => 
        `[${log.timestamp}] [${log.level}] [${log.deviceModel}] ${log.message}` +
        (log.stackTrace ? `\n${log.stackTrace}` : '')
    ).join('\n---\n');
    
    fs.appendFile(logFile, lines + '\n\n', (err) => {
        if (err) {
            console.error('Failed to write logs:', err);
            return res.status(500).json({ error: 'Failed to write logs' });
        }
        res.json({ received: logs.length });
    });
});
```

### Log File Output Example

```
[2026-01-18T14:30:25.123Z] [Error] [Samsung SM-G998B] API Error: Connection timeout
at WorldApi.FetchZone (WorldApi.cs:78)
at UnityEngine.SetupCoroutine.InvokeMoveNext

[2026-01-18T14:30:30.456Z] [Info] [Samsung SM-G998B] AR System Initialized
```

### Unity Configuration

In Unity scene, add empty GameObject with `RemoteLogger` script:
- **Base URL**: Your server URL
- **Log Endpoint**: `/api/ar/logs`
- **Flush Interval**: 30 seconds
- **Flush On Error**: Checked (immediate send on crashes)
