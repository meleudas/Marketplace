"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState, useCallback } from "react";
import { useAuth } from "@/features/auth/model/auth.store";
import { PageLayout } from "@/shared/ui/PageLayout";
import {
  ChevronLeftIcon,
  ChevronRightIcon,
  EditIcon,
  GiftIcon,
  LogOutIcon,
  PlusIcon,
  ShopIcon,
  TrashIcon,
  SleapCat,
  InitialsAvatar,
  TextField,
} from "@/shared/ui";
import {
  fetchAddresses,
  createAddress,
  deleteAddress,
  fetchOrders,
  UserAddress,
  OrderListItem,
} from "../api/me.api";
import {
  getNormalizedStatus,
  getStatusClass,
  getStatusLabel,
  type OrderStatusClassNames,
} from "../lib/order-status";
import styles from "./MeScreen.module.css";




interface Recipient {
  id: string;
  name: string;
  phone: string;
  initial: string;
}



interface Certificate {
  id: string;
  title: string;
  price: string;
}



type OrderTab = "all" | "active" | "completed";

interface ProfileState {
  firstName: string;
  lastName: string;
  middleName: string;
  birthday: string;
  gender: string;
  phone: string;
  email: string;
}

function BackNav() {
  return (
    <Link href="/" className={styles.backNav}>
      <ChevronLeftIcon className={styles.backNavIcon} />
      <span className={styles.backNavTitle}>Профіль</span>
    </Link>
  );
}

interface LogoutSectionProps {
  onLogout: () => void;
}

function LogoutSection({ onLogout }: LogoutSectionProps) {
  return (
    <button type="button" onClick={onLogout} className={styles.logoutButton}>
      <LogOutIcon className={styles.logoutIcon} />
      <span>Вийти з профілю</span>
    </button>
  );
}

interface PersonalDataProps {
  firstName: string;
  lastName: string;
  middleName: string;
  birthday: string;
  onEdit: () => void;
}

function PersonalDataSection({ firstName, lastName, middleName, birthday, onEdit }: PersonalDataProps) {
  const birthdayStr = birthday
    ? new Date(birthday).toLocaleDateString("uk-UA", {
      day: "numeric",
      month: "long",
      year: "numeric",
    })
    : "Не вказана";

  return (
    <section className={styles.card}>
      <div className={styles.sectionHeader}>
        <h2 className={styles.sectionTitle}>Персональні дані</h2>
        <button type="button" onClick={onEdit} className={styles.editButton} aria-label="Редагувати персональні дані">
          <EditIcon className={styles.editIcon} />
        </button>
      </div>

      <div className={styles.fieldsGroupWithDivider}>
        <div className={styles.fieldRow}>
          <p className={styles.fieldLabel}>Ім’я</p>
          <p className={styles.fieldValue}>{firstName || "Не вказано"}</p>
        </div>
        <div className={styles.fieldRow}>
          <p className={styles.fieldLabel}>Прізвище</p>
          <p className={styles.fieldValue}>{lastName || "Не вказано"}</p>
        </div>
        <div className={styles.fieldRow}>
          <p className={styles.fieldLabel}>По батькові</p>
          <p className={styles.fieldValue}>{middleName || "Не вказано"}</p>
        </div>
      </div>

      <div className={styles.fieldsGroup}>
        <div className={styles.fieldRow}>
          <p className={styles.fieldLabel}>Дата народження</p>
          <p className={styles.fieldValue}>{birthdayStr}</p>
        </div>
      </div>
    </section>
  );
}

interface ContactsSectionProps {
  phone: string;
  email: string;
  onEdit: () => void;
}

function ContactsSection({ phone, email, onEdit }: ContactsSectionProps) {
  return (
    <section className={styles.card}>
      <div className={styles.sectionHeader}>
        <h2 className={styles.sectionTitle}>Контакти</h2>
        <button type="button" onClick={onEdit} className={styles.editButton} aria-label="Редагувати контакти">
          <EditIcon className={styles.editIcon} />
        </button>
      </div>

      <div className={styles.fieldsGroup}>
        <div className={styles.fieldRow}>
          <p className={styles.fieldLabel}>Номер телефону</p>
          <p className={styles.fieldValue}>{phone || "Не вказано"}</p>
        </div>
        <div className={styles.fieldRow}>
          <p className={styles.fieldLabel}>Електронна пошта</p>
          <p className={styles.fieldValue}>{email || "Не вказано"}</p>
        </div>
      </div>
    </section>
  );
}

