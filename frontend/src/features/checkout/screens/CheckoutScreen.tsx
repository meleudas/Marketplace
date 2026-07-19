"use client";

import { useEffect, useState, useMemo, useRef } from "react";
import { useRouter } from "next/navigation";
import Image from "next/image";
import Link from "next/link";
import {
  PhoneInput,
  defaultCountries,
  parseCountry,
} from "react-international-phone";
import "react-international-phone/style.css";
import { useAuth } from "@/features/auth/model/auth.store";
import { getCatalogProducts } from "@/features/storefront/api/catalog.api";
import {
  fetchMyCart,
  fetchShippingMethods,
  submitCheckout,
  fetchNovaPoshtaCities,
  fetchNovaPoshtaWarehouses,
  updateCartItemQuantity,
  removeCartItem,
  type CartDto,
  type ShippingMethodDto,
  type CheckoutCartRequest,
  type NovaPoshtaCity,
  type NovaPoshtaWarehouse,
} from "../api/checkout.api";
import type { CatalogProductListItemDto } from "@/features/storefront/model/catalog.types";
import { PageLayout, Spinner, Button } from "@/shared/ui";
import styles from "./CheckoutScreen.module.css";

function generateUUID(): string {
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === "x" ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

const excludedCountryCodes = new Set([
  "ru",
  "by",
  "kz",
  "uz",
  "tm",
  "tj",
  "kg",
  "am",
  "az",
]);
const filteredCountries = defaultCountries.filter((c) => {
  const { iso2 } = parseCountry(c);
  return !excludedCountryCodes.has(iso2);
});

export function CheckoutScreen() {
  const router = useRouter();
  const { user, isAuthenticated, initialized, loadMe } = useAuth();

  useEffect(() => {
    loadMe();
  }, [loadMe]);

  const [cart, setCart] = useState<CartDto | null>(null);
  const [catalogProducts, setCatalogProducts] = useState<
    CatalogProductListItemDto[]
  >([]);
  const [shippingMethods, setShippingMethods] = useState<ShippingMethodDto[]>(
    [],
  );
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [successOrder, setSuccessOrder] = useState<{
    orderNumber: string;
    total: number;
  } | null>(null);
  const [errorMsg, setErrorMsg] = useState("");

  // Form states
  const [contacts, setContacts] = useState({
    firstName: "",
    lastName: "",
    middleName: "",
    phone: "",
    email: "",
  });

  const [editingItems, setEditingItems] = useState(false);

  const [deliveryTab, setDeliveryTab] = useState<"domestic" | "international">(
    "domestic",
  );
  const [carrier, setCarrier] = useState<"NovaPoshta" | "UkrPoshta">(
    "NovaPoshta",
  );

  // Nova Poshta
  const [domesticCity, setDomesticCity] = useState("");
  const [selectedCityRef, setSelectedCityRef] = useState("");
  const [citySuggestions, setCitySuggestions] = useState<NovaPoshtaCity[]>([]);
  const [showCityDropdown, setShowCityDropdown] = useState(false);
  const [warehouses, setWarehouses] = useState<NovaPoshtaWarehouse[]>([]);
  const [loadingWarehouses, setLoadingWarehouses] = useState(false);
  const [domesticBranch, setDomesticBranch] = useState("");
  const [showBranchDropdown, setShowBranchDropdown] = useState(false);
  const [branchFilter, setBranchFilter] = useState("");

  // Ukrposhta
  const [ukrposhtaCity, setUkrposhtaCity] = useState("");
  const [ukrposhtaBranch, setUkrposhtaBranch] = useState("");
  const [ukrposhtaIndex, setUkrposhtaIndex] = useState("");

  // International
  const [addressDetails, setAddressDetails] = useState({
    street: "",
    city: "",
    state: "",
    postalCode: "",
    country: "Україна",
  });

  const [wrapAsGift, setWrapAsGift] = useState(false);
  const [paymentMethod, setPaymentMethod] = useState<"Card" | "Cash">("Card");
  const [notes] = useState("");

  const searchTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  const cityDropdownRef = useRef<HTMLDivElement>(null);
  const branchDropdownRef = useRef<HTMLDivElement>(null);

  // Close dropdowns on outside click
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (
        cityDropdownRef.current &&
        !cityDropdownRef.current.contains(e.target as Node)
      ) {
        setShowCityDropdown(false);
      }
      if (
        branchDropdownRef.current &&
        !branchDropdownRef.current.contains(e.target as Node)
      ) {
        setShowBranchDropdown(false);
      }
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  useEffect(() => {
    if (initialized && !isAuthenticated) {
      router.push("/auth/login?redirect=/checkout");
      return;
    }
    if (isAuthenticated) {
      const loadData = async () => {
        try {
          const [cartData, productsData, methodsData] = await Promise.all([
            fetchMyCart(),
            getCatalogProducts(),
            fetchShippingMethods().catch(() => []),
          ]);
          setCart(cartData);
          setCatalogProducts(productsData);
          setShippingMethods(methodsData);
          if (user) {
            setContacts({
              firstName: user.firstName || "",
              lastName: user.lastName || "",
              middleName: user.patronymic || "",
              phone: user.phoneNumber || "",
              email: user.email || "",
            });
          }
        } catch (e) {
          console.error("Failed to load checkout data", e);
          setErrorMsg("Помилка завантаження даних замовлення.");
        } finally {
          setLoading(false);
        }
      };
      void loadData();
    }
  }, [isAuthenticated, initialized, user, router]);

  const handleUpdateQty = async (
    itemId: number,
    currentQty: number,
    delta: number,
  ) => {
    const newQty = currentQty + delta;
    if (newQty < 1) return;
    try {
      const updatedCart = await updateCartItemQuantity(itemId, newQty);
      setCart(updatedCart);
    } catch (err) {
      console.error("Failed to update item quantity", err);
    }
  };

  const handleDeleteItem = async (itemId: number) => {
    try {
      const updatedCart = await removeCartItem(itemId);
      setCart(updatedCart);
    } catch (err) {
      console.error("Failed to delete item from cart", err);
    }
  };

  const handleCityChange = (val: string) => {
    setDomesticCity(val);
    setSelectedCityRef("");
    setWarehouses([]);
    setDomesticBranch("");
    if (searchTimeoutRef.current) clearTimeout(searchTimeoutRef.current);
    if (!val.trim()) {
      setCitySuggestions([]);
      setShowCityDropdown(false);
      return;
    }
    searchTimeoutRef.current = setTimeout(async () => {
      try {
        const data = await fetchNovaPoshtaCities(val.trim());
        setCitySuggestions(data);
        setShowCityDropdown(data.length > 0);
      } catch (err) {
        console.error("Failed to fetch cities", err);
      }
    }, 400);
  };

  const handleSelectCity = async (city: NovaPoshtaCity) => {
    setDomesticCity(city.Description);
    setSelectedCityRef(city.Ref);
    setCitySuggestions([]);
    setShowCityDropdown(false);
    setLoadingWarehouses(true);
    try {
      const whData = await fetchNovaPoshtaWarehouses(city.Ref);
      setWarehouses(whData);
    } catch (err) {
      console.error("Failed to fetch warehouses", err);
    } finally {
      setLoadingWarehouses(false);
    }
  };

  const handleSelectBranch = (wh: NovaPoshtaWarehouse) => {
    setDomesticBranch(wh.Description);
    setBranchFilter("");
    setShowBranchDropdown(false);
  };

  const filteredWarehouses = useMemo(() => {
    if (!branchFilter.trim()) return warehouses;
    const q = branchFilter.toLowerCase();
    return warehouses.filter((w) => w.Description.toLowerCase().includes(q));
  }, [warehouses, branchFilter]);

  const selectedShippingMethod = useMemo(() => {
    if (shippingMethods.length === 0) return null;
    if (deliveryTab === "international") {
      return (
        shippingMethods.find(
          (m) =>
            m.carrierCode === "UkrPoshta" ||
            m.name.toLowerCase().includes("ukr"),
        ) || shippingMethods[0]
      );
    }
    if (carrier === "NovaPoshta") {
      return (
        shippingMethods.find(
          (m) =>
            m.carrierCode === "NovaPoshta" ||
            m.name.toLowerCase().includes("nova"),
        ) || shippingMethods[0]
      );
    }
    return (
      shippingMethods.find(
        (m) =>
          m.carrierCode === "UkrPoshta" || m.name.toLowerCase().includes("ukr"),
      ) || shippingMethods[0]
    );
  }, [shippingMethods, deliveryTab, carrier]);

  const cartItemsWithMetadata = useMemo(() => {
    if (!cart) return [];
    return cart.items.map((item) => {
      const product = catalogProducts.find((p) => p.id === item.productId);
      return {
        ...item,
        name: product?.name || `Книга #${item.productId}`,
        imageUrl: product?.imageUrls?.[0] || "",
        slug: product?.slug || "",
        author: product?.author || "Невідомий автор",
        stockStatus:
          product?.availabilityStatus === "InStock" || (product?.stock ?? 0) > 0
            ? "В наявності"
            : "Немає в наявності",
      };
    });
  }, [cart, catalogProducts]);

  const giftWrapCost = wrapAsGift ? 49 : 0;
  const shippingCost = selectedShippingMethod
    ? selectedShippingMethod.price
    : 0;
  const totalItemsAmount = cart ? cart.totalAmount : 0;
  const totalAmountToPay = totalItemsAmount + shippingCost + giftWrapCost;

  const handlePlaceOrder = async (e: React.FormEvent) => {
    e.preventDefault();
    if (
      !contacts.firstName.trim() ||
      !contacts.lastName.trim() ||
      !contacts.phone.trim()
    ) {
      setErrorMsg(
        "Заповніть обов'язкові контактні дані (Ім'я, Прізвище, Телефон).",
      );
      return;
    }
    if (!selectedShippingMethod) {
      setErrorMsg("Спосіб доставки недоступний.");
      return;
    }
    let street = "",
      city = "",
      state = "",
      postalCode = "",
      country = "Україна";
    if (deliveryTab === "domestic") {
      if (carrier === "NovaPoshta") {
        if (!domesticCity.trim() || !domesticBranch.trim()) {
          setErrorMsg("Вкажіть місто та відділення Нової Пошти.");
          return;
        }
        city = domesticCity;
        street = domesticBranch;
        state = "UA-NP";
        postalCode = "00000";
      } else {
        if (
          !ukrposhtaCity.trim() ||
          !ukrposhtaIndex.trim() ||
          !ukrposhtaBranch.trim()
        ) {
          setErrorMsg("Заповніть всі поля Укрпошти.");
          return;
        }
        city = ukrposhtaCity;
        street = `Відділення: ${ukrposhtaBranch}`;
        state = "UA-UP";
        postalCode = ukrposhtaIndex;
      }
    } else {
      if (
        !addressDetails.city.trim() ||
        !addressDetails.street.trim() ||
        !addressDetails.postalCode.trim()
      ) {
        setErrorMsg("Заповніть всі адресні поля.");
        return;
      }
      city = addressDetails.city;
      street = addressDetails.street;
      state = addressDetails.state || "INT";
      postalCode = addressDetails.postalCode;
      country = addressDetails.country;
    }
    setSubmitting(true);
    setErrorMsg("");
    try {
      const idempotencyKey = generateUUID();
      const payload: CheckoutCartRequest = {
        paymentMethod,
        shippingMethodId: selectedShippingMethod.id,
        address: {
          firstName: contacts.firstName,
          lastName: contacts.lastName,
          phone: contacts.phone,
          street,
          city,
          state,
          postalCode,
          country,
        },
        notes: notes.trim() || undefined,
      };
      const result = await submitCheckout(payload, idempotencyKey);

      const firstOrderWithPayment = result.createdOrders?.find(
        (o) => o.payment?.checkoutUrl,
      );
      if (
        paymentMethod === "Card" &&
        firstOrderWithPayment?.payment?.checkoutUrl
      ) {
        window.location.href = firstOrderWithPayment.payment.checkoutUrl;
        return;
      }

      const orderNumbers =
        result.createdOrders?.map((o) => o.orderNumber).join(", ") || "";
      const totalToPay =
        result.createdOrders?.reduce(
          (sum, o) => sum + Number(o.totalPrice),
          0,
        ) || totalAmountToPay;

      setSuccessOrder({ orderNumber: orderNumbers, total: totalToPay });
    } catch (err) {
      const errorObj = err as {
        response?: { data?: { detail?: string; title?: string } };
      };
      const serverError =
        errorObj.response?.data?.detail ||
        errorObj.response?.data?.title ||
        "Помилка при оформленні замовлення.";
      setErrorMsg(serverError);
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <PageLayout>
        <div className={styles.centerContainer}>
          <Spinner size="lg" />
          <p className={styles.loadingText}>Завантаження замовлення...</p>
        </div>
      </PageLayout>
    );
  }

  if (successOrder) {
    return (
      <PageLayout>
        <div className={styles.successWrapper}>
          <div className={styles.successCard}>
            <div className={styles.successIconWrap}>
              <svg
                className={styles.successCheckmark}
                xmlns="http://www.w3.org/2000/svg"
                viewBox="0 0 52 52"
              >
                <circle
                  className={styles.checkCircle}
                  cx="26"
                  cy="26"
                  r="25"
                  fill="none"
                />
                <path
                  className={styles.checkPath}
                  fill="none"
                  d="M14.1 27.2l7.1 7.2 16.7-16.8"
                />
              </svg>
            </div>
            <h2 className={styles.successTitle} data-testid="checkout-success">
              Замовлення успішно створено!
            </h2>
            <p className={styles.successSub}>
              Дякуємо за покупку. Наш менеджер зв&apos;яжеться з вами найближчим
              часом.
            </p>
            <div className={styles.successInfo}>
              <div className={styles.successRow}>
                <span>Номер замовлення:</span>
                <span>{successOrder.orderNumber}</span>
              </div>
              <div className={styles.successRow}>
                <span>Сума до сплати:</span>
                <span>{successOrder.total} ₴</span>
              </div>
            </div>
            <Link href="/">
              <Button variant="primary" size="lg">
                На головну
              </Button>
            </Link>
          </div>
        </div>
      </PageLayout>
    );
  }

  return (
    <PageLayout>
      <form onSubmit={handlePlaceOrder} className={styles.root}>
        {}
        <div className={styles.titleRow}>
          <h1 className={styles.pageTitle}>Оформлення замовлення</h1>
          <Link href="/" className={styles.closeBtn} aria-label="Закрити">
            <svg width="32" height="32" viewBox="0 0 32 32" fill="none">
              <path
                d="M8 8L24 24M24 8L8 24"
                stroke="#ffffff"
                strokeWidth="2"
                strokeLinecap="round"
              />
            </svg>
          </Link>
        </div>
        {errorMsg && (
          <div className={styles.errorAlert} data-testid="checkout-error" role="alert">
            {errorMsg}
          </div>
        )}
        <div className={styles.colsContainer}>
          <div className={styles.leftCol}>
            {/* ══════════════════════════════════════════════ */}
            {}
            {/* ══════════════════════════════════════════════ */}
            <section className={styles.section}>
              <h2 className={styles.sectionHeading}>Контактні дані</h2>

              {!isAuthenticated && (
                <button
                  type="button"
                  className={styles.pinkBtn}
                  onClick={() => router.push("/auth/login?redirect=/checkout")}
                >
                  <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
                    <path
                      d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"
                      stroke="#fff"
                      strokeWidth="2"
                    />
                    <circle
                      cx="12"
                      cy="7"
                      r="4"
                      stroke="#fff"
                      strokeWidth="2"
                    />
                  </svg>
                  <span>Увійти або зареєструватися</span>
                  <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
                    <path
                      d="M9 18l6-6-6-6"
                      stroke="#fff"
                      strokeWidth="2"
                      strokeLinecap="round"
                      strokeLinejoin="round"
                    />
                  </svg>
                </button>
              )}

              <div className={styles.fieldsStack}>
                {/* Ім'я */}
                <div className={styles.fieldGroup}>
                  <label className={styles.fieldLabel}>Ім&apos;я</label>
                  <div className={styles.inputBox}>
                    <input
                      type="text"
                      className={styles.input}
                      data-testid="checkout-first-name"
                      value={contacts.firstName}
                      onChange={(e) =>
                        setContacts({ ...contacts, firstName: e.target.value })
                      }
                      placeholder="Ім'я"
                    />
                  </div>
                </div>

                {/* Прізвище */}
                <div className={styles.fieldGroup}>
                  <label className={styles.fieldLabel}>Прізвище</label>
                  <div className={styles.inputBox}>
                    <input
                      type="text"
                      className={styles.input}
                      data-testid="checkout-last-name"
                      value={contacts.lastName}
                      onChange={(e) =>
                        setContacts({ ...contacts, lastName: e.target.value })
                      }
                      placeholder="Прізвище"
                    />
                  </div>
                </div>

                {/* По батькові */}
                <div className={styles.fieldGroup}>
                  <label className={styles.fieldLabel}>По батькові</label>
                  <div className={styles.inputBox}>
                    <input
                      type="text"
                      className={styles.input}
                      value={contacts.middleName}
                      onChange={(e) =>
                        setContacts({ ...contacts, middleName: e.target.value })
                      }
                      placeholder="По батькові"
                    />
                  </div>
                </div>

                {/* Номер */}
                <div className={styles.fieldGroup}>
                  <label className={styles.fieldLabel}>Номер</label>
                  <div className={styles.phoneInputWrapper}>
                    <PhoneInput
                      defaultCountry="ua"
                      value={contacts.phone}
                      onChange={(phone) =>
                        setContacts((prev) => ({ ...prev, phone }))
                      }
                      countries={filteredCountries}
                      preferredCountries={["ua", "pl", "de", "gb", "us"]}
                      forceDialCode
                    />
                  </div>
                </div>

                {/* E-mail */}
                <div className={styles.fieldGroup}>
                  <label className={styles.fieldLabel}>E-mail</label>
                  <div className={styles.inputBox}>
                    <input
                      type="email"
                      className={styles.input}
                      value={contacts.email}
                      onChange={(e) =>
                        setContacts({ ...contacts, email: e.target.value })
                      }
                      placeholder="email@example.com"
                    />
                  </div>
                </div>
              </div>
            </section>

            {/* ══════════════════════════════════════════════ */}
            {}
            {/* ══════════════════════════════════════════════ */}
            <section className={styles.section}>
              <h2 className={styles.sectionHeading}>Доставка</h2>

              {/* Tabs: По Україні / Міжнародна */}
              <div className={styles.deliveryTabs}>
                <button
                  type="button"
                  className={`${styles.deliveryTab} ${deliveryTab === "domestic" ? styles.deliveryTabActive : ""}`}
                  onClick={() => setDeliveryTab("domestic")}
                >
                  <span>По Україні</span>
                </button>
                <button
                  type="button"
                  className={`${styles.deliveryTab} ${deliveryTab === "international" ? styles.deliveryTabActive : ""}`}
                  onClick={() => setDeliveryTab("international")}
                >
                  <span>Міжнародна доставка</span>
                </button>
              </div>

              {deliveryTab === "domestic" ? (
                <div className={styles.deliveryBody}>
                  {/* Carrier radio buttons */}
                  <div className={styles.carrierRadios}>
                    <label
                      className={styles.radioLabel}
                      onClick={() => setCarrier("NovaPoshta")}
                    >
                      <span
                        className={`${styles.radioCircle} ${carrier === "NovaPoshta" ? styles.radioActive : ""}`}
                      >
                        {carrier === "NovaPoshta" && (
                          <span className={styles.radioDot} />
                        )}
                      </span>
                      <span className={styles.radioText}>Нова пошта</span>
                    </label>
                    <label
                      className={styles.radioLabel}
                      data-testid="checkout-carrier-ukrposhta"
                      onClick={() => setCarrier("UkrPoshta")}
                    >
                      <span
                        className={`${styles.radioCircle} ${carrier === "UkrPoshta" ? styles.radioActive : ""}`}
                      >
                        {carrier === "UkrPoshta" && (
                          <span className={styles.radioDot} />
                        )}
                      </span>
                      <span className={styles.radioText}>Укрпошта</span>
                    </label>
                  </div>

                  {carrier === "NovaPoshta" ? (
                    <div className={styles.deliveryFields}>
                      {/* Місто - custom dropdown */}
                      <div className={styles.selectBox} ref={cityDropdownRef}>
                        <button
                          type="button"
                          className={styles.selectTrigger}
                          onClick={() => {
                            if (!showCityDropdown) setShowCityDropdown(true);
                          }}
                        >
                          <span className={styles.selectValue}>
                            {domesticCity || "Місто"}
                          </span>
                          <svg
                            width="14"
                            height="8"
                            viewBox="0 0 14 8"
                            fill="none"
                          >
                            <path
                              d="M1 1l6 6 6-6"
                              stroke="#fff"
                              strokeWidth="2"
                              strokeLinecap="round"
                              strokeLinejoin="round"
                            />
                          </svg>
                        </button>
                        {showCityDropdown && (
                          <div className={styles.dropdown}>
                            <input
                              type="text"
                              className={styles.dropdownSearch}
                              placeholder="Почніть вводити назву міста..."
                              value={domesticCity}
                              onChange={(e) => handleCityChange(e.target.value)}
                              autoFocus
                            />
                            {citySuggestions.length > 0 && (
                              <ul className={styles.dropdownList}>
                                {citySuggestions.map((city) => (
                                  <li
                                    key={city.Ref}
                                    className={styles.dropdownItem}
                                    onClick={() => handleSelectCity(city)}
                                  >
                                    {city.Description}
                                  </li>
                                ))}
                              </ul>
                            )}
                          </div>
                        )}
                      </div>

                      {/* Відділення або поштомат - custom dropdown */}
                      <div className={styles.selectBox} ref={branchDropdownRef}>
                        <button
                          type="button"
                          className={styles.selectTrigger}
                          onClick={() => {
                            if (selectedCityRef && !loadingWarehouses)
                              setShowBranchDropdown(!showBranchDropdown);
                          }}
                          disabled={!selectedCityRef || loadingWarehouses}
                        >
                          <span className={styles.selectValue}>
                            {loadingWarehouses
                              ? "Завантаження..."
                              : domesticBranch || "Відділення або поштомат"}
                          </span>
                          <svg
                            width="14"
                            height="8"
                            viewBox="0 0 14 8"
                            fill="none"
                          >
                            <path
                              d="M1 1l6 6 6-6"
                              stroke="#fff"
                              strokeWidth="2"
                              strokeLinecap="round"
                              strokeLinejoin="round"
                            />
                          </svg>
                        </button>
                        {showBranchDropdown && warehouses.length > 0 && (
                          <div className={styles.dropdown}>
                            <input
                              type="text"
                              className={styles.dropdownSearch}
                              placeholder="Пошук відділення..."
                              value={branchFilter}
                              onChange={(e) => setBranchFilter(e.target.value)}
                              autoFocus
                            />
                            <ul className={styles.dropdownList}>
                              {filteredWarehouses.map((wh) => (
                                <li
                                  key={wh.Ref}
                                  className={styles.dropdownItem}
                                  onClick={() => handleSelectBranch(wh)}
                                >
                                  {wh.Description}
                                </li>
                              ))}
                            </ul>
                          </div>
                        )}
                      </div>
                    </div>
                  ) : (
                    <div className={styles.deliveryFields}>
                      <div className={styles.selectBox}>
                        <div className={styles.selectTrigger}>
                          <input
                            type="text"
                            className={styles.selectInput}
                            data-testid="checkout-ukrposhta-city"
                            placeholder="Місто / Село"
                            value={ukrposhtaCity}
                            onChange={(e) => setUkrposhtaCity(e.target.value)}
                          />
                        </div>
                      </div>
                      <div className={styles.selectBox}>
                        <div className={styles.selectTrigger}>
                          <input
                            type="text"
                            className={styles.selectInput}
                            data-testid="checkout-ukrposhta-index"
                            placeholder="Поштовий індекс (5 цифр)"
                            value={ukrposhtaIndex}
                            onChange={(e) => setUkrposhtaIndex(e.target.value)}
                            maxLength={5}
                          />
                        </div>
                      </div>
                      <div className={styles.selectBox}>
                        <div className={styles.selectTrigger}>
                          <input
                            type="text"
                            className={styles.selectInput}
                            data-testid="checkout-ukrposhta-branch"
                            placeholder="Відділення або поштомат"
                            value={ukrposhtaBranch}
                            onChange={(e) => setUkrposhtaBranch(e.target.value)}
                          />
                        </div>
                      </div>
                    </div>
                  )}
                </div>
              ) : (
                <div className={styles.deliveryBody}>
                  <div className={styles.deliveryFields}>
                    <div className={styles.selectBox}>
                      <div className={styles.selectTrigger}>
                        <input
                          type="text"
                          className={styles.selectInput}
                          placeholder="Країна"
                          value={addressDetails.country}
                          onChange={(e) =>
                            setAddressDetails({
                              ...addressDetails,
                              country: e.target.value,
                            })
                          }
                        />
                      </div>
                    </div>
                    <div className={styles.selectBox}>
                      <div className={styles.selectTrigger}>
                        <input
                          type="text"
                          className={styles.selectInput}
                          placeholder="Область / Регіон"
                          value={addressDetails.state}
                          onChange={(e) =>
                            setAddressDetails({
                              ...addressDetails,
                              state: e.target.value,
                            })
                          }
                        />
                      </div>
                    </div>
                    <div className={styles.selectBox}>
                      <div className={styles.selectTrigger}>
                        <input
                          type="text"
                          className={styles.selectInput}
                          placeholder="Місто"
                          value={addressDetails.city}
                          onChange={(e) =>
                            setAddressDetails({
                              ...addressDetails,
                              city: e.target.value,
                            })
                          }
                        />
                      </div>
                    </div>
                    <div className={styles.selectBox}>
                      <div className={styles.selectTrigger}>
                        <input
                          type="text"
                          className={styles.selectInput}
                          placeholder="Вулиця, будинок, квартира"
                          value={addressDetails.street}
                          onChange={(e) =>
                            setAddressDetails({
                              ...addressDetails,
                              street: e.target.value,
                            })
                          }
                        />
                      </div>
                    </div>
                    <div className={styles.selectBox}>
                      <div className={styles.selectTrigger}>
                        <input
                          type="text"
                          className={styles.selectInput}
                          placeholder="Поштовий індекс"
                          value={addressDetails.postalCode}
                          onChange={(e) =>
                            setAddressDetails({
                              ...addressDetails,
                              postalCode: e.target.value,
                            })
                          }
                        />
                      </div>
                    </div>
                  </div>
                </div>
              )}
            </section>

            {/* ══════════════════════════════════════════════ */}
            {}
            {/* ══════════════════════════════════════════════ */}
            <section className={styles.section}>
              <h3 className={styles.paymentHeading}>Оберіть спосіб оплати</h3>

              <div className={styles.paymentOptions}>
                <label
                  className={styles.radioLabel}
                  data-testid="checkout-payment-card"
                  onClick={() => setPaymentMethod("Card")}
                >
                  <span
                    className={`${styles.radioCircle} ${paymentMethod === "Card" ? styles.radioActive : ""}`}
                  >
                    {paymentMethod === "Card" && (
                      <span className={styles.radioDot} />
                    )}
                  </span>
                  <span className={styles.radioText}>Картою онлайн</span>
                </label>

                {/* Payment brand icons row */}
                {paymentMethod === "Card" && (
                  <div className={styles.paymentIcons}>
                    {/* Mastercard */}
                    <div className={styles.paymentIconCard}>
                      <Image
                        src="https://upload.wikimedia.org/wikipedia/commons/0/04/Mastercard-logo.png"
                        alt="Mastercard"
                        width={58}
                        height={40}
                        className={styles.paymentLogoImg}
                      />
                    </div>
                    {/* Visa */}
                    <div className={styles.paymentIconCard}>
                      <Image
                        src="https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTAPvfATaZfmrEIr1LZMPUn8tZzXXXtgVm3KDml0IHowQ&s=10"
                        alt="Visa"
                        width={58}
                        height={40}
                        className={styles.paymentLogoImg}
                      />
                    </div>
                    {/* Apple Pay */}
                    <div className={styles.paymentIconCard}>
                      <Image
                        src="https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRBy5P3IYlnd7aUxExdO2H60Herj-2IXHU5ucHgSTndlw&s=10"
                        alt="Apple Pay"
                        width={58}
                        height={40}
                        className={styles.paymentLogoImg}
                      />
                    </div>
                    {/* Google Pay */}
                    <div className={styles.paymentIconCard}>
                      <Image
                        src="https://cdn-icons-png.flaticon.com/512/6124/6124998.png"
                        alt="Google Pay"
                        width={58}
                        height={40}
                        className={styles.paymentLogoImg}
                      />
                    </div>
                  </div>
                )}

                <label
                  className={styles.radioLabel}
                  data-testid="checkout-payment-cash"
                  onClick={() => setPaymentMethod("Cash")}
                >
                  <span
                    className={`${styles.radioCircle} ${paymentMethod === "Cash" ? styles.radioActive : ""}`}
                  >
                    {paymentMethod === "Cash" && (
                      <span className={styles.radioDot} />
                    )}
                  </span>
                  <span className={styles.radioText}>
                    Під час отримання товару
                  </span>
                </label>
              </div>
            </section>
          </div>{" "}
          {/* end of leftCol */}
          <div className={styles.rightCol}>
            {/* ══════════════════════════════════════════════ */}
            {}
            {/* ══════════════════════════════════════════════ */}
            <section className={styles.section}>
              <h2 className={styles.sectionHeading}>Оплата</h2>

              {/* Order items sub-section */}
              <div className={styles.orderHeader}>
                <span className={styles.orderLabel}>Замовлення</span>
                <button
                  type="button"
                  className={styles.editLink}
                  onClick={() => setEditingItems(!editingItems)}
                >
                  {editingItems ? "Готово" : "Редагувати товари"}
                </button>
              </div>

              <div className={styles.orderItems}>
                {cartItemsWithMetadata.map((item) => (
                  <div key={item.id} className={styles.orderItem}>
                    <div className={styles.orderItemTop}>
                      <div className={styles.orderItemContent}>
                        <div className={styles.orderItemImg}>
                          {item.imageUrl ? (
                            <Image
                              src={item.imageUrl}
                              alt={item.name}
                              fill
                              className={styles.orderImg}
                              sizes="74px"
                            />
                          ) : (
                            <div className={styles.orderImgFallback} />
                          )}
                        </div>
                        <div className={styles.orderItemMeta}>
                          <span className={styles.authorText}>
                            {item.author}
                          </span>
                          <div className={styles.stockInfo}>
                            <span className={styles.stockGreen}>
                              {item.stockStatus}
                            </span>
                            <span className={styles.bookTitle}>
                              {item.name}
                            </span>
                          </div>
                        </div>
                      </div>
                    </div>
                    <div className={styles.orderItemBottom}>
                      {editingItems ? (
                        <>
                          <div className={styles.qtyEditor}>
                            <button
                              type="button"
                              className={styles.qtyBtn}
                              onClick={() =>
                                handleUpdateQty(item.id, item.quantity, -1)
                              }
                              disabled={item.quantity <= 1}
                            >
                              -
                            </button>
                            <span className={styles.qtyText}>
                              {item.quantity} шт
                            </span>
                            <button
                              type="button"
                              className={styles.qtyBtn}
                              onClick={() =>
                                handleUpdateQty(item.id, item.quantity, 1)
                              }
                            >
                              +
                            </button>
                          </div>
                          <button
                            type="button"
                            className={styles.deleteBtn}
                            onClick={() => handleDeleteItem(item.id)}
                            aria-label="Видалити"
                          >
                            <svg
                              width="18"
                              height="18"
                              viewBox="0 0 24 24"
                              fill="none"
                            >
                              <path
                                d="M19 7l-.867 12.142A2 2 0 0 1 16.138 21H7.862a2 2 0 0 1-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 0 0-1-1h-4a1 1 0 0 0-1 1v3M4 7h16"
                                stroke="currentColor"
                                strokeWidth="2"
                                strokeLinecap="round"
                                strokeLinejoin="round"
                              />
                            </svg>
                          </button>
                        </>
                      ) : (
                        <span className={styles.qtyText}>
                          {item.quantity} шт
                        </span>
                      )}
                      <span className={styles.priceText}>
                        {item.priceAtMoment * item.quantity} грн.
                      </span>
                    </div>
                  </div>
                ))}
              </div>

              {/* Items total row */}
              <div className={styles.itemsTotalRow}>
                <span className={styles.itemsCount}>
                  {cart?.totalItems} товари
                </span>
                <span className={styles.itemsTotal}>
                  Разом: {totalItemsAmount}грн.
                </span>
              </div>
            </section>

            {}
            <div
              className={styles.giftRow}
              onClick={() => setWrapAsGift(!wrapAsGift)}
            >
              <div
                className={`${styles.checkbox} ${wrapAsGift ? styles.checkboxChecked : ""}`}
              >
                {wrapAsGift && (
                  <svg width="8" height="6" viewBox="0 0 8 6" fill="none">
                    <path
                      d="M1 3l2 2 4-4"
                      stroke="#fff"
                      strokeWidth="1.5"
                      strokeLinecap="round"
                      strokeLinejoin="round"
                    />
                  </svg>
                )}
              </div>
              <span className={styles.giftText}>
                Загорнути в подарункову упаковку
              </span>
            </div>

            {/* ══════════════════════════════════════════════ */}
            {}
            {/* ══════════════════════════════════════════════ */}
            <section className={styles.totalsSection}>
              <h3 className={styles.totalsHeading}>Разом</h3>
              <div className={styles.totalsRows}>
                <div className={styles.totalsRow}>
                  <span>{cart?.totalItems} товари на суму</span>
                  <span>{totalItemsAmount}грн.</span>
                </div>
                <div className={styles.totalsRow}>
                  <span>Вартість доставки</span>
                  <span>
                    {shippingCost === 0 ? "Безкоштовно" : `${shippingCost}грн.`}
                  </span>
                </div>
                {wrapAsGift && (
                  <div className={styles.totalsRow}>
                    <span>Подарункова упаковка</span>
                    <span>49грн.</span>
                  </div>
                )}
              </div>
              <div className={styles.totalFinal}>
                <span>До сплати</span>
                <span>{totalAmountToPay}грн.</span>
              </div>
            </section>

            {}
            <button
              type="submit"
              className={styles.pinkBtn}
              data-testid="checkout-submit"
              disabled={submitting}
            >
              <span>
                {submitting ? "Оформлення..." : "Підтвердити замовлення"}
              </span>
            </button>
          </div>{" "}
          {/* end of rightCol */}
        </div>{" "}
        {/* end of colsContainer */}
      </form>
    </PageLayout>
  );
}
