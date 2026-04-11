"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { z } from "zod";
import { useAuth } from "@/features/auth/model/auth.store";
import {
  adjustInventory,
  createWarehouse,
  deactivateWarehouse,
  getInventoryMovements,
  getInventoryStocks,
  getWorkspaceWarehouses,
  receiveInventory,
  releaseInventoryReservation,
  reserveInventory,
  shipInventory,
  transferInventory,
  updateWarehouse,
} from "@/features/workspace/api/inventory.api";
import { getWorkspaceProducts } from "@/features/workspace/api/products.api";
import { getMyCompanyMembership } from "@/features/workspace/api/workspace.api";
import { WORKSPACE_COMPANY_ID } from "@/features/workspace/config/workspace.constants";
import { getWorkspaceErrorMessage } from "@/features/workspace/lib/workspace.error";
import {
  adjustFormSchema,
  receiveFormSchema,
  releaseReservationFormSchema,
  reserveFormSchema,
  shipFormSchema,
  transferFormSchema,
  type AdjustFormValues,
  type ReceiveFormValues,
  type ReleaseReservationFormValues,
  type ReserveFormValues,
  type ShipFormValues,
  type TransferFormValues,
} from "@/features/workspace/model/inventory-forms.schema";
import { canWriteInventory } from "@/features/workspace/model/workspace.permissions";
import type {
  CompanyProductDto,
  CompanyMembershipDto,
  InventoryMovementDto,
  InventoryStockDto,
  WarehouseDto,
} from "@/features/workspace/model/workspace.types";
import { WorkspaceMembershipError } from "@/features/workspace/model/workspace.types";
import { WarehouseForm } from "@/features/workspace/ui/WarehouseForm";
import styles from "./WorkspaceScreen.module.css";

const createOperationId = (): string => crypto.randomUUID();

interface TypeaheadOption {
  id: number;
  label: string;
}

interface TypeaheadFieldProps {
  value?: number;
  options: TypeaheadOption[];
  placeholder: string;
  disabled?: boolean;
  onChange: (value?: number) => void;
}

function TypeaheadField({ value, options, placeholder, disabled, onChange }: TypeaheadFieldProps) {
  const [query, setQuery] = useState("");
  const [open, setOpen] = useState(false);

  const selectedOption = useMemo(
    () => options.find((option) => option.id === value),
    [options, value],
  );

  const filtered = useMemo(() => {
    const normalized = query.trim().toLowerCase();
    if (!normalized) {
      return options.slice(0, 20);
    }

    return options
      .filter((option) => option.label.toLowerCase().includes(normalized) || String(option.id).includes(normalized))
      .slice(0, 20);
  }, [options, query]);

  return (
    <div className={styles.typeahead}>
      <input
        className={styles.input}
        value={open ? query : (selectedOption?.label ?? query)}
        disabled={disabled}
        placeholder={placeholder}
        onFocus={() => {
          setOpen(true);
          setQuery(selectedOption?.label ?? "");
        }}
        onChange={(event) => {
          const nextQuery = event.target.value;
          setQuery(nextQuery);

          if (!nextQuery.trim()) {
            onChange(undefined);
          }
        }}
        onBlur={() => {
          window.setTimeout(() => setOpen(false), 120);
        }}
      />
      {open && filtered.length > 0 ? (
        <div className={styles.typeaheadList}>
          {filtered.map((option) => (
            <button
              key={option.id}
              type="button"
              className={styles.typeaheadOption}
              onClick={() => {
                onChange(option.id);
                setQuery(option.label);
                setOpen(false);
              }}
            >
              {option.label}
            </button>
          ))}
        </div>
      ) : null}
    </div>
  );
}

