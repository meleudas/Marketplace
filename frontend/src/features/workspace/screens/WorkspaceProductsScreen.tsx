"use client";

import { useEffect, useMemo, useState } from "react";
import { useAuth } from "@/features/auth/model/auth.store";
import {
  createWorkspaceProduct,
  deleteWorkspaceProduct,
  getWorkspaceCategories,
  getWorkspaceProducts,
  updateWorkspaceProduct,
} from "@/features/workspace/api/products.api";
import { getMyCompanyMembership } from "@/features/workspace/api/workspace.api";
import { WORKSPACE_COMPANY_ID } from "@/features/workspace/config/workspace.constants";
import { getWorkspaceErrorMessage } from "@/features/workspace/lib/workspace.error";
import { canWriteProducts } from "@/features/workspace/model/workspace.permissions";
import type {
  CompanyMembershipDto,
  CompanyProductDto,
  UpsertProductRequest,
  WorkspaceCategoryDto,
} from "@/features/workspace/model/workspace.types";
import { WorkspaceMembershipError } from "@/features/workspace/model/workspace.types";
import { ProductForm } from "@/features/workspace/ui/ProductForm";
import styles from "./WorkspaceScreen.module.css";

export function WorkspaceProductsScreen() {
  const user = useAuth((state) => state.user);
  const isGlobalAdmin = user?.role === "admin";

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [membership, setMembership] = useState<CompanyMembershipDto | null>(null);
  const [products, setProducts] = useState<CompanyProductDto[]>([]);
  const [categories, setCategories] = useState<WorkspaceCategoryDto[]>([]);
  const [editingProduct, setEditingProduct] = useState<CompanyProductDto | null>(null);

  const canWrite = useMemo(() => isGlobalAdmin || canWriteProducts(membership), [isGlobalAdmin, membership]);

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

      const [productsData, categoriesData] = await Promise.all([
        getWorkspaceProducts(WORKSPACE_COMPANY_ID),
        getWorkspaceCategories(),
      ]);

      setMembership(membershipData);
      setProducts(productsData);
      setCategories(categoriesData);
    } catch (loadError) {
      if (
        !isGlobalAdmin &&
        loadError instanceof WorkspaceMembershipError &&
        ["forbidden", "notFound"].includes(loadError.kind)
      ) {
        setError("You do not have access to this company workspace");
      } else {
        setError(getWorkspaceErrorMessage(loadError, "Failed to load products"));
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, [isGlobalAdmin]);

  const onCreate = async (payload: UpsertProductRequest) => {
    try {
      setSaving(true);
      setFeedback(null);
      await createWorkspaceProduct(WORKSPACE_COMPANY_ID, payload);
      setFeedback("Product created.");
      await load();
    } catch (submitError) {
      setFeedback(getWorkspaceErrorMessage(submitError, "Failed to create product"));
    } finally {
      setSaving(false);
    }
  };

  const onEdit = async (payload: UpsertProductRequest) => {
    if (!editingProduct) {
      return;
    }

    try {
      setSaving(true);
      setFeedback(null);
      await updateWorkspaceProduct(WORKSPACE_COMPANY_ID, editingProduct.id, payload);
      setEditingProduct(null);
      setFeedback("Product updated.");
      await load();
    } catch (submitError) {
      setFeedback(getWorkspaceErrorMessage(submitError, "Failed to update product"));
    } finally {
      setSaving(false);
    }
  };

  const onDelete = async (product: CompanyProductDto) => {
    const shouldDelete = window.confirm(`Delete product "${product.name}"?`);
    if (!shouldDelete) {
      return;
    }

    try {
      setSaving(true);
      setFeedback(null);
      await deleteWorkspaceProduct(WORKSPACE_COMPANY_ID, product.id);
      setFeedback("Product deleted.");
      await load();
    } catch (deleteError) {
      setFeedback(getWorkspaceErrorMessage(deleteError, "Failed to delete product"));
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return <p className={styles.state}>Loading products...</p>;
  }

  if (error) {
    return <p className={styles.state}>{error}</p>;
  }

  return (
    <div className={styles.stack}>
      <section className={styles.card}>
        <h2 className={styles.sectionTitle}>Products</h2>
        <p className={styles.row}>Company ID: {WORKSPACE_COMPANY_ID}</p>
        <p className={styles.row}>My role: {membership?.role ?? (isGlobalAdmin ? "admin" : "unknown")}</p>
        {!canWrite ? <p className={styles.hint}>Read-only mode for your role.</p> : null}
        {feedback ? <p className={styles.feedback}>{feedback}</p> : null}
      </section>

      {canWrite ? (
        <section className={styles.card}>
          <h3 className={styles.subTitle}>Create product</h3>
          <ProductForm
            categories={categories}
            submitLabel="Create product"
            busy={saving}
            onSubmit={onCreate}
          />
        </section>
      ) : null}

      {editingProduct && canWrite ? (
        <section className={styles.card}>
          <div className={styles.rowBetween}>
            <h3 className={styles.subTitle}>Edit: {editingProduct.name}</h3>
            <button type="button" className={styles.ghostButton} onClick={() => setEditingProduct(null)}>
              Cancel
            </button>
          </div>

          <ProductForm
            categories={categories}
            initialProduct={editingProduct}
            submitLabel="Save changes"
            busy={saving}
            onSubmit={onEdit}
          />
        </section>
      ) : null}

      <section className={styles.card}>
        <h3 className={styles.subTitle}>Company products</h3>
        {products.length === 0 ? (
          <p className={styles.state}>No products found</p>
        ) : (
          <div className={styles.tableWrap}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Slug</th>
                  <th>Price</th>
                  <th>Old price</th>
                  <th>Min stock</th>
                  <th>Available</th>
                  <th>Status</th>
                  <th>Category</th>
                  {canWrite ? <th>Actions</th> : null}
                </tr>
              </thead>
              <tbody>
                {products.map((product) => (
                  <tr key={product.id}>
                    <td>{product.name}</td>
                    <td>{product.slug}</td>
                    <td>{product.price}</td>
                    <td>{product.oldPrice ?? "-"}</td>
                    <td>{product.minStock}</td>
                    <td>{product.availableQty ?? "-"}</td>
                    <td>{product.availabilityStatus ?? "-"}</td>
                    <td>{product.categoryName ?? product.categoryId ?? "-"}</td>
                    {canWrite ? (
                      <td className={styles.actionsInline}>
                        <button
                          type="button"
                          className={styles.ghostButton}
                          onClick={() => setEditingProduct(product)}
                          disabled={saving}
                        >
                          Edit
                        </button>
                        <button
                          type="button"
                          className={styles.dangerButton}
                          onClick={() => {
                            void onDelete(product);
                          }}
                          disabled={saving}
                        >
                          Delete
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
    </div>
  );
}