interface OrdersSectionProps {
  apiOrders: OrderListItem[];
}

function OrdersSection({ apiOrders }: OrdersSectionProps) {
  const [activeTab, setActiveTab] = useState<OrderTab>("all");

  const hasApiOrders = apiOrders && apiOrders.length > 0;

  const displayOrdersList = hasApiOrders
    ? apiOrders
    : [
      {
        orderId: 1,
        orderNumber: "2345678901354",
        status: "Delivered",
        totalPrice: 1900,
        createdAt: "2026-12-09T12:00:00Z",
        customerId: "",
        companyId: "",
        paymentMethod: "Card",
        updatedAt: "",
      },
      {
        orderId: 2,
        orderNumber: "2345678901355",
        status: "Shipped",
        totalPrice: 950,
        createdAt: "2026-12-10T14:30:00Z",
        customerId: "",
        companyId: "",
        paymentMethod: "Card",
        updatedAt: "",
      },
    ];

  const filteredOrders = displayOrdersList.filter((order) => {
    if (activeTab === "all") return true;
    const normalized = getNormalizedStatus(order.status);
    const isActive = ["pending", "processing", "shipped"].includes(normalized);
    const isCompleted = ["delivered", "cancelled", "refunded"].includes(normalized);
    return activeTab === "active" ? isActive : isCompleted;
  });

  return (
    <section className={`${styles.card} ${styles.ordersCard}`}>
      <SleapCat className={styles.catIllustration} />

      <h2 className={styles.sectionTitle}>Замовлення</h2>

      <div className={styles.tabsRow}>
        <button
          type="button"
          className={`${styles.tab} ${activeTab === "all" ? styles.tabActive : ""}`}
          onClick={() => setActiveTab("all")}
        >
          Усі
        </button>
        <button
          type="button"
          className={`${styles.tab} ${activeTab === "active" ? styles.tabActive : ""}`}
          onClick={() => setActiveTab("active")}
        >
          Активні
        </button>
        <button
          type="button"
          className={`${styles.tab} ${activeTab === "completed" ? styles.tabActive : ""}`}
          onClick={() => setActiveTab("completed")}
        >
          Завершені
        </button>
      </div>

      <div className={styles.ordersList} style={{ marginTop: "12px" }}>
        {filteredOrders.length > 0 ? (
          filteredOrders.slice(0, 3).map((order) => (
            <Link
              key={order.orderId}
              href={`/me/orders/${order.orderId}`}
              className={styles.orderListButton}
              style={{ textDecoration: "none" }}
            >
              <div className={styles.orderButtonLeft}>
                <span className={styles.orderButtonNumber}>№ {order.orderNumber}</span>
                <span className={styles.orderButtonDate}>
                  {new Date(order.createdAt).toLocaleDateString("uk-UA")}
                </span>
              </div>
              <div className={styles.orderButtonRight}>
                <span className={styles.orderButtonPrice}>{order.totalPrice} грн.</span>
                <span className={`${styles.orderStatusBadge} ${getStatusClass(order.status, styles as unknown as OrderStatusClassNames)}`}>
                  {getStatusLabel(order.status)}
                </span>
              </div>
            </Link>
          ))
        ) : (
          <div className={styles.noOrdersText}>Замовлень немає</div>
        )}
      </div>

      <Link href="/me/orders" className={styles.allOrdersButton}>
        Усі замовлення
      </Link>
    </section>
  );
}

interface DeliverySectionProps {
  addresses: UserAddress[];
  onAddAddress: (type: string) => void;
  onDeleteAddress: (id: number) => void;
}

