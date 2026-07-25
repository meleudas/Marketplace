import { useEffect, useState, type CSSProperties } from "react";
import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { fn, userEvent, within } from "storybook/test";
import { ProductCard, type ProductCardData } from "./ProductCard";

const baseProduct: ProductCardData = {
  id: "1",
  title: "Книга Відлуння старих книжок",
  author: "Барбара Девіс",
  price: 549,
  imageUrl: "/images/knyha-vidlunnia.png",
  inStock: true,
};

const stageStyle: CSSProperties = {
  display: "flex",
  alignItems: "center",
  justifyContent: "center",
  width: "100%",
  minHeight: "72vh",
  boxSizing: "border-box",
  padding: 24,
};

const cardFrameStyle: CSSProperties = { width: 280 };

declare global {
  interface Window {
    __productCardTourSetStep?: (step: ProductCardTourStep) => void;
  }
}

type ProductCardTourStep = "default" | "outOfStock" | "discount" | "hoverCard" | "hoverCart";

function productForStep(step: ProductCardTourStep): ProductCardData {
  if (step === "outOfStock") {
    return { ...baseProduct, inStock: false };
  }
  if (step === "discount" || step === "hoverCard") {
    return {
      ...baseProduct,
      price: 439,
      oldPrice: 549,
      discountPercent: 20,
    };
  }
  return baseProduct;
}

function ProductCardTour() {
  const [step, setStep] = useState<ProductCardTourStep>("default");

  useEffect(() => {
    window.__productCardTourSetStep = setStep;
    return () => {
      delete window.__productCardTourSetStep;
    };
  }, []);

  return (
    <div style={stageStyle} data-testid="product-card-tour" data-tour-step={step}>
      <div style={cardFrameStyle}>
        <ProductCard
          product={productForStep(step)}
          href="/products/book-echo"
          onAddToCart={fn()}
          isAddingToCart={false}
        />
      </div>
    </div>
  );
}

const meta = {
  title: "Shared/ProductCard",
  component: ProductCard,
  tags: ["autodocs"],
  args: {
    product: baseProduct,
    href: "/products/book-echo",
    onAddToCart: fn(),
    isAddingToCart: false,
  },
  parameters: {
    layout: "fullscreen",
  },
  decorators: [
    (Story, context) => {
      if (context.name === "Presentation Tour") {
        return <Story />;
      }
      return (
        <div style={stageStyle}>
          <div style={cardFrameStyle}>
            <Story />
          </div>
        </div>
      );
    },
  ],
} satisfies Meta<typeof ProductCard>;

export default meta;
type Story = StoryObj<typeof meta>;

/** Звичайна картка в наявності (базовий вигляд). */
export const Default: Story = {};

/** Немає в наявності — кнопка disabled. */
export const OutOfStock: Story = {
  args: {
    product: {
      ...baseProduct,
      inStock: false,
    },
  },
};

/** Зі знижкою — badge + стара/нова ціна. */
export const WithDiscount: Story = {
  args: {
    product: {
      ...baseProduct,
      price: 439,
      oldPrice: 549,
      discountPercent: 20,
    },
  },
};

/** Наведення на картку (CSS :hover на `.cardNav`). */
export const HoverOnCard: Story = {
  args: {
    product: {
      ...baseProduct,
      price: 439,
      oldPrice: 549,
      discountPercent: 20,
    },
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const nav = canvas.getByRole("link");
    await userEvent.hover(nav);
  },
};

/** Наведення на кнопку додавання в кошик. */
export const HoverOnAddToCart: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const button = canvas.getByTestId("product-card-add-to-cart");
    await userEvent.hover(button);
  },
};

/**
 * Один екран без remount між станами — для запису кліпів без блимання.
 * Керування: `window.__productCardTourSetStep('outOfStock' | ...)`
 */
export const PresentationTour: Story = {
  render: () => <ProductCardTour />,
  parameters: {
    layout: "fullscreen",
    docs: { disable: true },
  },
};
