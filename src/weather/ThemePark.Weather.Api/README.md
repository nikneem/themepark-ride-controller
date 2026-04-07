# ThemePark Weather Service

Autonomous weather simulation microservice for the ThemePark demo application.

## Overview

The Weather Service runs a background timer that randomly generates weather conditions (Calm, Mild, Severe) and publishes Dapr pub/sub events when conditions affect ride zones.

## Configuration

| Key | Default | Description |
|-----|---------|-------------|
| `Weather:SimulationIntervalSeconds` | `60` | How often (seconds) the timer fires |
| `Weather:CalmWeight` | `60` | Relative probability weight for Calm |
| `Weather:MildWeight` | `30` | Relative probability weight for Mild |
| `Weather:SevereWeight` | `10` | Relative probability weight for Severe |
| `Weather:Zones` | `["Zone-A","Zone-B","Zone-C"]` | Available zones for affected zone selection |
| `Dapr:DemoMode` | `false` | Enable the `POST /weather/simulate` endpoint |

## Endpoints

### GET /weather/current

Returns the most recently generated weather condition.

**Response 200 OK:**
```json
{
  "severity": "Mild",
  "affectedZones": ["Zone-A"],
  "generatedAt": "2024-01-01T12:00:00Z"
}
```

### POST /weather/simulate *(demo mode only)*

Force a specific weather condition immediately. Only available when `Dapr:DemoMode = true`.

**Request body:**
```json
{
  "severity": "Severe",
  "affectedZones": ["Zone-A", "Zone-B"]
}
```

**Responses:**
- `202 Accepted` — condition applied
- `400 Bad Request` — invalid severity value
- `404 Not Found` — endpoint not available (DemoMode is false)

## Dapr Pub/Sub

| Topic | Event type | When published |
|-------|-----------|----------------|
| `weather.alert` | `WeatherAlertEvent` | On every Mild or Severe condition (timer tick or manual simulate) |

**Event payload:**
```json
{
  "eventId": "guid",
  "severity": "Severe",
  "affectedZones": ["Zone-A"],
  "generatedAt": "2024-01-01T12:00:00Z"
}
```

## Port

Default port: **5104** (Dapr app-id: `weather-service`)