function DeliverySection({ addresses, onAddAddress, onDeleteAddress }: DeliverySectionProps) {
  const novaPoshtaList = addresses.filter((a) => {
    const s = (a.street || "").toLowerCase();
    return s.includes("nova") || s.includes("нова") || s.includes("поштомат");
  });
  const ukrPoshtaList = addresses.filter((a) => {
    const s = (a.street || "").toLowerCase();
    return !s.includes("nova") && !s.includes("нова") && !s.includes("поштомат");
  });

  return (
    <section className={styles.card}>
      <h2 className={styles.sectionTitle}>Доставка</h2>

      <div className={styles.deliveryGroup}>
        <div className={styles.deliveryGroupHeader}>
          <h3 className={styles.deliveryGroupTitle}>Адреси для &ldquo;Нової Пошти&rdquo;</h3>
          <button
            type="button"
            className={styles.plusButton}
            onClick={() => onAddAddress("NovaPoshta")}
            aria-label="Додати адресу Нової Пошти"
          >
            <PlusIcon className={styles.plusIcon} />
          </button>
        </div>
        {novaPoshtaList.length > 0 ? (
          novaPoshtaList.map((addr) => (
            <div className={styles.addressCard} key={addr.id}>
              <div className={styles.addressInfo}>
                <div className={styles.addressAvatar}>
                  <ShopIcon className={styles.addressAvatarIcon} />
                </div>
                <div className={styles.addressTexts}>
                  <p className={styles.addressCity}>{addr.city}</p>
                  <p className={styles.addressDetail}>{addr.street}</p>
                </div>
              </div>
              <button
                type="button"
                className={styles.trashButton}
                onClick={() => onDeleteAddress(addr.id)}
                aria-label="Видалити адресу"
              >
                <TrashIcon className={styles.trashIcon} />
              </button>
            </div>
          ))
        ) : (
          <div className={styles.noOrdersText} style={{ padding: "10px 0" }}>Немає збережених адрес Нової Пошти</div>
        )}
      </div>

      <div className={styles.deliveryGroup} style={{ marginTop: "24px" }}>
        <div className={styles.deliveryGroupHeader}>
          <h3 className={styles.deliveryGroupTitle}>Адреси для &ldquo;Укрпошти&rdquo;</h3>
          <button
            type="button"
            className={styles.plusButton}
            onClick={() => onAddAddress("UkrPoshta")}
            aria-label="Додати адресу Укрпошти"
          >
            <PlusIcon className={styles.plusIcon} />
          </button>
        </div>
        {ukrPoshtaList.length > 0 ? (
          ukrPoshtaList.map((addr) => (
            <div className={styles.addressCard} key={addr.id}>
              <div className={styles.addressInfo}>
                <div className={styles.addressAvatar}>
                  <ShopIcon className={styles.addressAvatarIcon} />
                </div>
                <div className={styles.addressTexts}>
                  <p className={styles.addressCity}>{addr.city}</p>
                  <p className={styles.addressDetail}>{addr.street}</p>
                </div>
              </div>
              <button
                type="button"
                className={styles.trashButton}
                onClick={() => onDeleteAddress(addr.id)}
                aria-label="Видалити адресу"
              >
                <TrashIcon className={styles.trashIcon} />
              </button>
            </div>
          ))
        ) : (
          <div className={styles.noOrdersText} style={{ padding: "10px 0" }}>Немає збережених адрес Укрпошти</div>
        )}
      </div>
    </section>
  );
}

interface RecipientsSectionProps {
  recipients: Recipient[];
  onAdd: () => void;
  onDelete: (id: string) => void;
}

function RecipientsSection({ recipients, onAdd, onDelete }: RecipientsSectionProps) {
  return (
    <section className={styles.card}>
      <div className={styles.sectionHeader} style={{ justifyContent: "space-between" }}>
        <h2 className={styles.sectionTitle}>Отримувачі</h2>
        <button type="button" onClick={onAdd} className={styles.plusButton} aria-label="Додати отримувача">
          <PlusIcon className={styles.plusIcon} />
        </button>
      </div>

      {recipients.length > 0 ? (
        recipients.map((r) => (
          <div key={r.id} className={styles.recipientCard}>
            <div className={styles.recipientInfo}>
              <InitialsAvatar
                firstName={r.name.split(" ")[0]}
                lastName={r.name.split(" ")[1] ?? ""}
              />
              <div className={styles.recipientTexts}>
                <p className={styles.recipientPhone}>{r.phone}</p>
                <p className={styles.recipientName}>{r.name}</p>
              </div>
            </div>
            <button type="button" onClick={() => onDelete(r.id)} className={styles.trashButton} aria-label="Видалити отримувача">
              <TrashIcon className={styles.trashIcon} />
            </button>
          </div>
        ))
      ) : (
        <div className={styles.noOrdersText} style={{ padding: "10px 0" }}>Немає доданих отримувачів</div>
      )}
    </section>
  );
}

