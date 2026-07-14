export type NormalizableOrderStatus = string | number | null | undefined;

export interface OrderStatusClassNames {
  statusPending: string;
  statusPaid?: string;
  statusProcessing: string;
  statusShipped: string;
  statusDelivered: string;
  statusCancelled: string;
  statusRefunded: string;
}

export function getNormalizedStatus(status: NormalizableOrderStatus): string {
  if (status === null || status === undefined) {
    return "";
  }

  if (typeof status === "number") {
    switch (status) {
      case 0:
        return "pending";
      case 1:
        return "paid";
      case 2:
        return "processing";
      case 3:
        return "shipped";
      case 4:
        return "delivered";
      case 5:
        return "cancelled";
      case 6:
        return "refunded";
      default:
        return String(status).toLowerCase();
    }
  }

  return String(status).toLowerCase();
}

export function getStatusLabel(status: NormalizableOrderStatus): string {
  const normalized = getNormalizedStatus(status);

  switch (normalized) {
    case "pending":
      return "Очікує оплати";
    case "paid":
      return "Оплачено";
    case "processing":
      return "Обробляється";
    case "shipped":
      return "Відправлено";
    case "delivered":
      return "Доставлено";
    case "cancelled":
      return "Скасовано";
    case "refunded":
      return "Повернено";
    default:
      return String(status ?? "");
  }
}

export function getStatusClass(
  status: NormalizableOrderStatus,
  classNames: OrderStatusClassNames,
): string {
  const normalized = getNormalizedStatus(status);

  switch (normalized) {
    case "pending":
      return classNames.statusPending;
    case "paid":
      return classNames.statusPaid || classNames.statusProcessing;
    case "processing":
      return classNames.statusProcessing;
    case "shipped":
      return classNames.statusShipped;
    case "delivered":
      return classNames.statusDelivered;
    case "cancelled":
      return classNames.statusCancelled;
    case "refunded":
      return classNames.statusRefunded;
    default:
      return "";
  }
}
