export type CompanyWorkspaceRole = "owner" | "manager" | "seller" | "support" | "logistics";

export interface CompanyMembershipDto {
  companyId: string;
  userId: string;
  role: CompanyWorkspaceRole;
  isOwner: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface WorkspaceCategoryDto {
  id: number;
  name: string;
  slug: string;
}

export interface CompanyProductDto {
  id: number;
  companyId: string;
  name: string;
  slug: string;
  description: string;
  price: number;
  oldPrice: number | null;
  categoryId: number;
  status: string;
  hasVariants: boolean;
  stock: number;
  minStock: number;
  availableQty: number;
  availabilityStatus: string;
  createdAt: string;
  updatedAt: string;
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
  companyId: string;
  name: string;
  code: string;
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  timeZone: string;
  priority: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateWarehouseRequest {
  name: string;
  code: string;
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
  code: string;
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  timeZone: string;
  priority: number;
}

export interface InventoryStockDto {
  id: number;
  companyId: string;
  warehouseId: number;
  productId: number;
  onHand: number;
  reserved: number;
  available: number;
  reorderPoint: number;
  version: number;
  updatedAt: string;
}

export interface InventoryMovementDto {
  id: number;
  companyId: string;
  warehouseId: number;
  productId: number;
  type: string;
  quantity: number;
  operationId: string;
  reference: string | null;
  reason: string | null;
  actorUserId: string;
  occurredAt: string;
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
  companyId: string;
  userId: string;
  role: CompanyWorkspaceRole;
  isOwner: boolean;
  createdAt: string;
  updatedAt: string;
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
