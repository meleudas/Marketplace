export type CompanyWorkspaceRole = "owner" | "manager" | "seller" | "support" | "logistics";

export interface CompanyMembershipDto {
  userId?: string;
  role: CompanyWorkspaceRole;
  isOwner: boolean;
  createdAt?: string | null;
  updatedAt?: string | null;
}

export interface WorkspaceCategoryDto {
  id: number;
  name: string;
  slug: string;
}

export interface CompanyProductDto {
  id: string;
  name: string;
  slug: string;
  description: string;
  price: number;
  oldPrice: number | null;
  minStock: number;
  availableQty?: number | null;
  availabilityStatus?: string | null;
  categoryId?: number | null;
  categoryName?: string | null;
  hasVariants: boolean;
  detail?: unknown;
  images?: unknown;
}

export interface UpsertProductRequest {
  name: string;
  slug: string;
  description: string;
  price: number;
  oldPrice: number | null;
  minStock: number;
  categoryId: number;
  hasVariants: boolean;
  detail: unknown;
  images: unknown;
}

export interface WarehouseDto {
  id: string;
  name: string;
  code?: string | null;
  address?: string | null;
  isActive?: boolean;
  createdAt?: string | null;
  updatedAt?: string | null;
}

export interface CreateWarehouseRequest {
  name: string;
  code?: string | null;
  address?: string | null;
}

export interface UpdateWarehouseRequest {
  name: string;
  code?: string | null;
  address?: string | null;
}

export interface InventoryStockDto {
  productId: string;
  warehouseId: string;
  availableQty: number;
  reservedQty?: number;
  onHandQty?: number;
  updatedAt?: string | null;
}

export interface InventoryMovementDto {
  id?: string;
  operationId?: string;
  productId: string;
  movementType?: string;
  quantity: number;
  fromWarehouseId?: string | null;
  toWarehouseId?: string | null;
  createdAt?: string | null;
}

export interface ReceiveStockRequest {
  operationId: string;
  warehouseId: string;
  productId: string;
  quantity: number;
  note?: string | null;
}

export interface ShipStockRequest {
  operationId: string;
  warehouseId: string;
  productId: string;
  quantity: number;
  note?: string | null;
}

export interface AdjustStockRequest {
  operationId: string;
  warehouseId: string;
  productId: string;
  quantityDelta: number;
  reason?: string | null;
}

export interface TransferStockRequest {
  operationId: string;
  fromWarehouseId: string;
  toWarehouseId: string;
  productId: string;
  quantity: number;
  note?: string | null;
}

export interface CreateReservationRequest {
  reservationCode: string;
  warehouseId: string;
  productId: string;
  quantity: number;
  note?: string | null;
}

export interface CompanyMemberDto {
  userId: string;
  role: CompanyWorkspaceRole;
  isOwner: boolean;
  createdAt?: string | null;
  updatedAt?: string | null;
}

export interface CompanyMemberRoleRequest {
  role: CompanyWorkspaceRole;
}

export type WorkspaceMembershipErrorKind = "forbidden" | "notFound" | "unknown";

export class WorkspaceMembershipError extends Error {
  kind: WorkspaceMembershipErrorKind;

  constructor(kind: WorkspaceMembershipErrorKind, message: string) {
    super(message);
    this.kind = kind;
  }
}
