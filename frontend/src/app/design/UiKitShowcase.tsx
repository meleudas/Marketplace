"use client";

import { Button, Container, Grid, ProductCard, QuantityStepper, Typography } from "@/shared/ui";
import { MOCK_PRODUCTS } from "@/shared/ui/mock";
import { InputFieldsDemo } from "./InputFieldsDemo";
import { SelectionsDemo } from "./SelectionsDemo";
import {
  ArrowsSortIcon,
  BookFlipIcon,
  CartIcon,
  CheckboxCheckedIcon,
  CheckboxIcon,
  ChevronDownIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  CloseIcon,
  EmailIcon,
  FacebookIcon,
  FilterIcon,
  InstagramIcon,
  MenuIcon,
  MicrophoneIcon,
  MinusIcon,
  OpenBookIcon,
  PhoneIcon,
  PlusIcon,
  RecordActiveIcon,
  RecordIcon,
  SearchIcon,
  StarIcon,
  TrashIcon,
  UserIcon,
  ViberIcon,
} from "@/shared/ui/icons";
import iconStyles from "@/shared/ui/icons/Icon.module.css";
import styles from "./UiKitShowcase.module.css";

const PRIMARY_COLORS = [
  { name: "Primary blue", swatchClass: "swatchPrimaryBlue", value: "#535DDF" },
  { name: "Primary pink", swatchClass: "swatchPrimaryPink", value: "#EE0290" },
  { name: "Primary light blue", swatchClass: "swatchPrimaryLightBlue", value: "#A3C2E0" },
  { name: "Primary dark", swatchClass: "swatchPrimaryDark", value: "#131417" },
  { name: "White", swatchClass: "swatchWhite", value: "#FFFFFF" },
] as const;

const SECONDARY_COLORS = [
  { name: "Success", swatchClass: "swatchSuccess", value: "#17870E" },
  { name: "Error", swatchClass: "swatchError", value: "#DC362E" },
  { name: "Secondary blue", swatchClass: "swatchSecondaryBlue", value: "#224487" },
  { name: "Secondary light blue", swatchClass: "swatchSecondaryLightBlue", value: "#7C90E0" },
] as const;

const NEUTRAL_COLORS = [
  { name: "Neutral dark", swatchClass: "swatchNeutralDark", value: "#08083D" },
  { name: "Neutral 800", swatchClass: "swatchNeutral800", value: "#2F2F2F" },
  { name: "Neutral 600", swatchClass: "swatchNeutral600", value: "#5F7081" },
  { name: "Neutral 400", swatchClass: "swatchNeutral400", value: "#80838D" },
] as const;

const SPACING_SCALE = [
  { blockClass: "spaceBlock4", value: "4px" },
  { blockClass: "spaceBlock8", value: "8px" },
  { blockClass: "spaceBlock12", value: "12px" },
  { blockClass: "spaceBlock16", value: "16px" },
  { blockClass: "spaceBlock20", value: "20px" },
  { blockClass: "spaceBlock24", value: "24px" },
  { blockClass: "spaceBlock32", value: "32px" },
  { blockClass: "spaceBlock36", value: "36px" },
] as const;

const ICON_ITEMS = [
  { name: "chevron-left", Icon: ChevronLeftIcon },
  { name: "arrows-sort", Icon: ArrowsSortIcon },
  { name: "chevron-down", Icon: ChevronDownIcon },
  { name: "chevron-right", Icon: ChevronRightIcon },
  { name: "record", Icon: RecordIcon },
  { name: "record-active", Icon: RecordActiveIcon },
  { name: "close", Icon: CloseIcon },
  { name: "filter", Icon: FilterIcon },
  { name: "cart", Icon: CartIcon },
  { name: "user", Icon: UserIcon },
  { name: "microphone", Icon: MicrophoneIcon },
  { name: "search", Icon: SearchIcon },
  { name: "menu", Icon: MenuIcon },
  { name: "instagram", Icon: InstagramIcon },
  { name: "facebook", Icon: FacebookIcon },
  { name: "email", Icon: EmailIcon },
  { name: "viber", Icon: ViberIcon },
  { name: "minus", Icon: MinusIcon },
  { name: "book-flip", Icon: BookFlipIcon },
  { name: "open-book", Icon: OpenBookIcon },
  { name: "phone", Icon: PhoneIcon },
  { name: "trash", Icon: TrashIcon },
  { name: "plus", Icon: PlusIcon },
  { name: "star", Icon: StarIcon },
  { name: "checkbox", Icon: CheckboxIcon },
  { name: "checkbox-checked", Icon: CheckboxCheckedIcon },
] as const;

function ColorSwatch({
  name,
  swatchClass,
  value,
}: {
  name: string;
  swatchClass: string;
  value: string;
}) {
  return (
    <figure className={styles.colorItem}>
      <div className={[styles.colorSwatch, styles[swatchClass]].filter(Boolean).join(" ")} aria-hidden />
      <figcaption>
        <Typography variant="body2">{name}</Typography>
        <Typography variant="body2" className={styles.muted}>
          {value}
        </Typography>
      </figcaption>
    </figure>
  );
}

