"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
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
  CompanyMembershipDto,
  InventoryMovementDto,
  InventoryStockDto,
  WarehouseDto,
} from "@/features/workspace/model/workspace.types";
import { WorkspaceMembershipError } from "@/features/workspace/model/workspace.types";
import { WarehouseForm } from "@/features/workspace/ui/WarehouseForm";
import styles from "./WorkspaceScreen.module.css";

const createOperationId = (): string => crypto.randomUUID();

export function WorkspaceInventoryScreen() {
  const user = useAuth((state) => state.user);
  const isGlobalAdmin = user?.role === "admin";

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [membership, setMembership] = useState<CompanyMembershipDto | null>(null);
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([]);
  const [stocks, setStocks] = useState<InventoryStockDto[]>([]);
  const [movements, setMovements] = useState<InventoryMovementDto[]>([]);
  const [editingWarehouse, setEditingWarehouse] = useState<WarehouseDto | null>(null);

  const canWrite = useMemo(() => isGlobalAdmin || canWriteInventory(membership), [isGlobalAdmin, membership]);

  const receiveForm = useForm<z.input<typeof receiveFormSchema>, unknown, ReceiveFormValues>({
    resolver: zodResolver(receiveFormSchema),
    defaultValues: { warehouseId: "", productId: "", quantity: 1, note: "" },
  });

  const shipForm = useForm<z.input<typeof shipFormSchema>, unknown, ShipFormValues>({
    resolver: zodResolver(shipFormSchema),
    defaultValues: { warehouseId: "", productId: "", quantity: 1, note: "" },
  });

  const adjustForm = useForm<z.input<typeof adjustFormSchema>, unknown, AdjustFormValues>({
    resolver: zodResolver(adjustFormSchema),
    defaultValues: { warehouseId: "", productId: "", quantityDelta: 0, reason: "" },
  });

  const transferForm = useForm<z.input<typeof transferFormSchema>, unknown, TransferFormValues>({
    resolver: zodResolver(transferFormSchema),
    defaultValues: { fromWarehouseId: "", toWarehouseId: "", productId: "", quantity: 1, note: "" },
  });

  const reserveForm = useForm<z.input<typeof reserveFormSchema>, unknown, ReserveFormValues>({
    resolver: zodResolver(reserveFormSchema),
    defaultValues: { warehouseId: "", productId: "", quantity: 1, note: "" },
  });

  const releaseForm = useForm<z.input<typeof releaseReservationFormSchema>, unknown, ReleaseReservationFormValues>({
    resolver: zodResolver(releaseReservationFormSchema),
    defaultValues: { reservationCode: "" },
  });

  const load = async () => {
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

      const [warehousesData, stocksData, movementsData] = await Promise.all([
        getWorkspaceWarehouses(WORKSPACE_COMPANY_ID),
        getInventoryStocks(WORKSPACE_COMPANY_ID),
        getInventoryMovements(WORKSPACE_COMPANY_ID),
      ]);

      setMembership(membershipData);
      setWarehouses(warehousesData);
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
  };

  useEffect(() => {
    void load();
  }, [isGlobalAdmin]);

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

  if (loading) {
    return <p className={styles.state}>Loading inventory...</p>;
  }

  if (error) {
    return <p className={styles.state}>{error}</p>;
  }

  return (
    <div className={styles.stack}>
      <section className={styles.card}>
        <h2 className={styles.sectionTitle}>Inventory</h2>
        <p className={styles.row}>Company ID: {WORKSPACE_COMPANY_ID}</p>
        <p className={styles.row}>My role: {membership?.role ?? (isGlobalAdmin ? "admin" : "unknown")}</p>
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
                    <td>{warehouse.address ?? "-"}</td>
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
                </tr>
              </thead>
              <tbody>
                {stocks.map((stock) => (
                  <tr key={`${stock.productId}-${stock.warehouseId}`}>
                    <td>{stock.productId}</td>
                    <td>{stock.warehouseId}</td>
                    <td>{stock.availableQty}</td>
                    <td>{stock.reservedQty ?? "-"}</td>
                    <td>{stock.onHandQty ?? "-"}</td>
                    <td>{stock.updatedAt ?? "-"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className={styles.card}>
        <h3 className={styles.subTitle}>Movements</h3>
        {movements.length === 0 ? (
          <p className={styles.state}>No movements found</p>
        ) : (
          <div className={styles.tableWrap}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>Operation</th>
                  <th>Type</th>
                  <th>Product ID</th>
                  <th>Qty</th>
                  <th>From</th>
                  <th>To</th>
                  <th>Created</th>
                </tr>
              </thead>
              <tbody>
                {movements.map((movement) => (
                  <tr key={movement.id ?? movement.operationId ?? `${movement.productId}-${movement.createdAt}`}>
                    <td>{movement.operationId ?? "-"}</td>
                    <td>{movement.movementType ?? "-"}</td>
                    <td>{movement.productId}</td>
                    <td>{movement.quantity}</td>
                    <td>{movement.fromWarehouseId ?? "-"}</td>
                    <td>{movement.toWarehouseId ?? "-"}</td>
                    <td>{movement.createdAt ?? "-"}</td>
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

          <div className={styles.formColumns}>
            <form
              className={styles.formGrid}
              onSubmit={receiveForm.handleSubmit(async (values) => {
                await runWrite(
                  async () =>
                    receiveInventory(WORKSPACE_COMPANY_ID, {
                      operationId: createOperationId(),
                      warehouseId: values.warehouseId,
                      productId: values.productId,
                      quantity: Number(values.quantity),
                      note: values.note?.trim() ? values.note : null,
                    }),
                  "Stock received.",
                );
              })}
            >
              <h4 className={styles.miniTitle}>Receive</h4>
              <input className={styles.input} placeholder="Warehouse ID" {...receiveForm.register("warehouseId")} />
              {receiveForm.formState.errors.warehouseId ? (
                <span className={styles.error}>{receiveForm.formState.errors.warehouseId.message}</span>
              ) : null}
              <input className={styles.input} placeholder="Product ID" {...receiveForm.register("productId")} />
              <input
                type="number"
                className={styles.input}
                placeholder="Quantity"
                {...receiveForm.register("quantity", { valueAsNumber: true })}
              />
              <input className={styles.input} placeholder="Note" {...receiveForm.register("note")} />
              <button type="submit" className={styles.primaryButton} disabled={saving}>
                Receive
              </button>
            </form>

            <form
              className={styles.formGrid}
              onSubmit={shipForm.handleSubmit(async (values) => {
                await runWrite(
                  async () =>
                    shipInventory(WORKSPACE_COMPANY_ID, {
                      operationId: createOperationId(),
                      warehouseId: values.warehouseId,
                      productId: values.productId,
                      quantity: Number(values.quantity),
                      note: values.note?.trim() ? values.note : null,
                    }),
                  "Stock shipped.",
                );
              })}
            >
              <h4 className={styles.miniTitle}>Ship</h4>
              <input className={styles.input} placeholder="Warehouse ID" {...shipForm.register("warehouseId")} />
              <input className={styles.input} placeholder="Product ID" {...shipForm.register("productId")} />
              <input
                type="number"
                className={styles.input}
                placeholder="Quantity"
                {...shipForm.register("quantity", { valueAsNumber: true })}
              />
              <input className={styles.input} placeholder="Note" {...shipForm.register("note")} />
              <button type="submit" className={styles.primaryButton} disabled={saving}>
                Ship
              </button>
            </form>

            <form
              className={styles.formGrid}
              onSubmit={adjustForm.handleSubmit(async (values) => {
                await runWrite(
                  async () =>
                    adjustInventory(WORKSPACE_COMPANY_ID, {
                      operationId: createOperationId(),
                      warehouseId: values.warehouseId,
                      productId: values.productId,
                      quantityDelta: Number(values.quantityDelta),
                      reason: values.reason?.trim() ? values.reason : null,
                    }),
                  "Stock adjusted.",
                );
              })}
            >
              <h4 className={styles.miniTitle}>Adjust</h4>
              <input className={styles.input} placeholder="Warehouse ID" {...adjustForm.register("warehouseId")} />
              <input className={styles.input} placeholder="Product ID" {...adjustForm.register("productId")} />
              <input
                type="number"
                className={styles.input}
                placeholder="Quantity delta"
                {...adjustForm.register("quantityDelta", { valueAsNumber: true })}
              />
              <input className={styles.input} placeholder="Reason" {...adjustForm.register("reason")} />
              <button type="submit" className={styles.primaryButton} disabled={saving}>
                Adjust
              </button>
            </form>

            <form
              className={styles.formGrid}
              onSubmit={transferForm.handleSubmit(async (values) => {
                await runWrite(
                  async () =>
                    transferInventory(WORKSPACE_COMPANY_ID, {
                      operationId: createOperationId(),
                      fromWarehouseId: values.fromWarehouseId,
                      toWarehouseId: values.toWarehouseId,
                      productId: values.productId,
                      quantity: Number(values.quantity),
                      note: values.note?.trim() ? values.note : null,
                    }),
                  "Stock transferred.",
                );
              })}
            >
              <h4 className={styles.miniTitle}>Transfer</h4>
              <input className={styles.input} placeholder="From warehouse ID" {...transferForm.register("fromWarehouseId")} />
              <input className={styles.input} placeholder="To warehouse ID" {...transferForm.register("toWarehouseId")} />
              <input className={styles.input} placeholder="Product ID" {...transferForm.register("productId")} />
              <input
                type="number"
                className={styles.input}
                placeholder="Quantity"
                {...transferForm.register("quantity", { valueAsNumber: true })}
              />
              <input className={styles.input} placeholder="Note" {...transferForm.register("note")} />
              <button type="submit" className={styles.primaryButton} disabled={saving}>
                Transfer
              </button>
            </form>

            <form
              className={styles.formGrid}
              onSubmit={reserveForm.handleSubmit(async (values) => {
                await runWrite(
                  async () =>
                    reserveInventory(WORKSPACE_COMPANY_ID, {
                      reservationCode: createOperationId(),
                      warehouseId: values.warehouseId,
                      productId: values.productId,
                      quantity: Number(values.quantity),
                      note: values.note?.trim() ? values.note : null,
                    }),
                  "Reservation created.",
                );
              })}
            >
              <h4 className={styles.miniTitle}>Reserve</h4>
              <input className={styles.input} placeholder="Warehouse ID" {...reserveForm.register("warehouseId")} />
              <input className={styles.input} placeholder="Product ID" {...reserveForm.register("productId")} />
              <input
                type="number"
                className={styles.input}
                placeholder="Quantity"
                {...reserveForm.register("quantity", { valueAsNumber: true })}
              />
              <input className={styles.input} placeholder="Note" {...reserveForm.register("note")} />
              <button type="submit" className={styles.primaryButton} disabled={saving}>
                Reserve
              </button>
            </form>

            <form
              className={styles.formGrid}
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

