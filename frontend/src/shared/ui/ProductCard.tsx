import { Button } from "./Button";
import { CartIcon } from "./icons";
import iconStyles from "./icons/Icon.module.css";
import styles from "./ProductCard.module.css";

export interface ProductCardData {
  id: string;
  title: string;
  author?: string;
  price: number;
  imageUrl?: string;
  inStock?: boolean;
}

interface ProductCardProps {
  product: ProductCardData;
  onAddToCart?: (productId: string) => void;
}

const formatPrice = (value: number): string =>
  `${new Intl.NumberFormat("uk-UA", { maximumFractionDigits: 0 }).format(value)} грн`;

export function ProductCard({ product, onAddToCart }: ProductCardProps) {
  const isInStock = product.inStock ?? true;

  return (
    <article className={styles.card}>
      <div className={styles.media}>
        <div className={styles.imagePlaceholder} aria-hidden="true" />
        {product.imageUrl ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img className={styles.image} src={product.imageUrl} alt={product.title} />
        ) : null}
      </div>

      <div className={styles.body}>
        {product.author ? <p className={styles.author}>{product.author}</p> : null}

        <h3 className={styles.title}>{product.title}</h3>

        <p className={isInStock ? styles.availability : styles.availabilityOut}>
          {isInStock ? "В наявності" : "Немає в наявності"}
        </p>

        <div className={styles.footer}>
          <span className={styles.price}>{formatPrice(product.price)}</span>
          <Button
            variant="primary"
            size="icon"
            aria-label="Додати до кошика"
            leadingIcon={<CartIcon className={iconStyles.icon} />}
            disabled={!isInStock}
            onClick={() => onAddToCart?.(product.id)}
          />
        </div>
      </div>
    </article>
  );
}
