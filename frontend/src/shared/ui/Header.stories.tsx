import { useEffect, useState, type CSSProperties } from "react";
import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { userEvent, within } from "storybook/test";
import { Container } from "./Container";
import { Spinner } from "./Spinner";
import { TextField } from "./TextField";
import {
  BookTopLogo,
  CartIcon,
  ChevronRightIcon,
  MicrophoneIcon,
  SearchIcon,
  UserIcon,
} from "./icons";
import iconStyles from "./icons/Icon.module.css";
import styles from "./Header.module.css";

type HeaderSearchTourStep =
  | "idle"
  | "hoverField"
  | "focus"
  | "typing"
  | "loading"
  | "results"
  | "hoverResult"
  | "selectResult";

interface MockSearchItem {
  id: string;
  slug: string;
  title: string;
  price: number;
}

const MOCK_RESULTS: MockSearchItem[] = [
  {
    id: "1",
    slug: "romany-lisova-pisnia-paper",
    title: "Романи: Лісова пісня (Паперова)",
    price: 320,
  },
  {
    id: "2",
    slug: "romany-lisova-pisnia-paper-folio",
    title: "Романи: Лісова пісня (Паперова)",
    price: 345,
  },
  {
    id: "3",
    slug: "romany-tini-zabutykh-predkiv-electronic",
    title: "Романи: Тіні забутих предків (Електронна)",
    price: 169,
  },
  {
    id: "4",
    slug: "klasyka-intermezzo-paper",
    title: "Класика: Intermezzo (Паперова)",
    price: 280,
  },
];

declare global {
  interface Window {
    __headerSearchTourSetStep?: (step: HeaderSearchTourStep) => void;
  }
}

const stageStyle: CSSProperties = {
  width: "100%",
  minHeight: "72vh",
  boxSizing: "border-box",
  padding: "48px 24px 80px",
  background: "#131417",
};

function queryForStep(step: HeaderSearchTourStep): string {
  if (step === "typing") return "Лісо";
  if (
    step === "loading" ||
    step === "results" ||
    step === "hoverResult" ||
    step === "selectResult"
  ) {
    return "Лісова пісня";
  }
  return "";
}

function HeaderSearchDemo({ step }: { step: HeaderSearchTourStep }) {
  const query = queryForStep(step);
  const showDropdown =
    step === "typing" ||
    step === "loading" ||
    step === "results" ||
    step === "hoverResult" ||
    step === "selectResult";
  const isLoading = step === "loading" || step === "typing";
  const showResults =
    step === "results" || step === "hoverResult" || step === "selectResult";
  const forceFieldHover = step === "hoverField";
  const fieldFocused =
    step === "focus" ||
    step === "typing" ||
    step === "loading" ||
    step === "results" ||
    step === "hoverResult" ||
    step === "selectResult";

  return (
    <header
      className={styles.header}
      data-search-dropdown-open={showDropdown ? "true" : "false"}
      data-testid="header-search-demo"
    >
      <Container className={styles.inner}>
        <div className={styles.topRow}>
          <div className={styles.brandGroup}>
            <a href="/" className={styles.logo} aria-label="BOOK TOP — на головну">
              <BookTopLogo className={styles.logoImage} />
            </a>
            <nav className={styles.nav} aria-label="Головна навігація">
              <button type="button" className={styles.navLink}>
                Каталог
              </button>
            </nav>
          </div>

          <div className={styles.searchWrap} data-testid="header-search-wrap">
            <TextField
              kind="search"
              className={`${styles.search} ${styles.primarySearch}`}
              value={query}
              placeholder="Пошук"
              aria-label="Пошук"
              aria-expanded={showDropdown}
              aria-controls="header-search-dropdown"
              forceHover={forceFieldHover}
              autoFocus={fieldFocused && step === "focus"}
              readOnly
              leadingIcon={<SearchIcon className={iconStyles.icon} />}
              trailingIcon={<MicrophoneIcon className={iconStyles.icon} />}
            />

            {showDropdown ? (
              <div
                id="header-search-dropdown"
                className={styles.searchDropdown}
                role="listbox"
                aria-label="Результати пошуку"
                data-testid="header-search-dropdown"
              >
                <section className={styles.previewCard} aria-label="Підбірка для вас">
                  <div className={styles.previewHeader}>
                    <span className={styles.previewTitle}>Для вас</span>
                    <ChevronRightIcon className={iconStyles.icon} />
                  </div>

                  {isLoading ? (
                    <div className={styles.loadingState} role="status" aria-live="polite">
                      <Spinner aria-hidden="true" />
                      <span className={styles.loadingText}>Завантаження...</span>
                    </div>
                  ) : showResults ? (
                    <ul className={styles.previewList}>
                      {MOCK_RESULTS.map((item, index) => {
                        const isHover = step === "hoverResult" && index === 0;
                        const isSelected = step === "selectResult" && index === 0;
                        return (
                          <li key={item.id}>
                            <a
                              href={`/products/${item.slug}`}
                              className={[
                                styles.previewItem,
                                isHover ? styles.previewItemForceHover : "",
                                isSelected ? styles.previewItemSelected : "",
                              ]
                                .filter(Boolean)
                                .join(" ")}
                              data-testid={
                                index === 0 ? "header-search-result-primary" : undefined
                              }
                            >
                              <span className={styles.previewName}>{item.title}</span>
                              <span className={styles.previewPrice}>
                                {item.price.toLocaleString("uk-UA")} грн
                              </span>
                            </a>
                          </li>
                        );
                      })}
                    </ul>
                  ) : (
                    <p className={styles.emptyState}>Нічого не знайдено</p>
                  )}
                </section>
              </div>
            ) : null}
          </div>

          <div className={styles.actions}>
            <a href="/me" className={`${styles.iconAction} ${styles.profileLink}`} aria-label="Профіль">
              <UserIcon className={iconStyles.icon} />
            </a>
            <a href="/cart" className={`${styles.iconAction} ${styles.cartIconWrap}`} aria-label="Кошик">
              <CartIcon className={iconStyles.icon} />
            </a>
          </div>
        </div>
      </Container>
    </header>
  );
}

