"use client";

import { isAxiosError } from "axios";
import Link from "next/link";
import { useEffect, useMemo, useRef, useState } from "react";
import {
  getCatalogCategories,
  getCatalogProductBySlug,
  searchCatalogProducts,
} from "@/features/storefront/api/catalog.api";
import type {
  CatalogCategoryDto,
  CatalogProductDetailDto,
} from "@/features/storefront/model/catalog.types";
import { fetchShippingMethods, type ShippingMethodDto } from "@/features/checkout/api/checkout.api";
import { useCartStore } from "@/features/cart/model/cart.store";
import { apiClient } from "@/shared/api/http.client";
import {
  Button,
  ChevronDownIcon,
  FooterCatIllustration,
  InitialsAvatar,
  PageLayout,
  QuantityStepper,
  SideDecorShell,
  Spinner,
  SurfaceCard,
} from "@/shared/ui";
import iconStyles from "@/shared/ui/icons/Icon.module.css";
import {
  buildProductCharacteristics,
  formatPriceWithUnit,
  getProductAuthor,
  resolveAvailabilityLabel,
  resolveProductFormat,
  stripFormatSuffix,
} from "../lib/product-details.lib";
import {
  buildNovaPoshtaDeliveryDetails,
  buildUkrposhtaDeliveryDetails,
} from "../lib/product-delivery.lib";
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

function buildCategoryBreadcrumbChain(
  categories: CatalogCategoryDto[],
  categoryId: number,
): CatalogCategoryDto[] {
  const chain: CatalogCategoryDto[] = [];
  let current = categories.find((category) => category.id === categoryId) ?? null;

  while (current) {
    chain.unshift(current);
    current =
      current.parentId === null
        ? null
        : (categories.find((category) => category.id === current?.parentId) ?? null);
  }

  return chain;
}

function splitAuthorName(author: string): { firstName: string; lastName: string } {
  const parts = author.trim().split(/\s+/u);
  if (parts.length === 1) {
    return { firstName: parts[0], lastName: "" };
  }

  return {
    firstName: parts[0],
    lastName: parts.slice(1).join(" "),
  };
}

