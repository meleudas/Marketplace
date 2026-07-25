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
      { label: "Проєкти", href: "/projects" },
      { label: "Події", href: "/events" },
      { label: "Партнери", href: "/partners" },
      { label: "Про нас", href: "/about" },
    ],
  },
  {
    title: "Сервіси",
    links: [
      { label: "Доставка і оплата", href: "/delivery-payment" },
      { label: "Повернення товару", href: "/returns" },
      { label: "Зворотний зв'язок", href: "/feedback" },
      { label: "Умови використання сайта", href: "/terms" },
      { label: "Публічний договір(оферта)", href: "/offer" },
    ],
  },
  {
    title: "Пропозиції",
    links: [
      { label: "Перепродажі", href: "/resales" },
      { label: "Програма лояльності", href: "/loyalty" },
      { label: "Акції", href: "/sales" },
      { label: "Бестселери", href: "/bestsellers" },
    ],
  },
];

const DEFAULT_DESKTOP_SECTIONS: FooterSection[] = [
  {
    title: "Book Top",
    links: [
      { label: "Про нас", href: "/about" },
      { label: "Блог", href: "/blog" },
      { label: "Партнери", href: "/partners" },
      { label: "Проєкти", href: "/projects" },
      { label: "Події в мережі книгарень", href: "/events" },
      { label: "Контакти магазинів", href: "/contacts" },
    ],
  },
  {
    title: "Сервіси",
    links: [
      { label: "Доставка і оплата", href: "/delivery-payment" },
      { label: "Повернення товару", href: "/returns" },
      { label: "Зворотний зв'язок", href: "/feedback" },
      { label: "Умови використання сайта", href: "/terms" },
      { label: "Публічний договір(оферта)", href: "/offer" },
      { label: "Карта сайту", href: "/sitemap" },
    ],
  },
  {
    title: "Пропозиції",
    links: [
      { label: "Акції", href: "/sales" },
      { label: "Бестселери", href: "/bestsellers" },
      { label: "Перепродажі", href: "/resales" },
      { label: "Електронні книги", href: "/catalog?format=електронний" },
      { label: "Програма лояльності", href: "/loyalty" },
    ],
  },
];

const DEFAULT_SOCIAL_LINKS: FooterSocialLink[] = [
  { label: "Instagram", href: "https://instagram.com/booktop.ua", icon: "instagram" },
  { label: "Facebook", href: "https://facebook.com/booktop.ua", icon: "facebook" },
  { label: "Viber", href: "https://viber.com/booktop.ua", icon: "viber" },
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

// All footer links with real hrefs render as <Link>; only "#" stays as <span>.

function FooterLinkItem({ link, className }: { link: FooterLink; className: string }) {
  if (link.href === "#" || link.href === "") {
    return <span className={className}>{link.label}</span>;
  }

  return (
    <Link href={link.href} className={className}>
      {link.label}
    </Link>
  );
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

function DesktopContactItem({ label, icon, href }: { label: string; icon: ReactNode; href?: string }) {
  const content = (
    <>
      <span className={styles.socialLink} aria-hidden="true">
        {icon}
      </span>
      <span className={styles.desktopContactLabel}>{label}</span>
    </>
  );

  if (href) {
    return (
      <a href={href} className={styles.desktopContactItem} target="_blank" rel="noopener noreferrer">
        {content}
      </a>
    );
  }

  return (
    <div className={styles.desktopContactItem}>
      {content}
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
                  <a
                    key={item.label}
                    href={item.href}
                    className={styles.socialLink}
                    target="_blank"
                    rel="noopener noreferrer"
                    aria-label={item.label}
                  >
                    <Icon className={iconStyles.icon} />
                  </a>
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
          <FooterCatIllustration className={styles.desktopCat} />
          <div className={styles.desktopTopRow}>
            <Link href={homeHref} className={styles.logoLink} aria-label="BOOK TOP — на головну">
              <BookTopLogo className={styles.logo} />
            </Link>

            <div className={styles.social}>
              {socialLinks.map((item) => {
                const Icon = SOCIAL_ICONS[item.icon];
                return (
                  <a
                    key={item.label}
                    href={item.href}
                    className={styles.socialLink}
                    target="_blank"
                    rel="noopener noreferrer"
                    aria-label={item.label}
                  >
                    <Icon className={iconStyles.icon} />
                  </a>
                );
              })}
            </div>
          </div>

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
              <ul className={styles.desktopPhoneList}>
                {contactPhones.map((phone) => (
                  <li key={phone.label}>
                    <DesktopContactItem
                      label={phone.label}
                      href={phone.href}
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
