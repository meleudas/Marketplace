import type { ShippingMethodDto } from "@/features/checkout/api/checkout.api";
import { formatPriceWithUnit } from "./product-details.lib";

export interface DeliveryDetailPoint {
  label: string;
  text: string;
}

export interface DeliveryDetailsContent {
  intro: string;
  points: DeliveryDetailPoint[];
}

const NOVA_POSHTA_FALLBACK = {
  priceLabel: "від 70 грн.",
  daysMin: 1,
  daysMax: 3,
  freeFrom: 1000,
};

const UKRPOSHTA_FALLBACK = {
  price: 40,
  daysMin: 3,
  daysMax: 7,
  freeFrom: 1500,
};

export function buildNovaPoshtaDeliveryDetails(
  method: ShippingMethodDto | null,
): DeliveryDetailsContent {
  const daysMin = method?.estimatedDaysMin ?? NOVA_POSHTA_FALLBACK.daysMin;
  const daysMax = method?.estimatedDaysMax ?? NOVA_POSHTA_FALLBACK.daysMax;
  const freeFrom =
    typeof method?.freeShippingThreshold === "number"
      ? method.freeShippingThreshold
      : NOVA_POSHTA_FALLBACK.freeFrom;
  const priceLabel = method
    ? `від ${formatPriceWithUnit(method.price)}`
    : NOVA_POSHTA_FALLBACK.priceLabel;

  return {
    intro:
      "По всій Україні — у відділення або поштомат. Після відправлення замовлення ви отримаєте SMS із номером експрес-накладної та зможете відстежити посилку онлайн.",
    points: [
      {
        label: "Термін",
        text: `${daysMin}–${daysMax} робочі дні після підтвердження та комплектації замовлення.`,
      },
      {
        label: "Вартість",
        text: `${priceLabel}. Точна сума залежить від ваги книг, розміру посилки та обраного відділення отримання.`,
      },
      {
        label: "Безкоштовно",
        text: `для замовлень від ${formatPriceWithUnit(freeFrom)}.`,
      },
      {
        label: "Отримання",
        text: "у відділенні або поштоматі за документом, що посвідчує особу, або за номером телефону з замовлення.",
      },
      {
        label: "Зберігання",
        text: "у відділенні — до 7 календарних днів безкоштовно, далі згідно з тарифами перевізника.",
      },
      {
        label: "Відстеження",
        text: "у мобільному застосунку або на сайті novaposhta.ua.",
      },
    ],
  };
}

export function buildUkrposhtaDeliveryDetails(
  method: ShippingMethodDto | null,
): DeliveryDetailsContent {
  const daysMin = method?.estimatedDaysMin ?? UKRPOSHTA_FALLBACK.daysMin;
  const daysMax = method?.estimatedDaysMax ?? UKRPOSHTA_FALLBACK.daysMax;
  const price = method?.price ?? UKRPOSHTA_FALLBACK.price;
  const freeFrom =
    typeof method?.freeShippingThreshold === "number"
      ? method.freeShippingThreshold
      : UKRPOSHTA_FALLBACK.freeFrom;

  return {
    intro:
      "Надійний варіант для міст і сіл, зокрема там, де немає зручних відділень інших перевізників. Після прибуття посилки вам надійде SMS-сповіщення.",
    points: [
      {
        label: "Термін",
        text: `${daysMin}–${daysMax} робочі дні після відправлення замовлення.`,
      },
      {
        label: "Тариф",
        text: `до ${formatPriceWithUnit(freeFrom)} — ${formatPriceWithUnit(price)}; від ${formatPriceWithUnit(freeFrom)} — безкоштовно.`,
      },
      {
        label: "Отримання",
        text: "у відділенні за паспортом, посвідченням водія або іншим документом, що посвідчує особу.",
      },
      {
        label: "Зберігання",
        text: "у відділенні — до 30 календарних днів згідно з тарифами перевізника.",
      },
      {
        label: "Відстеження",
        text: "на сайті track.ukrposhta.ua за номером штрих-коду.",
      },
      {
        label: "Рекомендація",
        text: "для стандартних паперових видань; для великих замовлень краще обрати Нову пошту.",
      },
    ],
  };
}
