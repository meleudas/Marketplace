import type { ProductCardData } from "./ProductCard";

/** Mock data for the UI skeleton showcase only — not wired to any API. */
export const MOCK_PRODUCTS: ProductCardData[] = [
  {
    id: "book-1",
    author: "Барбара Девіс",
    title: "Книга Відлуння старих книжок",
    price: 549,
    inStock: true,
    imageUrl: "/images/products/detective-stories-cover.png",
  },
  {
    id: "book-2",
    author: "Олена Кoval",
    title: "Тіні забутих сторінок",
    price: 420,
    inStock: true,
    imageUrl: "/images/products/detective-stories-cover.png",
  },
  {
    id: "book-3",
    author: "Михайло Грин",
    title: "Останній розділ",
    price: 699,
    inStock: false,
    imageUrl: "/images/products/detective-stories-cover.png",
  },
];
