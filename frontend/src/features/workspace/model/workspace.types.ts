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
  id: number;
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
  id: number;
  name: string;
  code?: string | null;
  street?: string | null;
  city?: string | null;
  state?: string | null;
  postalCode?: string | null;
  country?: string | null;
  timeZone?: string | null;
  priority?: number | null;
  isActive?: boolean;
  createdAt?: string | null;
  updatedAt?: string | null;
}

export interface CreateWarehouseRequest {
  name: string;
  code?: string | null;
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  timeZone: string;
  priority: number;
}

export interface UpdateWarehouseRequest {
  name: string;
  code?: string | null;
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  timeZone: string;
  priority: number;
}

export interface InventoryStockDto {
  productId: number;
  warehouseId: number;
  availableQty: number;
  reservedQty?: number;
  onHandQty?: number;
  updatedAt?: string | null;
}

export interface InventoryMovementDto {
  id?: string;
  operationId?: string;
  productId: number;
  movementType?: string;
  quantity: number;
  fromWarehouseId?: number | null;
  toWarehouseId?: number | null;
  createdAt?: string | null;
}

export interface ReceiveStockRequest {
  operationId: string;
  warehouseId: number;
  productId: number;
  quantity: number;
  reference?: string | null;
}

export interface ShipStockRequest {
  operationId: string;
  warehouseId: number;
  productId: number;
  quantity: number;
  reference?: string | null;
}

export interface AdjustStockRequest {
  operationId: string;
  warehouseId: number;
  productId: number;
  onHand: number;
  reserved: number;
  reorderPoint: number;
  reason?: string | null;
}

export interface TransferStockRequest {
  operationId: string;
  fromWarehouseId: number;
  toWarehouseId: number;
  productId: number;
  quantity: number;
}

export interface CreateReservationRequest {
   reservationCode: string;
  warehouseId: number;
  productId: number;
   quantity: number;
  ttlMinutes: number;
  reference?: string | null;
}

export interface CompanyMemberDto {
  companyId?: string;
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
