export function roundUpMoney(value: number, fractionDigits = 2): number {
  const factor = 10 ** fractionDigits;
  const normalized = Number(value.toFixed(10));

  return Math.ceil(normalized * factor) / factor;
}

export function formatCartPrice(value: number): string {
  return roundUpMoney(value).toFixed(2);
}
