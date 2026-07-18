import Link from "next/link";
import type { MouseEvent } from "react";
import { Button } from "./Button";
import { CartIcon } from "./icons";
import iconStyles from "./icons/Icon.module.css";
import styles from "./ProductCard.module.css";

export interface ProductCardData {
  id: string;
  title: string;
  author?: string;
  price: number;
  oldPrice?: number | null;
  discountPercent?: number | null;
  imageUrl?: string;
  inStock?: boolean;
}

interface ProductCardProps {
  product: ProductCardData;
  href?: string;
  onAddToCart?: (product: ProductCardData) => void;
  isAddingToCart?: boolean;
}

interface ProductSaleInfo {
  discountPercent: number;
  oldPrice: number;
}

const formatPrice = (value: number): string =>
  `${new Intl.NumberFormat("uk-UA", { maximumFractionDigits: 0 }).format(value)} грн`;

const getSaleInfo = (product: ProductCardData): ProductSaleInfo | null => {
  const { price, oldPrice, discountPercent } = product;

  if (typeof oldPrice === "number" && oldPrice > price) {
    const resolvedDiscount =
      typeof discountPercent === "number" && discountPercent > 0
        ? Math.round(discountPercent)
        : Math.round(((oldPrice - price) / oldPrice) * 100);

    return {
      discountPercent: resolvedDiscount,
      oldPrice,
    };
  }

  return null;
};

export function ProductCard({
  product,
  href,
  onAddToCart,
  isAddingToCart = false,
}: ProductCardProps) {
  const isInStock = product.inStock ?? true;
  const sale = getSaleInfo(product);

  const handleAddToCart = (event: MouseEvent<HTMLButtonElement>) => {
    event.preventDefault();
    event.stopPropagation();
    onAddToCart?.(product);
  };

  const cardContent = (
    <>
      <div className={styles.media}>
        <div className={styles.imagePlaceholder} aria-hidden="true" />
        {product.imageUrl ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img className={styles.image} src={product.imageUrl} alt={product.title} />
        ) : null}
        {sale ? (
          <span className={styles.discountBadge} aria-label={`Знижка ${sale.discountPercent}%`}>
            -{sale.discountPercent}%
          </span>
        ) : null}
      </div>

      <div className={styles.body}>
        {product.author ? <p className={styles.author}>{product.author}</p> : <p className={styles.author} aria-hidden="true">&nbsp;</p>}

        <h3 className={styles.title}>{product.title}</h3>

        <p className={isInStock ? styles.availability : styles.availabilityOut}>
          {isInStock ? "В наявності" : "Немає в наявності"}
        </p>

        <div className={styles.footer}>
          <div className={styles.priceGroup}>
            {sale ? (
              <>
                <span className={styles.oldPrice}>{formatPrice(sale.oldPrice)}</span>
                <span className={styles.priceSale}>{formatPrice(product.price)}</span>
              </>
            ) : (
              <span className={styles.price}>{formatPrice(product.price)}</span>
            )}
          </div>
        </div>
      </div>
    </>
  );

  return (
    <article className={styles.card}>
      {href ? (
        <Link href={href} className={styles.cardNav}>
          {cardContent}
        </Link>
      ) : (
        <div className={styles.cardNav}>{cardContent}</div>
      )}

      <Button
        variant="primary"
        size="icon"
        className={styles.cartButton}
        aria-label="Додати до кошика"
        leadingIcon={<CartIcon className={iconStyles.icon} />}
        disabled={!isInStock || isAddingToCart}
        onClick={handleAddToCart}
      />
    </article>
  );
}
