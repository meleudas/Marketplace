"use client";

import { isAxiosError } from "axios";
import { useEffect, useMemo, useState } from "react";
import {
  getCatalogProductBySlug,
  searchCatalogProducts,
} from "@/features/storefront/api/catalog.api";
import type { CatalogProductDetailDto } from "@/features/storefront/model/catalog.types";
import { fetchShippingMethods, type ShippingMethodDto } from "@/features/checkout/api/checkout.api";
import { useCartStore } from "@/features/cart/model/cart.store";
import { StateBlock } from "@/features/storefront/ui/StateBlock";
import { apiClient } from "@/shared/api/http.client";
import {
  Button,
  ChevronDownIcon,
  OpenBookIcon,
  PageLayout,
  PhoneIcon,
  QuantityStepper,
  Spinner,
  SurfaceCard,
} from "@/shared/ui";
import iconStyles from "@/shared/ui/icons/Icon.module.css";
import {
  buildProductCharacteristics,
  formatCompactPrice,
  formatPriceWithUnit,
  resolveAvailabilityLabel,
  resolveProductFormat,
  stripFormatSuffix,
} from "../lib/product-details.lib";
import { ProductGallery } from "../ui/ProductGallery";
import { ReviewsSection } from "../ui/ReviewsSection";
import { SimilarProductsSection } from "../ui/SimilarProductsSection";
import styles from "./ProductDetailsScreen.module.css";

interface ProductDetailsScreenProps {
  slug: string;
}

interface FormatPriceOption {
  key: string;
  label: string;
  price: number;
  isElectronic: boolean;
}

const VISIBLE_CHARACTERISTICS_COUNT = 4;

const MOCK_NOVA_POSHTA_PRICE_LABEL = "70-135 грн.";

