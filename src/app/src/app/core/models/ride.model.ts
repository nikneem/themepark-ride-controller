export interface RideDto {
  rideId: string;
  name: string;
  status: string;
}

export interface RideStatusResponse {
  rideId: string;
  name: string;
  status: string;
  workflowStep: string | null;
  activeChaosEvents: string[];
}

export interface StartRideResponse {
  workflowInstanceId: string;
}

export interface RideHistoryEntry {
  sessionId: string;
  rideId: string;
  startedAt: string;
  completedAt: string | null;
  outcome: string;
}