export function WorkspaceInventoryScreen() {
  const user = useAuth((state) => state.user);
  const isGlobalAdmin = user?.role === "admin";

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [membership, setMembership] = useState<CompanyMembershipDto | null>(null);
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([]);
  const [products, setProducts] = useState<CompanyProductDto[]>([]);
  const [stocks, setStocks] = useState<InventoryStockDto[]>([]);
  const [movements, setMovements] = useState<InventoryMovementDto[]>([]);
  const [editingWarehouse, setEditingWarehouse] = useState<WarehouseDto | null>(null);

  const canWrite = useMemo(() => isGlobalAdmin || canWriteInventory(membership), [isGlobalAdmin, membership]);
  const warehouseLabelById = useMemo(
    () => new Map(warehouses.map((warehouse) => [warehouse.id, `${warehouse.name} (${warehouse.code})`])),
    [warehouses],
  );
  const productLabelById = useMemo(
    () => new Map(products.map((product) => [product.id, product.name])),
    [products],
  );
  const warehouseOptions = useMemo<TypeaheadOption[]>(
    () => warehouses.map((warehouse) => ({ id: warehouse.id, label: `${warehouse.name} (${warehouse.code})` })),
    [warehouses],
  );
  const productOptions = useMemo<TypeaheadOption[]>(
    () => products.map((product) => ({ id: product.id, label: `${product.name} (#${product.id})` })),
    [products],
  );

    const receiveForm = useForm<z.input<typeof receiveFormSchema>, unknown, ReceiveFormValues>({
        resolver: zodResolver(receiveFormSchema),
        defaultValues: { quantity: 1, reference: "" },
    });

    const shipForm = useForm<z.input<typeof shipFormSchema>, unknown, ShipFormValues>({
        resolver: zodResolver(shipFormSchema),
        defaultValues: { quantity: 1, reference: "" },
    });

    const adjustForm = useForm<z.input<typeof adjustFormSchema>, unknown, AdjustFormValues>({
        resolver: zodResolver(adjustFormSchema),
        defaultValues: { reason: "" },
    });

    const transferForm = useForm<z.input<typeof transferFormSchema>, unknown, TransferFormValues>({
        resolver: zodResolver(transferFormSchema),
        defaultValues: { quantity: 1 },
    });

    const reserveForm = useForm<z.input<typeof reserveFormSchema>, unknown, ReserveFormValues>({
        resolver: zodResolver(reserveFormSchema),
        defaultValues: { quantity: 1, ttlMinutes: 15, reference: "" },
    });

    const releaseForm = useForm<z.input<typeof releaseReservationFormSchema>, unknown, ReleaseReservationFormValues>({
        resolver: zodResolver(releaseReservationFormSchema),
        defaultValues: { reservationCode: "" },
    });

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      let membershipData: CompanyMembershipDto | null = null;

      if (!isGlobalAdmin) {
        membershipData = await getMyCompanyMembership(WORKSPACE_COMPANY_ID);
      } else {
        try {
          membershipData = await getMyCompanyMembership(WORKSPACE_COMPANY_ID);
        } catch (membershipError) {
          if (
            !(membershipError instanceof WorkspaceMembershipError) ||
            (membershipError.kind !== "forbidden" && membershipError.kind !== "notFound")
          ) {
            throw membershipError;
          }
        }
      }

      const [warehousesData, productsData, stocksData, movementsData] = await Promise.all([
        getWorkspaceWarehouses(WORKSPACE_COMPANY_ID),
        getWorkspaceProducts(WORKSPACE_COMPANY_ID),
        getInventoryStocks(WORKSPACE_COMPANY_ID),
        getInventoryMovements(WORKSPACE_COMPANY_ID),
      ]);

      setMembership(membershipData);
      setWarehouses(warehousesData);
      setProducts(productsData);
      setStocks(stocksData);
      setMovements(movementsData);
    } catch (loadError) {
      if (
        !isGlobalAdmin &&
        loadError instanceof WorkspaceMembershipError &&
        ["forbidden", "notFound"].includes(loadError.kind)
      ) {
        setError("You do not have access to this company workspace");
      } else {
        setError(getWorkspaceErrorMessage(loadError, "Failed to load inventory"));
      }
    } finally {
      setLoading(false);
    }
  }, [isGlobalAdmin]);

  useEffect(() => {
    void load();
  }, [load]);

  const runWrite = async (action: () => Promise<unknown>, successMessage: string) => {
    try {
      setSaving(true);
      setFeedback(null);
      await action();
      setFeedback(successMessage);
      await load();
    } catch (actionError) {
      setFeedback(getWorkspaceErrorMessage(actionError, "Action failed"));
    } finally {
      setSaving(false);
    }
  };

  const pickStockForActions = (warehouseId: number, productId: number) => {
    receiveForm.setValue("warehouseId", warehouseId);
    receiveForm.setValue("productId", productId);
    shipForm.setValue("warehouseId", warehouseId);
    shipForm.setValue("productId", productId);
    adjustForm.setValue("warehouseId", warehouseId);
    adjustForm.setValue("productId", productId);
    reserveForm.setValue("warehouseId", warehouseId);
    reserveForm.setValue("productId", productId);
    transferForm.setValue("fromWarehouseId", warehouseId);
    transferForm.setValue("productId", productId);
    setFeedback("Selected stock values were copied into inventory forms.");
  };

  if (loading) {
    return <p className={styles.state}>Loading inventory...</p>;
  }

  if (error) {
    return <p className={styles.state}>{error}</p>;
  }

  return (
    <div className={styles.stack}>
      <section className={styles.card}>
        <div className={styles.cardHeader}>
          <h2 className={styles.sectionTitle}>Inventory</h2>
          <p className={styles.muted}>Track warehouses, stocks, and inventory movements.</p>
        </div>
        <div className={styles.metaGrid}>
          <p className={styles.metaItem}>Company ID: {WORKSPACE_COMPANY_ID}</p>
          <p className={styles.metaItem}>My role: {membership?.role ?? (isGlobalAdmin ? "admin" : "unknown")}</p>
        </div>
        {!canWrite ? <p className={styles.hint}>Read-only mode for your role.</p> : null}
        {feedback ? <p className={styles.feedback}>{feedback}</p> : null}
      </section>

      {canWrite ? (
        <section className={styles.card}>
          <h3 className={styles.subTitle}>Create warehouse</h3>
          <WarehouseForm
            submitLabel="Create warehouse"
            busy={saving}
            onSubmit={async (payload) => {
              await runWrite(async () => createWarehouse(WORKSPACE_COMPANY_ID, payload), "Warehouse created.");
            }}
          />
        </section>
      ) : null}

      {editingWarehouse && canWrite ? (
        <section className={styles.card}>
          <div className={styles.rowBetween}>
            <h3 className={styles.subTitle}>Update warehouse: {editingWarehouse.name}</h3>
            <button type="button" className={styles.ghostButton} onClick={() => setEditingWarehouse(null)}>
              Cancel
            </button>
          </div>

          <WarehouseForm
            initialWarehouse={editingWarehouse}
            submitLabel="Update warehouse"
            busy={saving}
            onSubmit={async (payload) => {
              await runWrite(
                async () => updateWarehouse(WORKSPACE_COMPANY_ID, editingWarehouse.id, payload),
                "Warehouse updated.",
              );
              setEditingWarehouse(null);
            }}
          />
        </section>
      ) : null}

      <section className={styles.card}>
        <h3 className={styles.subTitle}>Warehouses</h3>
        <p className={styles.muted}>Warehouse list with status and update timestamp.</p>
        {warehouses.length === 0 ? (
          <p className={styles.state}>No warehouses found</p>
        ) : (
          <div className={styles.tableWrap}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Code</th>
                  <th>Address</th>
                  <th>Active</th>
                  <th>Updated</th>
                  {canWrite ? <th>Actions</th> : null}
                </tr>
              </thead>
              <tbody>
                {warehouses.map((warehouse) => (
                  <tr key={warehouse.id}>
                    <td>{warehouse.name}</td>
                    <td>{warehouse.code ?? "-"}</td>
                    <td>
                      {[warehouse.street, warehouse.city, warehouse.state, warehouse.postalCode, warehouse.country]
                        .filter(Boolean)
                        .join(", ") || "-"}
                    </td>
                    <td>{warehouse.isActive === false ? "No" : "Yes"}</td>
                    <td>{warehouse.updatedAt ?? "-"}</td>
                    {canWrite ? (
                      <td className={styles.actionsInline}>
                        <button
                          type="button"
                          className={styles.ghostButton}
                          disabled={saving}
                          onClick={() => setEditingWarehouse(warehouse)}
                        >
                          Edit
                        </button>
                        <button
                          type="button"
                          className={styles.dangerButton}
                          disabled={saving}
                          onClick={() => {
                            void runWrite(
                              async () => deactivateWarehouse(WORKSPACE_COMPANY_ID, warehouse.id),
                              "Warehouse deactivated.",
                            );
                          }}
                        >
                          Deactivate
                        </button>
                      </td>
                    ) : null}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className={styles.card}>
        <h3 className={styles.subTitle}>Stocks</h3>
        <p className={styles.muted}>Current stock balances by product and warehouse.</p>
        {stocks.length === 0 ? (
          <p className={styles.state}>No stocks found</p>
        ) : (
          <div className={styles.tableWrap}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>Product ID</th>
                  <th>Warehouse ID</th>
                  <th>Available</th>
                  <th>Reserved</th>
                  <th>On hand</th>
                  <th>Updated</th>
                  {canWrite ? <th>Quick action</th> : null}
                </tr>
              </thead>
              <tbody>
                {stocks.map((stock) => (
                  <tr key={`${stock.productId}-${stock.warehouseId}`}>
                    <td>{`${stock.productId} - ${productLabelById.get(stock.productId) ?? "Unknown product"}`}</td>
                    <td>{`${stock.warehouseId} - ${warehouseLabelById.get(stock.warehouseId) ?? "Unknown warehouse"}`}</td>
                    <td>{stock.available}</td>
                    <td>{stock.reserved}</td>
                    <td>{stock.onHand}</td>
                    <td>{stock.updatedAt ?? "-"}</td>
                    {canWrite ? (
                      <td>
                        <button
                          type="button"
                          className={styles.ghostButton}
                          disabled={saving}
                          onClick={() => pickStockForActions(stock.warehouseId, stock.productId)}
                        >
                          Use in forms
                        </button>
                      </td>
                    ) : null}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className={styles.card}>
        <h3 className={styles.subTitle}>Movements</h3>
        <p className={styles.muted}>Recent inventory operations and transfer history.</p>
        {movements.length === 0 ? (
          <p className={styles.state}>No movements found</p>
        ) : (
          <div className={styles.tableWrap}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>Operation</th>
                  <th>Type</th>
                  <th>Warehouse</th>
                  <th>Product ID</th>
                  <th>Qty</th>
                  <th>Reference</th>
                  <th>Occurred</th>
                </tr>
              </thead>
              <tbody>
                {movements.map((movement) => (
                  <tr key={movement.id}>
                    <td>{movement.operationId}</td>
                    <td>{movement.type}</td>
                    <td>{movement.warehouseId}</td>
                    <td>{movement.productId}</td>
                    <td>{movement.quantity}</td>
                    <td>{movement.reference ?? "-"}</td>
                    <td>{movement.occurredAt}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {canWrite ? (
        <section className={styles.card}>
          <h3 className={styles.subTitle}>Inventory actions</h3>
          <p className={styles.muted}>
            Choose warehouse and product from dropdowns. You can also click &quot;Use in forms&quot; in Stocks table.
          </p>

          <div className={styles.formColumns}>
            <form
              className={`${styles.formGrid} ${styles.actionCard}`}
              onSubmit={receiveForm.handleSubmit(async (values) => {
                await runWrite(
                  async () =>
                    receiveInventory(WORKSPACE_COMPANY_ID, {
                      operationId: createOperationId(),
                      warehouseId: values.warehouseId,
                      productId: values.productId,
                      quantity: Number(values.quantity),
                      reference: values.reference?.trim() ? values.reference : null,
                    }),
                  "Stock received.",
                );
              })}
            >
              <h4 className={styles.miniTitle}>Receive</h4>
              <Controller
                control={receiveForm.control}
                name="warehouseId"
                render={({ field }) => (
                  <TypeaheadField
                    value={field.value}
                    options={warehouseOptions}
                    placeholder="Type warehouse name or ID"
                    disabled={saving}
                    onChange={(nextValue) => field.onChange(nextValue ?? Number.NaN)}
                  />
                )}
              />
              {receiveForm.formState.errors.warehouseId ? (
                <span className={styles.error}>{receiveForm.formState.errors.warehouseId.message}</span>
              ) : null}
              <Controller
                control={receiveForm.control}
                name="productId"
                render={({ field }) => (
                  <TypeaheadField
                    value={field.value}
                    options={productOptions}
                    placeholder="Type product name or ID"
                    disabled={saving}
                    onChange={(nextValue) => field.onChange(nextValue ?? Number.NaN)}
                  />
                )}
              />
              {receiveForm.formState.errors.productId ? (
                <span className={styles.error}>{receiveForm.formState.errors.productId.message}</span>
              ) : null}
              <input
                type="number"
                className={styles.input}
                placeholder="Quantity"
                {...receiveForm.register("quantity", { valueAsNumber: true })}
              />
              <input className={styles.input} placeholder="Reference" {...receiveForm.register("reference")} />
              <button type="submit" className={styles.primaryButton} disabled={saving}>
                Receive
              </button>
            </form>

            <form
              className={`${styles.formGrid} ${styles.actionCard}`}
              onSubmit={shipForm.handleSubmit(async (values) => {
                await runWrite(
                  async () =>
                    shipInventory(WORKSPACE_COMPANY_ID, {
                      operationId: createOperationId(),
                      warehouseId: values.warehouseId,
                      productId: values.productId,
                      quantity: Number(values.quantity),
                      reference: values.reference?.trim() ? values.reference : null,
                    }),
                  "Stock shipped.",
                );
              })}
            >
              <h4 className={styles.miniTitle}>Ship</h4>
              <Controller
                control={shipForm.control}
                name="warehouseId"
                render={({ field }) => (
                  <TypeaheadField
                    value={field.value}
                    options={warehouseOptions}
                    placeholder="Type warehouse name or ID"
                    disabled={saving}
                    onChange={(nextValue) => field.onChange(nextValue ?? Number.NaN)}
                  />
                )}
              />
              {shipForm.formState.errors.warehouseId ? (
                <span className={styles.error}>{shipForm.formState.errors.warehouseId.message}</span>
              ) : null}
              <Controller
                control={shipForm.control}
                name="productId"
                render={({ field }) => (
                  <TypeaheadField
                    value={field.value}
                    options={productOptions}
                    placeholder="Type product name or ID"
                    disabled={saving}
                    onChange={(nextValue) => field.onChange(nextValue ?? Number.NaN)}
                  />
                )}
              />
              {shipForm.formState.errors.productId ? (
                <span className={styles.error}>{shipForm.formState.errors.productId.message}</span>
              ) : null}
              <input
                type="number"
                className={styles.input}
                placeholder="Quantity"
                {...shipForm.register("quantity", { valueAsNumber: true })}
              />
              <input className={styles.input} placeholder="Reference" {...shipForm.register("reference")} />
              <button type="submit" className={styles.primaryButton} disabled={saving}>
                Ship
              </button>
            </form>

            <form
              className={`${styles.formGrid} ${styles.actionCard}`}
              onSubmit={adjustForm.handleSubmit(async (values) => {
                await runWrite(
                  async () =>
                    adjustInventory(WORKSPACE_COMPANY_ID, {
                      operationId: createOperationId(),
                      warehouseId: values.warehouseId,
                      productId: values.productId,
                      onHand: Number(values.onHand),
                      reserved: Number(values.reserved),
                      reorderPoint: Number(values.reorderPoint),
                      reason: values.reason?.trim() ? values.reason : null,
                    }),
                  "Stock adjusted.",
                );
              })}
            >
              <h4 className={styles.miniTitle}>Adjust</h4>
              <Controller
                control={adjustForm.control}
                name="warehouseId"
                render={({ field }) => (
                  <TypeaheadField
                    value={field.value}
                    options={warehouseOptions}
                    placeholder="Type warehouse name or ID"
                    disabled={saving}
                    onChange={(nextValue) => field.onChange(nextValue ?? Number.NaN)}
                  />
                )}
              />
              {adjustForm.formState.errors.warehouseId ? (
                <span className={styles.error}>{adjustForm.formState.errors.warehouseId.message}</span>
              ) : null}
              <Controller
                control={adjustForm.control}
                name="productId"
                render={({ field }) => (
                  <TypeaheadField
                    value={field.value}
                    options={productOptions}
                    placeholder="Type product name or ID"
                    disabled={saving}
                    onChange={(nextValue) => field.onChange(nextValue ?? Number.NaN)}
                  />
                )}
              />
              {adjustForm.formState.errors.productId ? (
                <span className={styles.error}>{adjustForm.formState.errors.productId.message}</span>
              ) : null}
              <input
                type="number"
                className={styles.input}
                placeholder="On hand"
                {...adjustForm.register("onHand", { valueAsNumber: true })}
              />
              <input
                type="number"
                className={styles.input}
                placeholder="Reserved"
                {...adjustForm.register("reserved", { valueAsNumber: true })}
              />
              <input
                type="number"
                className={styles.input}
                placeholder="Reorder point"
                {...adjustForm.register("reorderPoint", { valueAsNumber: true })}
              />
              <input className={styles.input} placeholder="Reason" {...adjustForm.register("reason")} />
              <button type="submit" className={styles.primaryButton} disabled={saving}>
                Adjust
              </button>
            </form>

            <form
              className={`${styles.formGrid} ${styles.actionCard}`}
              onSubmit={transferForm.handleSubmit(async (values) => {
                await runWrite(
                  async () =>
                    transferInventory(WORKSPACE_COMPANY_ID, {
                      operationId: createOperationId(),
                      fromWarehouseId: values.fromWarehouseId,
                      toWarehouseId: values.toWarehouseId,
                      productId: values.productId,
                      quantity: Number(values.quantity),
                    }),
                  "Stock transferred.",
                );
              })}
            >
              <h4 className={styles.miniTitle}>Transfer</h4>
              <Controller
                control={transferForm.control}
                name="fromWarehouseId"
                render={({ field }) => (
                  <TypeaheadField
                    value={field.value}
                    options={warehouseOptions}
                    placeholder="Type source warehouse"
                    disabled={saving}
                    onChange={(nextValue) => field.onChange(nextValue ?? Number.NaN)}
                  />
                )}
              />
              {transferForm.formState.errors.fromWarehouseId ? (
                <span className={styles.error}>{transferForm.formState.errors.fromWarehouseId.message}</span>
              ) : null}
              <Controller
                control={transferForm.control}
                name="toWarehouseId"
                render={({ field }) => (
                  <TypeaheadField
                    value={field.value}
                    options={warehouseOptions}
                    placeholder="Type destination warehouse"
                    disabled={saving}
                    onChange={(nextValue) => field.onChange(nextValue ?? Number.NaN)}
                  />
                )}
              />
              {transferForm.formState.errors.toWarehouseId ? (
                <span className={styles.error}>{transferForm.formState.errors.toWarehouseId.message}</span>
              ) : null}
              <Controller
                control={transferForm.control}
                name="productId"
                render={({ field }) => (
                  <TypeaheadField
                    value={field.value}
                    options={productOptions}
                    placeholder="Type product name or ID"
                    disabled={saving}
                    onChange={(nextValue) => field.onChange(nextValue ?? Number.NaN)}
                  />
                )}
              />
              {transferForm.formState.errors.productId ? (
                <span className={styles.error}>{transferForm.formState.errors.productId.message}</span>
              ) : null}
              <input
                type="number"
                className={styles.input}
                placeholder="Quantity"
                {...transferForm.register("quantity", { valueAsNumber: true })}
              />
              <button type="submit" className={styles.primaryButton} disabled={saving}>
                Transfer
              </button>
            </form>

            <form
              className={`${styles.formGrid} ${styles.actionCard}`}
              onSubmit={reserveForm.handleSubmit(async (values) => {
                await runWrite(
                  async () =>
                    reserveInventory(WORKSPACE_COMPANY_ID, {
                      reservationCode: createOperationId(),
                      warehouseId: values.warehouseId,
                      productId: values.productId,
                      quantity: Number(values.quantity),
                      ttlMinutes: Number(values.ttlMinutes),
                      reference: values.reference?.trim() ? values.reference : null,
                    }),
                  "Reservation created.",
                );
              })}
            >
              <h4 className={styles.miniTitle}>Reserve</h4>
              <Controller
                control={reserveForm.control}
                name="warehouseId"
                render={({ field }) => (
                  <TypeaheadField
                    value={field.value}
                    options={warehouseOptions}
                    placeholder="Type warehouse name or ID"
                    disabled={saving}
                    onChange={(nextValue) => field.onChange(nextValue ?? Number.NaN)}
                  />
                )}
              />
              {reserveForm.formState.errors.warehouseId ? (
                <span className={styles.error}>{reserveForm.formState.errors.warehouseId.message}</span>
              ) : null}
              <Controller
                control={reserveForm.control}
                name="productId"
                render={({ field }) => (
                  <TypeaheadField
                    value={field.value}
                    options={productOptions}
                    placeholder="Type product name or ID"
                    disabled={saving}
                    onChange={(nextValue) => field.onChange(nextValue ?? Number.NaN)}
                  />
                )}
              />
              {reserveForm.formState.errors.productId ? (
                <span className={styles.error}>{reserveForm.formState.errors.productId.message}</span>
              ) : null}
              <input
                type="number"
                className={styles.input}
                placeholder="Quantity"
                {...reserveForm.register("quantity", { valueAsNumber: true })}
              />
              <input
                type="number"
                className={styles.input}
                placeholder="TTL minutes"
                {...reserveForm.register("ttlMinutes", { valueAsNumber: true })}
              />
              <input className={styles.input} placeholder="Reference" {...reserveForm.register("reference")} />
              <button type="submit" className={styles.primaryButton} disabled={saving}>
                Reserve
              </button>
            </form>

            <form
              className={`${styles.formGrid} ${styles.actionCard}`}
              onSubmit={releaseForm.handleSubmit(async (values) => {
                await runWrite(
                  async () =>
                    releaseInventoryReservation(WORKSPACE_COMPANY_ID, values.reservationCode),
                  "Reservation released.",
                );
              })}
            >
              <h4 className={styles.miniTitle}>Release reservation</h4>
              <input
                className={styles.input}
                placeholder="Reservation code"
                {...releaseForm.register("reservationCode")}
              />
              {releaseForm.formState.errors.reservationCode ? (
                <span className={styles.error}>{releaseForm.formState.errors.reservationCode.message}</span>
              ) : null}
              <button type="submit" className={styles.primaryButton} disabled={saving}>
                Release
              </button>
            </form>
          </div>
        </section>
      ) : null}
    </div>
  );
}

