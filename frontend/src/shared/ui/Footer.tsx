import type { ReactNode } from "react";
import Link from "next/link";
import { Container } from "./Container";
import {
  BookTopLogo,
  EmailIcon,
  FacebookIcon,
  FooterCatIllustration,
  InstagramIcon,
  PhoneIcon,
  ViberIcon,
} from "./icons";
import iconStyles from "./icons/Icon.module.css";
import styles from "./Footer.module.css";

interface FooterLink {
  label: string;
  href: string;
}

interface FooterSection {
  title: string;
  links: FooterLink[];
}

interface FooterSocialLink {
  label: string;
  href: string;
  icon: "instagram" | "facebook" | "email" | "viber";
}

interface FooterContactPhone {
  label: string;
  href: string;
}

interface FooterProps {
  homeHref?: string;
  sections?: FooterSection[];
  desktopSections?: FooterSection[];
  socialLinks?: FooterSocialLink[];
  contactPhones?: FooterContactPhone[];
}

const DEFAULT_SECTIONS: FooterSection[] = [
  {
    title: "Book Stop",
    links: [
      { label: "Проєкти", href: "#" },
      { label: "Події", href: "#" },
      { label: "Партнери", href: "#" },
      { label: "Про нас", href: "/about" },
    ],
  },
  {
    title: "Сервіси",
    links: [
      { label: "Повернення товару", href: "#" },
      { label: "Доставка і оплата", href: "#" },
      { label: "Умови", href: "#" },
      { label: "Контакти", href: "#" },
    ],
  },
  {
    title: "Пропозиції",
    links: [
      { label: "Перепродажі", href: "#" },
      { label: "Програма лояльності", href: "#" },
      { label: "Акції", href: "#" },
      { label: "Бестселери", href: "#" },
    ],
  },
];

const DEFAULT_DESKTOP_SECTIONS: FooterSection[] = [
  {
    title: "Book Top",
    links: [
      { label: "Про нас", href: "/about" },
      { label: "Блог", href: "#" },
      { label: "Партнери", href: "#" },
      { label: "Проєкти", href: "#" },
      { label: "Події в мережі книгарень", href: "#" },
      { label: "Контакти магазинів", href: "#" },
    ],
  },
  {
    title: "Сервіси",
    links: [
      { label: "Доставка і оплата", href: "#" },
      { label: "Повернення товару", href: "#" },
      { label: "Зворотний зв'язок", href: "#" },
      { label: "Умови використання сайта", href: "#" },
      { label: "Публічний договір(оферта)", href: "#" },
      { label: "Карта сайту", href: "#" },
    ],
  },
  {
    title: "Пропозиції",
    links: [
      { label: "Акції", href: "#" },
      { label: "Бестселери", href: "#" },
      { label: "Перепродажі", href: "#" },
      { label: "Електронні книги", href: "#" },
      { label: "Програма лояльності", href: "#" },
    ],
  },
];

const DEFAULT_SOCIAL_LINKS: FooterSocialLink[] = [
  { label: "Instagram", href: "#", icon: "instagram" },
  { label: "Facebook", href: "#", icon: "facebook" },
  { label: "Viber", href: "#", icon: "viber" },
  { label: "Email", href: "mailto:info@booktop.ua", icon: "email" },
];

const DEFAULT_CONTACT_PHONES: FooterContactPhone[] = [
  { label: "+380 (90) 854 46 05", href: "tel:+380908544605" },
  { label: "+380 (67) 520 89 99", href: "tel:+380675208999" },
];

const SOCIAL_ICONS = {
  instagram: InstagramIcon,
  facebook: FacebookIcon,
  email: EmailIcon,
  viber: ViberIcon,
} as const;

const ACTIVE_FOOTER_PATH = "/about";

function isActiveFooterLink(href: string): boolean {
  return href === ACTIVE_FOOTER_PATH;
}