function HeaderSearchTour() {
  const [step, setStep] = useState<HeaderSearchTourStep>("idle");

  useEffect(() => {
    window.__headerSearchTourSetStep = setStep;
    return () => {
      delete window.__headerSearchTourSetStep;
    };
  }, []);

  return (
    <div style={stageStyle} data-testid="header-search-tour" data-tour-step={step}>
      <HeaderSearchDemo step={step} />
    </div>
  );
}

const meta = {
  title: "Shared/HeaderSearch",
  tags: ["autodocs"],
  parameters: {
    layout: "fullscreen",
    nextjs: {
      appDirectory: true,
      navigation: {
        pathname: "/",
      },
    },
  },
  decorators: [
    (Story, context) => {
      if (context.name === "Presentation Tour") {
        return <Story />;
      }
      return (
        <div style={stageStyle}>
          <Story />
        </div>
      );
    },
  ],
} satisfies Meta;

export default meta;
type Story = StoryObj<typeof meta>;

/** Порожній пошук у хедері. */
export const Idle: Story = {
  render: () => <HeaderSearchDemo step="idle" />,
};

/** Наведення на поле пошуку. */
export const HoverField: Story = {
  render: () => <HeaderSearchDemo step="hoverField" />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.hover(canvas.getByLabelText("Пошук"));
  },
};

/** Клік / фокус у полі пошуку. */
export const Focused: Story = {
  render: () => <HeaderSearchDemo step="focus" />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(canvas.getByLabelText("Пошук"));
  },
};

/** Введення запиту — стан завантаження. */
export const TypingAndLoading: Story = {
  render: () => <HeaderSearchDemo step="loading" />,
};

/** Dropdown з результатами. */
export const WithResults: Story = {
  render: () => <HeaderSearchDemo step="results" />,
};

/** Наведення на перший результат. */
export const HoverResult: Story = {
  render: () => <HeaderSearchDemo step="hoverResult" />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.hover(canvas.getByTestId("header-search-result-primary"));
  },
};

/** Вибір результату кліком. */
export const SelectResult: Story = {
  render: () => <HeaderSearchDemo step="selectResult" />,
};

/**
 * Один екран без remount між станами — для запису кліпів без блимання.
 * Керування: `window.__headerSearchTourSetStep('hoverField' | ...)`
 */
export const PresentationTour: Story = {
  render: () => <HeaderSearchTour />,
  parameters: {
    layout: "fullscreen",
    docs: { disable: true },
  },
};
