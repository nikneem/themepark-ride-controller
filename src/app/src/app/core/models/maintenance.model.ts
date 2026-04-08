export interface CreateMaintenanceRequestResponse {
  maintenanceId: string;
  rideId: string;
  status: string;
}

export interface CompleteMaintenanceRequestResponse {
  maintenanceId: string;
  rideId: string;
  status: string;
  durationMinutes: number | null;
}

export interface MaintenanceHistoryItem {
  maintenanceId: string;
  rideId: string;
  reason: string;
  status: string;
  requestedAt: string;
  completedAt: string | null;
  durationMinutes: number | null;
}

export interface GetMaintenanceHistoryResponse {
  rideId: string;
  history: MaintenanceHistoryItem[];
}