function FooterLinkItem({ link, className }: { link: FooterLink; className: string }) {
  if (isActiveFooterLink(link.href)) {
    return (
      <Link href={link.href} className={className}>
        {link.label}
      </Link>
    );
  }

  return <span className={className}>{link.label}</span>;
}

function FooterSectionBlock({ section }: { section: FooterSection }) {
  return (
    <nav className={styles.sectionNav} aria-label={section.title}>
      <h2 className={styles.sectionTitle}>{section.title}</h2>
      <ul className={styles.list}>
        {section.links.map((link) => (
          <li key={link.label}>
            <FooterLinkItem link={link} className={styles.link} />
          </li>
        ))}
      </ul>
    </nav>
  );
}

function DesktopContactItem({ label, icon }: { label: string; icon: ReactNode }) {
  return (
    <div className={styles.desktopContactItem}>
      <span className={styles.socialLink} aria-hidden="true">
        {icon}
      </span>
      <span className={styles.desktopContactLabel}>{label}</span>
    </div>
  );
}

export function Footer({
  homeHref = "/",
  sections = DEFAULT_SECTIONS,
  desktopSections = DEFAULT_DESKTOP_SECTIONS,
  socialLinks = DEFAULT_SOCIAL_LINKS,
  contactPhones = DEFAULT_CONTACT_PHONES,
}: FooterProps) {
  const [firstSection, ...restSections] = sections;

  return (
    <footer className={styles.footer}>
      <Container className={styles.inner}>
        <div className={styles.mobileContent}>
          <div className={styles.content}>
            <Link href={homeHref} className={styles.logoLink} aria-label="BOOK TOP — на головну">
              <BookTopLogo className={styles.logo} />
            </Link>

            <div className={styles.social}>
              {socialLinks.map((item) => {
                const Icon = SOCIAL_ICONS[item.icon];
                return (
                  <span
                    key={item.label}
                    className={styles.socialLink}
                    role="img"
                    aria-label={item.label}
                  >
                    <Icon className={iconStyles.icon} />
                  </span>
                );
              })}
            </div>

            {firstSection ? (
              <div className={styles.firstSectionRow}>
                <FooterSectionBlock section={firstSection} />
                <FooterCatIllustration className={styles.cat} />
              </div>
            ) : null}

            <hr className={styles.divider} />

            {restSections.map((section, index) => (
              <div key={section.title} className={styles.section}>
                {index > 0 ? <hr className={styles.divider} /> : null}
                <FooterSectionBlock section={section} />
              </div>
            ))}
          </div>
        </div>

        <div className={styles.desktopContent}>
          <div className={styles.desktopGrid}>
            {desktopSections.map((section) => (
              <nav
                key={section.title}
                className={styles.desktopSectionNav}
                aria-label={section.title}
              >
                <h2 className={styles.desktopSectionTitle}>{section.title}</h2>
                <ul className={styles.desktopList}>
                  {section.links.map((link) => (
                    <li key={link.label}>
                      <FooterLinkItem link={link} className={styles.desktopLink} />
                    </li>
                  ))}
                </ul>
              </nav>
            ))}

            <div className={styles.desktopContacts}>
              <ul className={styles.desktopContactList}>
                {socialLinks.map((item) => {
                  const Icon = SOCIAL_ICONS[item.icon];
                  return (
                    <li key={item.label}>
                      <DesktopContactItem
                        label={item.label}
                        icon={<Icon className={iconStyles.icon} />}
                      />
                    </li>
                  );
                })}
              </ul>

              <ul className={styles.desktopPhoneList}>
                {contactPhones.map((phone) => (
                  <li key={phone.label}>
                    <DesktopContactItem
                      label={phone.label}
                      icon={<PhoneIcon className={iconStyles.icon} />}
                    />
                  </li>
                ))}
              </ul>
            </div>
          </div>
        </div>
      </Container>
    </footer>
  );
}