export function UiKitShowcase() {
  return (
    <div className={styles.page}>
      <Container as="main" className={styles.main}>
        <header className={styles.hero}>
          <Typography variant="h1">Mobile UI Kit</Typography>
          <Typography variant="body1" className={styles.muted}>
            Превʼю токенів і базових компонентів. Тільки для перевірки дизайну.
          </Typography>
        </header>

        <section className={styles.section}>
          <Typography variant="h2" className={styles.sectionTitle}>
            Colors
          </Typography>

          <Typography variant="h3" className={styles.groupTitle}>
            Primary colors
          </Typography>
          <div className={styles.colorRow}>
            {PRIMARY_COLORS.map((color) => (
              <ColorSwatch key={color.swatchClass} {...color} />
            ))}
          </div>

          <Typography variant="h3" className={styles.groupTitle}>
            Secondary colors
          </Typography>
          <div className={styles.colorRow}>
            {SECONDARY_COLORS.map((color) => (
              <ColorSwatch key={color.swatchClass} {...color} />
            ))}
          </div>

          <Typography variant="h3" className={styles.groupTitle}>
            Neutral colors
          </Typography>
          <div className={styles.colorRow}>
            {NEUTRAL_COLORS.map((color) => (
              <ColorSwatch key={color.swatchClass} {...color} />
            ))}
          </div>
        </section>

        <section className={styles.section}>
          <Typography variant="h2" className={styles.sectionTitle}>
            Typography
          </Typography>

          <div className={styles.fontSamples}>
            <div className={styles.fontSample}>
              <Typography variant="body2" className={styles.muted}>
                Montserrat — headings
              </Typography>
              <Typography variant="h1" as="p">
                Aa Бб 123
              </Typography>
            </div>
            <div className={styles.fontSample}>
              <Typography variant="body2" className={styles.muted}>
                Inter — body
              </Typography>
              <Typography variant="body1" as="p">
                Aa Бб 123
              </Typography>
            </div>
          </div>

          <div className={styles.typographyStack}>
            <Typography variant="h1">Heading 1 — Semibold 24 / 21</Typography>
            <Typography variant="h2">Heading 2 — Medium 20 / 21</Typography>
            <Typography variant="h3">Heading 3 — Regular 20 / 21</Typography>
            <Typography variant="body1">Body 1 — Regular 16 / 16</Typography>
            <Typography variant="body2">Body 2 — Regular 12 / 16</Typography>
            <Typography variant="body3">Body 3 — Medium 16 / 16</Typography>
            <Typography variant="body4">Body 4 — Regular 16 / 16</Typography>
          </div>
        </section>

        <section className={styles.section}>
          <Typography variant="h2" className={styles.sectionTitle}>
            Spacing
          </Typography>
          <div className={styles.spacingRow}>
            {SPACING_SCALE.map((item) => (
              <div key={item.blockClass} className={styles.spacingItem}>
                <div
                  className={[styles.spacingBlock, styles[item.blockClass]].join(" ")}
                  aria-hidden
                />
                <Typography variant="body2">{item.value}</Typography>
              </div>
            ))}
          </div>
        </section>

        <section className={styles.section}>
          <Typography variant="h2" className={styles.sectionTitle}>
            Grid system
          </Typography>
          <Typography variant="body2" className={styles.muted}>
            Mobile-first · 5 columns · gap 20px · padding 20px
          </Typography>
          <div className={styles.gridDemo}>
            <Grid layout="columns" gap="grid">
              {Array.from({ length: 5 }).map((_, index) => (
                <div key={index} className={styles.gridColumn} aria-hidden />
              ))}
            </Grid>
          </div>
        </section>

        <section className={styles.section}>
          <Typography variant="h2" className={styles.sectionTitle}>
            Border radius
          </Typography>
          <div className={styles.radiusDemo}>
            <div className={styles.radiusBox} aria-hidden />
            <Typography variant="body2">8px</Typography>
          </div>
        </section>

        <section className={styles.section}>
          <Typography variant="h2" className={styles.sectionTitle}>
            Icons
          </Typography>
          <div className={styles.iconGrid}>
            {ICON_ITEMS.map(({ name, Icon }) => (
              <div key={name} className={styles.iconItem}>
                <Icon className={iconStyles.icon} />
                <Typography variant="body2">{name}</Typography>
              </div>
            ))}
          </div>
        </section>

        <section className={styles.section}>
          <Typography variant="h2" className={styles.sectionTitle}>
            Input fields
          </Typography>
          <Typography variant="body2" className={styles.muted}>
            Наведи, сфокусуй або введи некоректні дані — стани працюють через CSS та zod + react-hook-form.
          </Typography>
          <InputFieldsDemo />
        </section>

        <section className={styles.section}>
          <Typography variant="h2" className={styles.sectionTitle}>
            Selections
          </Typography>
          <Typography variant="body2" className={styles.muted}>
            Checkbox та Radio Button на існуючих іконках — клікни, щоб перемкнути стан.
          </Typography>
          <SelectionsDemo />
        </section>

        <section className={styles.section}>
          <Typography variant="h2" className={styles.sectionTitle}>
            Card
          </Typography>
          <div className={styles.cardDemo}>
            <ProductCard product={MOCK_PRODUCTS[0]} />
          </div>
        </section>

        <section className={styles.section}>
          <Typography variant="h2" className={styles.sectionTitle}>
            Buttons
          </Typography>
          <div className={styles.buttonShowcase}>
            <Button variant="primary" size="sm">
              Застосувати
            </Button>

            <Button variant="primary" size="sm" leadingIcon={<CartIcon />}>
              До кошика
            </Button>

            <Button variant="secondary" size="lg" fullWidth>
              Надіслати відгук
            </Button>

            <QuantityStepper value={1} />

            <Button variant="gradient" size="sm">
              Отримати знижку
            </Button>

            <Button variant="primary" size="lg" fullWidth>
              Увійти або зареєструватися
            </Button>

            <Button variant="secondary" size="sm" leadingIcon={<OpenBookIcon />}>
              950грн
            </Button>

            <Button variant="dark" size="sm" leadingIcon={<PhoneIcon />}>
              400грн
            </Button>

            <Button variant="primary" size="icon" leadingIcon={<CartIcon />} aria-label="До кошика" />
          </div>
        </section>
      </Container>
    </div>
  );
}
