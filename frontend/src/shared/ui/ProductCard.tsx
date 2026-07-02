import styles from "./ProductCard.module.css";
import { Button } from "./Button";
import { CartIcon } from "./icons";
import { IconButton } from "./IconButton";

export interface ProductCardData {
  id: string;
  title: string;
  category?: string;
  price: number;
  oldPrice?: number;
  rating?: number;
  imageUrl?: string;
  badge?: string;
}

interface ProductCardProps {
  product: ProductCardData;
}

const formatPrice = (value: number): string =>
  `${new Intl.NumberFormat("uk-UA", { maximumFractionDigits: 0 }).format(value)} грн`;

export function ProductCard({ product }: ProductCardProps) {
  return (
    <article className={styles.card}>
      <div className={styles.media}>
        {product.imageUrl ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img className={styles.image} src={product.imageUrl} alt={product.title} />
        ) : (
          <div className={styles.imagePlaceholder} aria-hidden="true" />
        )}

        {product.badge ? <span className={styles.badge}>{product.badge}</span> : null}

        <IconButton
          className={styles.favorite}
          label="Додати в обране"
          variant="solid"
          size="sm"
          icon={<span>♡</span>}
        />
      </div>

      <div className={styles.body}>
        {product.category ? <span className={styles.category}>{product.category}</span> : null}
        <h3 className={styles.title}>{product.title}</h3>

        {typeof product.rating === "number" ? (
          <div className={styles.rating} aria-label={`Рейтинг ${product.rating}`}>
            <span aria-hidden="true">★</span>
            <span>{product.rating.toFixed(1)}</span>
          </div>
        ) : null}

        <div className={styles.priceRow}>
          <span className={styles.price}>{formatPrice(product.price)}</span>
          {typeof product.oldPrice === "number" ? (
            <span className={styles.oldPrice}>{formatPrice(product.oldPrice)}</span>
          ) : null}
        </div>

        <Button variant="primary" size="sm" fullWidth leadingIcon={<CartIcon />}>
          До кошика
        </Button>
      </div>
    </article>
  );
}
