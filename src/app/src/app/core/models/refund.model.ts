export interface RefundPassengerRequest {
  passengerId: string;
  isVip: boolean;
}

export interface RefundBatchSummaryDto {
  batchId?: string;
  rideId: string;
  reason: string;
  processedAt?: string;
  passengerCount?: number;
}

export interface GetRefundHistoryResponse {
  rideId: string;
  history: RefundBatchSummaryDto[];
}
