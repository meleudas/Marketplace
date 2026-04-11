import { apiClient } from "@/shared/api/http.client";
import type {
  AdjustStockRequest,
  CreateReservationRequest,
  CreateWarehouseRequest,
  InventoryMovementDto,
  InventoryStockDto,
  ReceiveStockRequest,
  ShipStockRequest,
  TransferStockRequest,
  UpdateWarehouseRequest,
  WarehouseDto,
} from "@/features/workspace/model/workspace.types";

const extractList = <T>(payload: unknown): T[] => {
  if (Array.isArray(payload)) {
    return payload as T[];
  }

  if (payload && typeof payload === "object") {
    const data = payload as Record<string, unknown>;

    if (Array.isArray(data.value)) {
      return data.value as T[];
    }

    if (Array.isArray(data.items)) {
      return data.items as T[];
    }

    if (Array.isArray(data.data)) {
      return data.data as T[];
    }
  }

  return [];
};

export const getWorkspaceWarehouses = async (companyId: string): Promise<WarehouseDto[]> => {
  const response = await apiClient.get<unknown>(`/companies/${companyId}/warehouses`);
  return extractList<WarehouseDto>(response.data);
};

export const getInventoryStocks = async (companyId: string): Promise<InventoryStockDto[]> => {
  const response = await apiClient.get<unknown>(`/companies/${companyId}/inventory/stocks`);
  return extractList<InventoryStockDto>(response.data);
};

export const getInventoryMovements = async (companyId: string): Promise<InventoryMovementDto[]> => {
  const response = await apiClient.get<unknown>(`/companies/${companyId}/inventory/movements`);
  return extractList<InventoryMovementDto>(response.data);
};

export const createWarehouse = async (
  companyId: string,
  payload: CreateWarehouseRequest,
): Promise<WarehouseDto> => {
  const response = await apiClient.post<WarehouseDto>(`/companies/${companyId}/warehouses`, payload);
  return response.data;
};

export const updateWarehouse = async (
  companyId: string,
  warehouseId: number,
  payload: UpdateWarehouseRequest,
): Promise<WarehouseDto> => {
  const response = await apiClient.put<WarehouseDto>(
    `/companies/${companyId}/warehouses/${warehouseId}`,
    payload,
  );
  return response.data;
};

export const deactivateWarehouse = async (companyId: string, warehouseId: number): Promise<void> => {
  await apiClient.post(`/companies/${companyId}/warehouses/${warehouseId}/deactivate`);
};

export const receiveInventory = async (companyId: string, payload: ReceiveStockRequest): Promise<void> => {
  await apiClient.post(`/companies/${companyId}/inventory/receive`, payload);
};

export const shipInventory = async (companyId: string, payload: ShipStockRequest): Promise<void> => {
  await apiClient.post(`/companies/${companyId}/inventory/ship`, payload);
};

export const adjustInventory = async (companyId: string, payload: AdjustStockRequest): Promise<void> => {
  await apiClient.post(`/companies/${companyId}/inventory/adjust`, payload);
};

export const transferInventory = async (companyId: string, payload: TransferStockRequest): Promise<void> => {
  await apiClient.post(`/companies/${companyId}/inventory/transfer`, payload);
};

export const reserveInventory = async (
  companyId: string,
  payload: CreateReservationRequest,
): Promise<void> => {
  await apiClient.post(`/companies/${companyId}/inventory/reservations`, payload);
};

export const releaseInventoryReservation = async (
  companyId: string,
  reservationCode: string,
): Promise<void> => {
  await apiClient.delete(`/companies/${companyId}/inventory/reservations/${reservationCode}`);
};
