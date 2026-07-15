"use client";

const GUEST_CART_STORAGE_KEY = "guestCart";
const GUEST_CART_EVENT = "guest-cart-changed";

export interface GuestCartItem {
  productId: number;
  quantity: number;
}

const canUseStorage = () => typeof window !== "undefined";

const isGuestCartItem = (value: unknown): value is GuestCartItem => {
  if (!value || typeof value !== "object") {
    return false;
  }

  const item = value as Record<string, unknown>;
  return (
    typeof item.productId === "number" &&
    typeof item.quantity === "number" &&
    item.quantity > 0
  );
};

const readGuestCart = (): GuestCartItem[] => {
  if (!canUseStorage()) {
    return [];
  }

  try {
    const raw = window.localStorage.getItem(GUEST_CART_STORAGE_KEY);
    if (!raw) {
      return [];
    }

    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? parsed.filter(isGuestCartItem) : [];
  } catch {
    return [];
  }
};

const writeGuestCart = (items: GuestCartItem[]): void => {
  if (!canUseStorage()) {
    return;
  }

  if (items.length === 0) {
    window.localStorage.removeItem(GUEST_CART_STORAGE_KEY);
  } else {
    window.localStorage.setItem(GUEST_CART_STORAGE_KEY, JSON.stringify(items));
  }

  window.dispatchEvent(new Event(GUEST_CART_EVENT));
};

export const getGuestCart = (): GuestCartItem[] => readGuestCart();

export const getGuestCartTotalItems = (): number =>
  readGuestCart().reduce((sum, item) => sum + item.quantity, 0);

export const setGuestCart = (items: GuestCartItem[]): void => {
  writeGuestCart(items.filter(isGuestCartItem));
};

export const addGuestCartItem = (productId: number, quantity = 1): GuestCartItem[] => {
  const items = readGuestCart();
  const existing = items.find((item) => item.productId === productId);

  const next = existing
    ? items.map((item) =>
        item.productId === productId
          ? { ...item, quantity: item.quantity + quantity }
          : item,
      )
    : [...items, { productId, quantity }];

  writeGuestCart(next);
  return next;
};

export const setGuestCartItemQuantity = (productId: number, quantity: number): GuestCartItem[] => {
  const items = readGuestCart();
  const next =
    quantity <= 0
      ? items.filter((item) => item.productId !== productId)
      : items.map((item) => (item.productId === productId ? { ...item, quantity } : item));

  writeGuestCart(next);
  return next;
};

export const removeGuestCartItem = (productId: number): GuestCartItem[] => {
  const next = readGuestCart().filter((item) => item.productId !== productId);
  writeGuestCart(next);
  return next;
};

export const clearGuestCart = (): void => {
  if (!canUseStorage()) {
    return;
  }

  window.localStorage.removeItem(GUEST_CART_STORAGE_KEY);
  window.dispatchEvent(new Event(GUEST_CART_EVENT));
};

export const subscribeToGuestCart = (callback: () => void): (() => void) => {
  if (!canUseStorage()) {
    return () => {};
  }

  window.addEventListener(GUEST_CART_EVENT, callback);
  window.addEventListener("storage", callback);

  return () => {
    window.removeEventListener(GUEST_CART_EVENT, callback);
    window.removeEventListener("storage", callback);
  };
};
