export interface QueueStateResponse {
  rideId: string;
  waitingCount: number;
  hasVip: boolean;
  estimatedWaitMinutes: number;
}

export interface PassengerDto {
  passengerId: string;
  isVip: boolean;
}

export interface LoadPassengersResponse {
  passengers: PassengerDto[];
  loadedCount: number;
  vipCount: number;
  remainingInQueue: number;
}