function CertificatesSection() {
  const certificates: Certificate[] = []; // No mock certificates

  return (
    <section className={styles.card}>
      <h2 className={styles.sectionTitle}>Мої сертифікати</h2>

      {certificates.length > 0 ? (
        certificates.map((cert) => (
          <div key={cert.id} className={styles.certificateCard}>
            <GiftIcon className={styles.certificateIcon} />
            <div className={styles.certificateTexts}>
              <p className={styles.certificateTitle}>{cert.title}</p>
              <p className={styles.certificatePrice}>{cert.price}</p>
            </div>
          </div>
        ))
      ) : (
        <div className={styles.noOrdersText} style={{ padding: "10px 0" }}>Немає придбаних сертифікатів</div>
      )}

      <button type="button" className={styles.certificateButton}>
        <span>Подарункові сертифікати</span>
        <ChevronRightIcon className={styles.chevronRightIcon} />
      </button>
    </section>
  );
}


export function MeScreen() {
  const router = useRouter();
  const user = useAuth((state) => state.user);
  const isAuthenticated = useAuth((state) => state.isAuthenticated);
  const initialized = useAuth((state) => state.initialized);
  const loading = useAuth((state) => state.loading);
  const loadMe = useAuth((state) => state.loadMe);
  const logout = useAuth((state) => state.logout);

  const handleLogout = useCallback(() => {
    void logout().then(() => {
      router.push("/");
    });
  }, [logout, router]);

  const [addresses, setAddresses] = useState<UserAddress[]>([]);
  const [orders, setOrders] = useState<OrderListItem[]>([]);

  // Local storage profile state
  const [profile, setProfile] = useState<ProfileState>(() => {
    if (typeof window !== "undefined") {
      const stored = localStorage.getItem("booktop_profile_overrides");
      if (stored) return JSON.parse(stored) as ProfileState;
    }
    return {
      firstName: "",
      lastName: "",
      middleName: "",
      birthday: "",
      gender: "",
      phone: "",
      email: "",
    };
  });

  // Sync profile values when backend user loaded and there are no overrides yet
  useEffect(() => {
    if (!user) {
      return;
    }

    const frameId = window.requestAnimationFrame(() => {
      setProfile((prev) => {
        const stored = localStorage.getItem("booktop_profile_overrides");
        if (stored) return JSON.parse(stored) as ProfileState;

        return {
          firstName: user.firstName || prev.firstName,
          lastName: user.lastName || prev.lastName,
          middleName: prev.middleName,
          birthday: user.birthday ? new Date(user.birthday).toISOString().split("T")[0] : prev.birthday,
          gender: prev.gender,
          phone: prev.phone,
          email: prev.email || user.email || "",
        };
      });
    });

    return () => {
      window.cancelAnimationFrame(frameId);
    };
  }, [user]);

  // Local storage recipients state
  const [recipients, setRecipients] = useState<Recipient[]>(() => {
    if (typeof window !== "undefined") {
      const stored = localStorage.getItem("booktop_recipients");
      if (stored) return JSON.parse(stored);
    }
    return [];
  });

  // Modal open states
  const [isPersonalModalOpen, setIsPersonalModalOpen] = useState(false);
  const [isContactsModalOpen, setIsContactsModalOpen] = useState(false);
  const [isAddRecipientModalOpen, setIsAddRecipientModalOpen] = useState(false);
  const [isAddAddressModalOpen, setIsAddAddressModalOpen] = useState(false);

  // Modal form states
  const [personalForm, setPersonalForm] = useState({ firstName: "", lastName: "", middleName: "", birthday: "", gender: "" });
  const [contactsForm, setContactsForm] = useState({ phone: "", email: "" });
  const [recipientForm, setRecipientForm] = useState({ name: "", phone: "" });
  const [addressForm, setAddressForm] = useState({
    type: "NovaPoshta",
    firstName: "",
    lastName: "",
    phone: "",
    city: "",
    street: "",
    state: "",
    postalCode: "",
    country: "",
    isDefault: false
  });

  const loadApiData = useCallback(async () => {
    try {
      const addressesData = await fetchAddresses();
      setAddresses(addressesData);
    } catch (e) {
      console.warn("Failed to fetch addresses from API:", e);
    }

    try {
      const pagedOrders = await fetchOrders();
      setOrders(pagedOrders.items);
    } catch (e) {
      console.warn("Failed to fetch orders from API:", e);
    }
  }, []);

  useEffect(() => {
    void loadMe();
  }, [loadMe]);

  useEffect(() => {
    if (!isAuthenticated) {
      return;
    }

    const frameId = window.requestAnimationFrame(() => {
      void loadApiData();
    });

    return () => {
      window.cancelAnimationFrame(frameId);
    };
  }, [isAuthenticated, loadApiData]);

  // Open Modals preloaded with current data
  const handleOpenPersonalModal = () => {
    setPersonalForm({
      firstName: profile.firstName,
      lastName: profile.lastName,
      middleName: profile.middleName,
      birthday: profile.birthday,
      gender: profile.gender,
    });
    setIsPersonalModalOpen(true);
  };

  const handleOpenContactsModal = () => {
    setContactsForm({
      phone: profile.phone,
      email: profile.email,
    });
    setIsContactsModalOpen(true);
  };

  const handleOpenAddRecipientModal = () => {
    setRecipientForm({ name: "", phone: "" });
    setIsAddRecipientModalOpen(true);
  };

  const handleOpenAddAddressModal = (type: string) => {
    setAddressForm({
      type,
      firstName: "",
      lastName: "",
      phone: "",
      city: "",
      street: "",
      state: "",
      postalCode: "",
      country: "",
      isDefault: false
    });
    setIsAddAddressModalOpen(true);
  };

  // Save actions
  const handleSavePersonal = () => {
    const updated = { ...profile, ...personalForm };
    setProfile(updated);
    localStorage.setItem("booktop_profile_overrides", JSON.stringify(updated));
    setIsPersonalModalOpen(false);
  };

  const handleSaveContacts = () => {
    const updated = { ...profile, phone: contactsForm.phone };
    setProfile(updated);
    localStorage.setItem("booktop_profile_overrides", JSON.stringify(updated));
    setIsContactsModalOpen(false);
  };

  const handleAddRecipient = () => {
    if (!recipientForm.name || !recipientForm.phone) return;
    const newRecipient: Recipient = {
      id: String(Date.now()),
      name: recipientForm.name,
      phone: recipientForm.phone,
      initial: recipientForm.name.charAt(0).toUpperCase(),
    };
    const updated = [...recipients, newRecipient];
    setRecipients(updated);
    localStorage.setItem("booktop_recipients", JSON.stringify(updated));
    setIsAddRecipientModalOpen(false);
  };

  const handleDeleteRecipient = (id: string) => {
    const updated = recipients.filter((r) => r.id !== id);
    setRecipients(updated);
    localStorage.setItem("booktop_recipients", JSON.stringify(updated));
  };

  const handleSaveAddress = async () => {
    try {
      await createAddress({
        ...addressForm,
        type: "Shipping", // Backend validator requires Enum value "Shipping" (0)
      });
      const updated = await fetchAddresses();
      setAddresses(updated);
      setIsAddAddressModalOpen(false);
    } catch (e) {
      console.error("Failed to create address:", e);
    }
  };

  const handleDeleteAddress = async (id: number) => {
    try {
      await deleteAddress(id);
      const updated = await fetchAddresses();
      setAddresses(updated);
    } catch (e) {
      console.error("Failed to delete address:", e);
    }
  };

  if (!initialized || loading) {
    return (
      <PageLayout>
        <div className={styles.loadingContainer}>
          <p className={styles.loadingText}>Завантаження профілю...</p>
        </div>
      </PageLayout>
    );
  }

  if (!isAuthenticated || !user) {
    return (
      <PageLayout>
        <div className={styles.loadingContainer}>
          <div className={styles.authPrompt}>
            <h1 className={styles.authTitle}>Профіль</h1>
            <p className={styles.authSubtitle}>Увійдіть, щоб переглянути свій профіль</p>
            <div className={styles.authActions}>
              <Link href="/auth" className={styles.signInButton}>
                Увійти
              </Link>
              <Link href="/" className={styles.backButton}>
                На головну
              </Link>
            </div>
          </div>
        </div>
      </PageLayout>
    );
  }

  return (
    <PageLayout className={styles.mainContainer}>
      <div className={styles.page}>
        <div className={styles.main}>
          <BackNav />
          <div className={styles.gridContainer}>
            <div className={styles.leftColumn}>
              <PersonalDataSection
                firstName={profile.firstName}
                lastName={profile.lastName}
                middleName={profile.middleName}
                birthday={profile.birthday}
                onEdit={handleOpenPersonalModal}
              />
              <ContactsSection
                phone={profile.phone}
                email={profile.email}
                onEdit={handleOpenContactsModal}
              />
              <RecipientsSection
                recipients={recipients}
                onAdd={handleOpenAddRecipientModal}
                onDelete={handleDeleteRecipient}
              />
            </div>
            <div className={styles.rightColumn}>
              <OrdersSection apiOrders={orders} />
              <DeliverySection
                addresses={addresses}
                onAddAddress={handleOpenAddAddressModal}
                onDeleteAddress={handleDeleteAddress}
              />
              <CertificatesSection />
            </div>
          </div>
          <LogoutSection onLogout={handleLogout} />
        </div>
      </div>

      {}
      {isPersonalModalOpen && (
        <div className={styles.modalOverlay} onClick={() => setIsPersonalModalOpen(false)}>
          <div className={styles.modalContent} onClick={(e) => e.stopPropagation()}>
            <div className={styles.modalHeader}>
              <h3 className={styles.modalTitle}>Персональні дані</h3>
              <button type="button" className={styles.modalClose} onClick={() => setIsPersonalModalOpen(false)}>&times;</button>
            </div>
            <div className={styles.modalBody}>
              <TextField
                label="Ім'я"
                value={personalForm.firstName}
                onChange={(e) => setPersonalForm({ ...personalForm, firstName: e.target.value })}
                className={styles.formGroup}
              />
              <TextField
                label="Прізвище"
                value={personalForm.lastName}
                onChange={(e) => setPersonalForm({ ...personalForm, lastName: e.target.value })}
                className={styles.formGroup}
              />
              <TextField
                label="По батькові"
                value={personalForm.middleName}
                onChange={(e) => setPersonalForm({ ...personalForm, middleName: e.target.value })}
                className={styles.formGroup}
              />
              <TextField
                label="Дата народження"
                type="date"
                value={personalForm.birthday}
                onChange={(e) => setPersonalForm({ ...personalForm, birthday: e.target.value })}
                className={styles.formGroup}
              />
              <div className={styles.modalActions}>
                <button type="button" className={`${styles.modalBtn} ${styles.btnCancel}`} onClick={() => setIsPersonalModalOpen(false)}>Скасувати</button>
                <button type="button" className={`${styles.modalBtn} ${styles.btnSave}`} onClick={handleSavePersonal}>Зберегти</button>
              </div>
            </div>
          </div>
        </div>
      )}

      {isContactsModalOpen && (
        <div className={styles.modalOverlay} onClick={() => setIsContactsModalOpen(false)}>
          <div className={styles.modalContent} onClick={(e) => e.stopPropagation()}>
            <div className={styles.modalHeader}>
              <h3 className={styles.modalTitle}>Контакти</h3>
              <button type="button" className={styles.modalClose} onClick={() => setIsContactsModalOpen(false)}>&times;</button>
            </div>
            <div className={styles.modalBody}>
              <TextField
                label="Номер телефону"
                kind="tel"
                value={contactsForm.phone}
                onChange={(e) => setContactsForm({ ...contactsForm, phone: e.target.value })}
                className={styles.formGroup}
              />
              <div className={styles.modalActions}>
                <button type="button" className={`${styles.modalBtn} ${styles.btnCancel}`} onClick={() => setIsContactsModalOpen(false)}>Скасувати</button>
                <button type="button" className={`${styles.modalBtn} ${styles.btnSave}`} onClick={handleSaveContacts}>Зберегти</button>
              </div>
            </div>
          </div>
        </div>
      )}

      {isAddRecipientModalOpen && (
        <div className={styles.modalOverlay} onClick={() => setIsAddRecipientModalOpen(false)}>
          <div className={styles.modalContent} onClick={(e) => e.stopPropagation()}>
            <div className={styles.modalHeader}>
              <h3 className={styles.modalTitle}>Додати отримувача</h3>
              <button type="button" className={styles.modalClose} onClick={() => setIsAddRecipientModalOpen(false)}>&times;</button>
            </div>
            <div className={styles.modalBody}>
              <TextField
                label="Ім'я та Прізвище"
                placeholder="Данило Гамаран"
                value={recipientForm.name}
                onChange={(e) => setRecipientForm({ ...recipientForm, name: e.target.value })}
                className={styles.formGroup}
              />
              <TextField
                label="Номер телефону"
                placeholder="+380 56 435 678"
                kind="tel"
                value={recipientForm.phone}
                onChange={(e) => setRecipientForm({ ...recipientForm, phone: e.target.value })}
                className={styles.formGroup}
              />
              <div className={styles.modalActions}>
                <button type="button" className={`${styles.modalBtn} ${styles.btnCancel}`} onClick={() => setIsAddRecipientModalOpen(false)}>Скасувати</button>
                <button type="button" className={`${styles.modalBtn} ${styles.btnSave}`} onClick={handleAddRecipient}>Додати</button>
              </div>
            </div>
          </div>
        </div>
      )}

      {isAddAddressModalOpen && (
        <div className={styles.modalOverlay} onClick={() => setIsAddAddressModalOpen(false)}>
          <div className={styles.modalContent} onClick={(e) => e.stopPropagation()}>
            <div className={styles.modalHeader}>
              <h3 className={styles.modalTitle}>Додати адресу доставки</h3>
              <button type="button" className={styles.modalClose} onClick={() => setIsAddAddressModalOpen(false)}>&times;</button>
            </div>
            <div className={styles.modalBody}>
              <div className={styles.formGroup}>
                <label className={styles.formLabel}>Служба доставки</label>
                <select
                  className={styles.formSelect}
                  value={addressForm.type}
                  onChange={(e) => setAddressForm({ ...addressForm, type: e.target.value })}
                >
                  <option value="NovaPoshta">Нова Пошта</option>
                  <option value="UkrPoshta">Укрпошта</option>
                </select>
              </div>
              <TextField
                label="Ім'я отримувача"
                value={addressForm.firstName}
                onChange={(e) => setAddressForm({ ...addressForm, firstName: e.target.value })}
                className={styles.formGroup}
              />
              <TextField
                label="Прізвище отримувача"
                value={addressForm.lastName}
                onChange={(e) => setAddressForm({ ...addressForm, lastName: e.target.value })}
                className={styles.formGroup}
              />
              <TextField
                label="Телефон отримувача"
                kind="tel"
                value={addressForm.phone}
                onChange={(e) => setAddressForm({ ...addressForm, phone: e.target.value })}
                className={styles.formGroup}
              />
              <TextField
                label="Місто"
                value={addressForm.city}
                onChange={(e) => setAddressForm({ ...addressForm, city: e.target.value })}
                className={styles.formGroup}
              />
              <TextField
                label="Вулиця / Відділення / Поштомат"
                placeholder={addressForm.type === "NovaPoshta" ? "Поштомат №4567, вул. Сагайдачного 54ж" : "Відділення №45, вул. Сагайдачного 54ж"}
                value={addressForm.street}
                onChange={(e) => setAddressForm({ ...addressForm, street: e.target.value })}
                className={styles.formGroup}
              />
              <TextField
                label="Поштовий індекс"
                value={addressForm.postalCode}
                onChange={(e) => setAddressForm({ ...addressForm, postalCode: e.target.value })}
                className={styles.formGroup}
              />
              <div className={styles.modalActions}>
                <button type="button" className={`${styles.modalBtn} ${styles.btnCancel}`} onClick={() => setIsAddAddressModalOpen(false)}>Скасувати</button>
                <button type="button" className={`${styles.modalBtn} ${styles.btnSave}`} onClick={handleSaveAddress}>Зберегти</button>
              </div>
            </div>
          </div>
        </div>
      )}
    </PageLayout>
  );
}