export function ProductDetailsScreen({ slug }: ProductDetailsScreenProps) {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [productDetails, setProductDetails] = useState<CatalogProductDetailDto | null>(null);
  const [categories, setCategories] = useState<CatalogCategoryDto[]>([]);
  const [siblingFormat, setSiblingFormat] = useState<FormatPriceOption | null>(null);

  const [shippingMethods, setShippingMethods] = useState<ShippingMethodDto[] | null>(null);
  const [novaPoshtaOpen, setNovaPoshtaOpen] = useState(false);
  const [ukrposhtaOpen, setUkrposhtaOpen] = useState(true);

  const [quantity, setQuantity] = useState(1);
  const [addingToCart, setAddingToCart] = useState(false);
  const [cartMessage, setCartMessage] = useState<{ text: string; isError: boolean } | null>(null);

  const [descriptionExpanded, setDescriptionExpanded] = useState(false);
  const [allCharacteristicsVisible, setAllCharacteristicsVisible] = useState(false);

  const reviewsSectionRef = useRef<HTMLElement>(null);

  const { loadCart } = useCartStore();

  useEffect(() => {
    let isCancelled = false;

    const load = async () => {
      try {
        setLoading(true);
        setError(null);
        setSiblingFormat(null);

        const [details, categoriesData] = await Promise.all([
          getCatalogProductBySlug(slug),
          getCatalogCategories(),
        ]);
        if (isCancelled) return;
        setProductDetails(details);
        setCategories(categoriesData);

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

  const novaPoshtaDetails = useMemo(
    () => buildNovaPoshtaDeliveryDetails(novaPoshtaMethod),
    [novaPoshtaMethod],
  );

  const ukrposhtaDetails = useMemo(
    () => buildUkrposhtaDeliveryDetails(ukrposhtaMethod),
    [ukrposhtaMethod],
  );

  const productAuthor = useMemo(
    () => (productDetails ? getProductAuthor(productDetails.detail) : null),
    [productDetails],
  );

  const authorNameParts = useMemo(
    () => (productAuthor ? splitAuthorName(productAuthor) : null),
    [productAuthor],
  );

  const categoryBreadcrumbs = useMemo(() => {
    if (!productDetails || categories.length === 0) {
      return [];
    }

    return buildCategoryBreadcrumbChain(categories, productDetails.product.categoryId);
  }, [categories, productDetails]);

  const availability = productDetails
    ? resolveAvailabilityLabel(productDetails.product)
    : { inStock: false, label: "" };

  const handleScrollToReviews = () => {
    reviewsSectionRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
  };

  const renderCharacteristicsList = (items: typeof visibleCharacteristics) => (
    <dl className={styles.characteristicsList}>
      {items.map((item) => (
        <div className={styles.characteristicsRow} key={item.label}>
          <dt className={styles.characteristicsLabel}>{item.label}</dt>
          <span className={styles.characteristicsDash} aria-hidden="true">
            —
          </span>
          <dd className={styles.characteristicsValue}>{item.value}</dd>
        </div>
      ))}
    </dl>
  );

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
      className={styles.pageMain}
      headerProps={{
        homeHref: "/",
        userHref: "/me",
        searchPlaceholder: "Пошук книг",
      }}
      footerProps={{ homeHref: "/" }}
    >
      <SideDecorShell contentClassName={styles.main}>
        {loading ? (
          <div className={styles.loading}>
            <Spinner size="lg" />
          </div>
        ) : null}

        {!loading && error ? (
          <div className={styles.errorState}>
            <FooterCatIllustration className={styles.errorCat} />
            <div className={styles.errorBody}>
              <p className={styles.errorTitle}>Не вдалося завантажити дані про товар</p>
              <p className={styles.errorText}>
                Спробуйте оновити сторінку або поверніться до каталогу.
              </p>
              <Link href="/catalog" className={styles.errorLink}>
                До каталогу
              </Link>
            </div>
          </div>
        ) : null}

        {!loading && !error && !productDetails ? (
          <div className={styles.errorState}>
            <FooterCatIllustration className={styles.errorCat} />
            <div className={styles.errorBody}>
              <p className={styles.errorTitle}>Товар не знайдено</p>
              <p className={styles.errorText}>
                Можливо, цю книгу було видалено або вона тимчасово недоступна.
              </p>
              <Link href="/catalog" className={styles.errorLink}>
                До каталогу
              </Link>
            </div>
          </div>
        ) : null}

        {!loading && !error && productDetails ? (
          <>
            <nav className={styles.breadcrumbs} aria-label="Навігація">
              <Link href="/" className={styles.breadcrumbLink}>
                Головна
              </Link>
              {categoryBreadcrumbs.map((category) => (
                <span key={category.id} className={styles.breadcrumbSegment}>
                  <span className={styles.breadcrumbSeparator} aria-hidden="true">
                    ›
                  </span>
                  <Link href={`/catalog/${category.slug}`} className={styles.breadcrumbLink}>
                    {category.name}
                  </Link>
                </span>
              ))}
              <span className={styles.breadcrumbSegment}>
                <span className={styles.breadcrumbSeparator} aria-hidden="true">
                  ›
                </span>
                <span className={styles.breadcrumbCurrent}>
                  {stripFormatSuffix(productDetails.product.name)}
                </span>
              </span>
            </nav>

            <div className={styles.hero}>
              <div className={styles.heroGalleryCol}>
                <ProductGallery
                  images={productDetails.images.map((image) => image.imageUrl)}
                  alt={productDetails.product.name}
                />

              </div>

              <div className={styles.heroDetailsCol}>
                <div className={styles.headline}>
                  <h1 className={styles.title}>{stripFormatSuffix(productDetails.product.name)}</h1>
                  {productAuthor ? <p className={styles.author}>{productAuthor}</p> : null}
                </div>

                <div className={styles.statusRow}>
                  <span
                    className={
                      availability.inStock ? styles.availabilityBadge : styles.availabilityBadgeOut
                    }
                  >
                    {availability.label}
                  </span>
                  <button type="button" className={styles.reviewLink} onClick={handleScrollToReviews}>
                    Написати відгук
                  </button>
                </div>

                <p className={styles.price}>{formatPriceWithUnit(productDetails.product.price)}</p>

                <p className={styles.meta}>
                  {availability.label}
                  {" · "}
                  {productFormat?.label ?? "Паперова"} книга
                </p>

                <div className={styles.actionsGrid}>
                  <Button
                    variant="primary"
                    fullWidth
                    className={styles.gridCell}
                    disabled={addingToCart || !availability.inStock}
                    onClick={handleAddToCart}
                  >
                    {addingToCart ? "Додавання..." : "Додати у кошик"}
                  </Button>

                  <QuantityStepper
                    value={quantity}
                    onChange={setQuantity}
                    className={styles.gridQuantityStepper}
                  />


                </div>

                {cartMessage ? (
                  <p
                    className={
                      cartMessage.isError ? styles.cartMessageError : styles.cartMessageSuccess
                    }
                  >
                    {cartMessage.text}
                  </p>
                ) : null}
                {characteristics.length > 0 ? (
                  <section className={styles.desktopCharacteristics}>
                    <h2 className={styles.desktopCharacteristicsTitle}>Характеристики</h2>
                    {renderCharacteristicsList(characteristics)}
                  </section>
                ) : null}
              </div>
            </div>

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
                    <div className={styles.deliveryBody}>
                      <p className={styles.deliveryIntro}>{novaPoshtaDetails.intro}</p>
                      <ul className={styles.deliveryDetails}>
                        {novaPoshtaDetails.points.map((point) => (
                          <li key={point.label}>
                            <span className={styles.deliveryPointLabel}>{point.label}</span>
                            <span className={styles.deliveryPointText}>{point.text}</span>
                          </li>
                        ))}
                      </ul>
                    </div>
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
                    <div className={styles.deliveryBody}>
                      <p className={styles.deliveryIntro}>{ukrposhtaDetails.intro}</p>
                      <ul className={styles.deliveryDetails}>
                        {ukrposhtaDetails.points.map((point) => (
                          <li key={point.label}>
                            <span className={styles.deliveryPointLabel}>{point.label}</span>
                            <span className={styles.deliveryPointText}>{point.text}</span>
                          </li>
                        ))}
                      </ul>
                    </div>
                  ) : null}
                </div>
              </div>
            </section>

            {characteristics.length > 0 ? (
              <div className={styles.surfaceCardWrap}>
                <SurfaceCard>
                  <h2 className={styles.cardSectionTitle}>Характеристики</h2>

                  {renderCharacteristicsList(visibleCharacteristics)}

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
              <div className={styles.descriptionWrap}>
                <SurfaceCard>
                  <h2 className={styles.cardSectionTitle}>Опис книги</h2>
                  <p
                    className={
                      descriptionExpanded
                        ? styles.description
                        : `${styles.description} ${styles.descriptionClamped}`
                    }
                  >
                    {productDetails.product.description}
                  </p>

                  {productDetails.product.description.length > 500 ? (
                    descriptionExpanded ? (
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
                    )
                  ) : null}
                </SurfaceCard>
              </div>
            ) : null}

            {productAuthor && authorNameParts ? (
              <section className={styles.authorSection}>
                <InitialsAvatar
                  firstName={authorNameParts.firstName}
                  lastName={authorNameParts.lastName}
                />
                <div className={styles.authorSectionBody}>
                  <p className={styles.authorSectionLabel}>Автор</p>
                  <p className={styles.authorSectionName}>{productAuthor}</p>
                </div>
                <Link
                  href={`/catalog?authors=${encodeURIComponent(productAuthor)}`}
                  className={styles.authorSectionLink}
                >
                  Інші книги цього автора ›
                </Link>
              </section>
            ) : null}

            <ReviewsSection
              ref={reviewsSectionRef}
              productId={productDetails.product.id}
            />

            <SimilarProductsSection slug={productDetails.product.slug} />
          </>
        ) : null}
      </SideDecorShell>
    </PageLayout>
  );
}
