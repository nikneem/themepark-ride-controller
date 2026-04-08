export interface MascotDto {
  mascotId: string;
  name: string;
  currentZone: string;
  isInRestrictedZone: boolean;
  affectedRideId: string | null;
}

export interface ClearMascotResponse {
  mascotId: string;
  clearedFromRideId: string;
  clearedAt: string;
}
