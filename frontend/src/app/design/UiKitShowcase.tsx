"use client";

import {
  Button,
  Container,
  Footer,
  Grid,
  Header,
  IconButton,
  Pagination,
  ProductCard,
  TextField,
  Typography,
} from "@/shared/ui";
import { MOCK_PRODUCTS } from "@/shared/ui/mock";
import styles from "./UiKitShowcase.module.css";

export function UiKitShowcase() {
  return (
    <div className={styles.page}>
      <Header />

      <main>
        <Container as="section" className={styles.section}>
          <header className={styles.sectionHead}>
            <Typography variant="h1">UI Skeleton</Typography>
            <Typography variant="body1" className={styles.subtext}>
              Презентаційні компоненти для нового дизайну. Без бізнес-логіки, лише
              верстка та токени.
            </Typography>
          </header>
        </Container>

        <Container as="section" className={styles.section}>
          <Typography variant="h2" className={styles.blockTitle}>
            Typography
          </Typography>
          <div className={styles.typographyStack}>
            <Typography variant="h1">Heading 1</Typography>
            <Typography variant="h2">Heading 2</Typography>
            <Typography variant="h3">Heading 3</Typography>
            <Typography variant="body1">Body 1 — основний текст</Typography>
            <Typography variant="body2">Body 2 — дрібний текст</Typography>
            <Typography variant="body3">Body 3 — напівжирний текст</Typography>
            <Typography variant="body4">Body 4 — звичайний текст</Typography>
          </div>
        </Container>

        <Container as="section" className={styles.section}>
          <Typography variant="h2" className={styles.blockTitle}>
            Buttons
          </Typography>
          <div className={styles.row}>
            <Button variant="primary">Primary</Button>
            <Button variant="secondary">Secondary</Button>
            <Button variant="ghost">Ghost</Button>
            <Button variant="danger">Danger</Button>
            <Button variant="primary" disabled>
              Disabled
            </Button>
          </div>
          <div className={styles.row}>
            <Button size="sm">Small</Button>
            <Button size="md">Medium</Button>
            <Button size="lg">Large</Button>
          </div>
        </Container>

        <Container as="section" className={styles.section}>
          <Typography variant="h2" className={styles.blockTitle}>Icon buttons</Typography>
          <div className={styles.row}>
            <IconButton label="Обране" icon={<span>♡</span>} />
            <IconButton label="Кошик" icon={<span>🛒</span>} variant="solid" />
            <IconButton label="Пошук" icon={<span>⌕</span>} variant="solid" size="lg" />
          </div>
        </Container>

        <Container as="section" className={styles.section}>
          <Typography variant="h2" className={styles.blockTitle}>Inputs / TextField</Typography>
          <div className={styles.formGrid}>
            <TextField label="Email" name="email" placeholder="you@example.com" />
            <TextField
              label="Пошук"
              name="search"
              placeholder="Знайти товар"
              leadingIcon={<span>⌕</span>}
            />
            <TextField label="Хінт" name="hinted" placeholder="З підказкою" hint="Допоміжний текст" />
            <TextField
              label="Помилка"
              name="errored"
              placeholder="Некоректне значення"
              error="Поле обовʼязкове"
            />
          </div>
        </Container>

        <Container as="section" className={styles.section}>
          <Typography variant="h2" className={styles.blockTitle}>Product grid</Typography>
          <Grid minColumnWidth="15rem">
            {MOCK_PRODUCTS.map((product) => (
              <ProductCard key={product.id} product={product} />
            ))}
          </Grid>
        </Container>

        <Container as="section" className={styles.section}>
          <Typography variant="h2" className={styles.blockTitle}>Pagination</Typography>
          <Pagination currentPage={3} totalPages={12} />
        </Container>
      </main>

      <Footer />
    </div>
  );
}