export function ProductDetailsScreen({ slug }: ProductDetailsScreenProps) {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [productDetails, setProductDetails] = useState<CatalogProductDetailDto | null>(null);
  const [siblingFormat, setSiblingFormat] = useState<FormatPriceOption | null>(null);

  const [shippingMethods, setShippingMethods] = useState<ShippingMethodDto[] | null>(null);
  const [novaPoshtaOpen, setNovaPoshtaOpen] = useState(false);
  const [ukrposhtaOpen, setUkrposhtaOpen] = useState(true);

  const [quantity, setQuantity] = useState(1);
  const [addingToCart, setAddingToCart] = useState(false);
  const [cartMessage, setCartMessage] = useState<{ text: string; isError: boolean } | null>(null);

  const [descriptionExpanded, setDescriptionExpanded] = useState(false);
  const [allCharacteristicsVisible, setAllCharacteristicsVisible] = useState(false);

  const { loadCart } = useCartStore();

  useEffect(() => {
    let isCancelled = false;

    const load = async () => {
      try {
        setLoading(true);
        setError(null);
        setSiblingFormat(null);

        const details = await getCatalogProductBySlug(slug);
        if (isCancelled) return;
        setProductDetails(details);

        const stem = stripFormatSuffix(details.product.name);
        if (stem) {
          try {
            const siblingResult = await searchCatalogProducts({
              name: stem,
              companyId: details.product.companyId,
              pageSize: 5,
            });
            const sibling = siblingResult.items.find(
              (item) => item.slug !== details.product.slug && item.name.startsWith(stem),
            );

            if (sibling && !isCancelled) {
              const isElectronic = sibling.name.toLowerCase().includes("електронна");
              setSiblingFormat({
                key: sibling.slug,
                label: isElectronic ? "Електронна" : "Паперова",
                price: sibling.price,
                isElectronic,
              });
            }
          } catch {
            // Sibling format is a nice-to-have; ignore failures silently.
          }
        }
      } catch {
        if (!isCancelled) {
          setError("Не вдалося завантажити дані про товар");
        }
      } finally {
        if (!isCancelled) {
          setLoading(false);
        }
      }
    };

    void load();

    return () => {
      isCancelled = true;
    };
  }, [slug]);

  useEffect(() => {
    let isCancelled = false;

    const loadShipping = async () => {
      try {
        const methods = await fetchShippingMethods();
        if (!isCancelled) {
          setShippingMethods(methods);
        }
      } catch {
        if (!isCancelled) {
          setShippingMethods([]);
        }
      }
    };

    void loadShipping();

    return () => {
      isCancelled = true;
    };
  }, []);

  const characteristics = useMemo(
    () => buildProductCharacteristics(productDetails?.detail ?? null),
    [productDetails],
  );

  const productFormat = useMemo(
    () =>
      productDetails
        ? resolveProductFormat(productDetails.detail, productDetails.product.name)
        : null,
    [productDetails],
  );

  const visibleCharacteristics = allCharacteristicsVisible
    ? characteristics
    : characteristics.slice(0, VISIBLE_CHARACTERISTICS_COUNT);

  const hasHiddenCharacteristics = characteristics.length > VISIBLE_CHARACTERISTICS_COUNT;

  const novaPoshtaMethod = useMemo(
    () => shippingMethods?.find((method) => method.name.toLowerCase().includes("нова")) ?? null,
    [shippingMethods],
  );

  const ukrposhtaMethod = useMemo(
    () => shippingMethods?.find((method) => method.name.toLowerCase().includes("укрпошта")) ?? null,
    [shippingMethods],
  );

  const novaPoshtaPriceLabel = novaPoshtaMethod
    ? formatPriceWithUnit(novaPoshtaMethod.price)
    : MOCK_NOVA_POSHTA_PRICE_LABEL;

  const ukrposhtaPriceLabel = ukrposhtaMethod
    ? formatPriceWithUnit(ukrposhtaMethod.price)
    : "40 грн.";

  const handleAddToCart = async () => {
    if (!productDetails) return;

    setAddingToCart(true);
    setCartMessage(null);

    try {
      await apiClient.post("/me/cart/items", {
        productId: productDetails.product.id,
        quantity,
      });
      setCartMessage({ text: "Книгу додано в кошик", isError: false });
      void loadCart();
    } catch (err: unknown) {
      if (isAxiosError(err) && err.response?.status === 401) {
        setCartMessage({ text: "Увійдіть, щоб додати книгу в кошик", isError: true });
      } else {
        setCartMessage({ text: "Не вдалося додати книгу в кошик", isError: true });
      }
    } finally {
      setAddingToCart(false);
    }
  };

  return (
    <PageLayout
      headerProps={{
        homeHref: "/",
        userHref: "/me",
        searchPlaceholder: "Пошук книг",
      }}
      footerProps={{ homeHref: "/" }}
    >
      <div className={styles.main}>
        {loading ? (
          <div className={styles.loading}>
            <Spinner size="lg" />
          </div>
        ) : null}

        {!loading && error ? <StateBlock message={error} isError /> : null}

        {!loading && !error && !productDetails ? (
          <StateBlock message="Товар не знайдено" />
        ) : null}

        {!loading && !error && productDetails ? (
          <>
            <ProductGallery
              images={productDetails.images.map((image) => image.imageUrl)}
              alt={productDetails.product.name}
            />

            <Button variant="secondary" fullWidth leadingIcon={<OpenBookIcon />} className={styles.previewButton}>
              Читати уривок
            </Button>

            <div className={styles.headline}>
              <h1 className={styles.title}>{stripFormatSuffix(productDetails.product.name)}</h1>
              <p className={styles.price}>{formatPriceWithUnit(productDetails.product.price)}</p>
              <p className={styles.meta}>
                {resolveAvailabilityLabel(productDetails.product).label}
                {" · "}
                {productFormat?.label ?? "Паперова"} книга
              </p>
            </div>

            <div className={styles.actionsGrid}>
              <Button
                variant="primary"
                fullWidth
                className={styles.gridCell}
                disabled={addingToCart || !resolveAvailabilityLabel(productDetails.product).inStock}
                onClick={handleAddToCart}
              >
                {addingToCart ? "Додавання..." : "До кошика"}
              </Button>

              <QuantityStepper
                value={quantity}
                onChange={setQuantity}
                className={styles.gridQuantityStepper}
              />

              <Button
                variant="filter"
                fullWidth
                selectable
                selected
                leadingIcon={<PhoneIcon />}
                className={styles.gridCell}
                disabled={!productFormat?.isElectronic && !siblingFormat?.isElectronic}
              >
                {productFormat?.isElectronic
                  ? formatCompactPrice(productDetails.product.price)
                  : siblingFormat?.isElectronic
                    ? formatCompactPrice(siblingFormat.price)
                    : "—"}
              </Button>

              <Button
                variant="filter"
                fullWidth
                selectable
                selected={false}
                leadingIcon={<OpenBookIcon />}
                className={styles.gridCell}
                disabled={productFormat?.isElectronic && !siblingFormat}
              >
                {!productFormat?.isElectronic
                  ? formatCompactPrice(productDetails.product.price)
                  : siblingFormat
                    ? formatCompactPrice(siblingFormat.price)
                    : "—"}
              </Button>
            </div>

            {cartMessage ? (
              <p className={cartMessage.isError ? styles.cartMessageError : styles.cartMessageSuccess}>
                {cartMessage.text}
              </p>
            ) : null}

            <section className={styles.section}>
              <h2 className={styles.sectionTitle}>Доставка</h2>

              <div className={styles.deliveryList}>
                <div className={styles.deliveryItem}>
                  <button
                    type="button"
                    className={styles.deliveryHeader}
                    onClick={() => setNovaPoshtaOpen((open) => !open)}
                    aria-expanded={novaPoshtaOpen}
                  >
                    <span className={styles.deliveryName}>Нова пошта</span>
                    <span className={styles.deliveryHeaderRight}>
                      {!novaPoshtaOpen ? (
                        <span className={styles.deliveryPrice}>{novaPoshtaPriceLabel}</span>
                      ) : null}
                      <ChevronDownIcon
                        className={[
                          iconStyles.icon,
                          styles.deliveryChevron,
                          novaPoshtaOpen ? styles.deliveryChevronOpen : "",
                        ]
                          .filter(Boolean)
                          .join(" ")}
                      />
                    </span>
                  </button>

                  {novaPoshtaOpen ? (
                    <ul className={styles.deliveryDetails}>
                      {novaPoshtaMethod ? (
                        <>
                          <li>
                            Термін доставки — {novaPoshtaMethod.estimatedDaysMin}-
                            {novaPoshtaMethod.estimatedDaysMax} дні.
                          </li>
                          {typeof novaPoshtaMethod.freeShippingThreshold === "number" ? (
                            <li>
                              Безкоштовно від {formatPriceWithUnit(novaPoshtaMethod.freeShippingThreshold)}
                            </li>
                          ) : null}
                        </>
                      ) : (
                        <li>Вартість залежить від відділення отримання.</li>
                      )}
                    </ul>
                  ) : null}
                </div>

                <div className={styles.deliveryItem}>
                  <button
                    type="button"
                    className={styles.deliveryHeader}
                    onClick={() => setUkrposhtaOpen((open) => !open)}
                    aria-expanded={ukrposhtaOpen}
                  >
                    <span className={styles.deliveryName}>Укрпошта</span>
                    <span className={styles.deliveryHeaderRight}>
                      {!ukrposhtaOpen ? (
                        <span className={styles.deliveryPrice}>{ukrposhtaPriceLabel}</span>
                      ) : null}
                      <ChevronDownIcon
                        className={[
                          iconStyles.icon,
                          styles.deliveryChevron,
                          ukrposhtaOpen ? styles.deliveryChevronOpen : "",
                        ]
                          .filter(Boolean)
                          .join(" ")}
                      />
                    </span>
                  </button>

                  {ukrposhtaOpen ? (
                    <ul className={styles.deliveryDetails}>
                      {ukrposhtaMethod && typeof ukrposhtaMethod.freeShippingThreshold === "number" ? (
                        <>
                          <li>
                            Замовлення до {formatPriceWithUnit(ukrposhtaMethod.freeShippingThreshold)} —{" "}
                            {formatPriceWithUnit(ukrposhtaMethod.price)}
                          </li>
                          <li>
                            Замовлення від {formatPriceWithUnit(ukrposhtaMethod.freeShippingThreshold)} —
                            безкоштовно.
                          </li>
                        </>
                      ) : (
                        <>
                          <li>Замовлення до 499 грн. — 40 грн.</li>
                          <li>Замовлення від 499 грн. — безкоштовно.</li>
                        </>
                      )}
                    </ul>
                  ) : null}
                </div>
              </div>
            </section>

            {characteristics.length > 0 ? (
              <div className={styles.surfaceCardWrap}>
                <SurfaceCard>
                  <h2 className={styles.cardSectionTitle}>Характеристики</h2>

                  <dl className={styles.characteristicsList}>
                    {visibleCharacteristics.map((item) => (
                      <div className={styles.characteristicsRow} key={item.label}>
                        <dt className={styles.characteristicsLabel}>{item.label}</dt>
                        <span className={styles.characteristicsDash} aria-hidden="true">
                          —
                        </span>
                        <dd className={styles.characteristicsValue}>{item.value}</dd>
                      </div>
                    ))}
                  </dl>

                  {hasHiddenCharacteristics ? (
                    <button
                      type="button"
                      className={styles.expandLink}
                      onClick={() => setAllCharacteristicsVisible((visible) => !visible)}
                    >
                      {allCharacteristicsVisible ? "Згорнути" : "Усі параметри"}
                    </button>
                  ) : null}
                </SurfaceCard>
              </div>
            ) : null}

            {productDetails.product.description ? (
              <div className={styles.surfaceCardWrap}>
                <SurfaceCard>
                  <h2 className={styles.cardSectionTitle}>Опис</h2>
                  <p
                    className={
                      descriptionExpanded ? styles.description : `${styles.description} ${styles.descriptionClamped}`
                    }
                  >
                    {productDetails.product.description}
                  </p>

                  {descriptionExpanded ? (
                    <button
                      type="button"
                      className={styles.expandLink}
                      onClick={() => setDescriptionExpanded(false)}
                    >
                      Згорнути
                    </button>
                  ) : (
                    <button
                      type="button"
                      className={styles.expandLink}
                      onClick={() => setDescriptionExpanded(true)}
                    >
                      Розгорнути
                    </button>
                  )}
                </SurfaceCard>
              </div>
            ) : null}

            <ReviewsSection productId={productDetails.product.id} />

            <SimilarProductsSection slug={productDetails.product.slug} />
          </>
        ) : null}
      </div>
    </PageLayout>
  );
}
