import Link from "next/link";
import { Container } from "./Container";
import {
  BookTopLogo,
  EmailIcon,
  FacebookIcon,
  FooterCatIllustration,
  InstagramIcon,
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

interface FooterProps {
  homeHref?: string;
  sections?: FooterSection[];
  socialLinks?: FooterSocialLink[];
}

const DEFAULT_SECTIONS: FooterSection[] = [
  {
    title: "Book Stop",
    links: [
      { label: "Проєкти", href: "#" },
      { label: "Події", href: "#" },
      { label: "Партнери", href: "#" },
      { label: "Про нас", href: "#" },
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

const DEFAULT_SOCIAL_LINKS: FooterSocialLink[] = [
  { label: "Instagram", href: "#", icon: "instagram" },
  { label: "Facebook", href: "#", icon: "facebook" },
  { label: "Email", href: "mailto:info@booktop.ua", icon: "email" },
  { label: "Viber", href: "#", icon: "viber" },
];

const SOCIAL_ICONS = {
  instagram: InstagramIcon,
  facebook: FacebookIcon,
  email: EmailIcon,
  viber: ViberIcon,
} as const;

function FooterSectionBlock({ section }: { section: FooterSection }) {
  return (
    <nav className={styles.sectionNav} aria-label={section.title}>
      <h2 className={styles.sectionTitle}>{section.title}</h2>
      <ul className={styles.list}>
        {section.links.map((link) => (
          <li key={link.label}>
            <a href={link.href} className={styles.link}>
              {link.label}
            </a>
          </li>
        ))}
      </ul>
    </nav>
  );
}

export function Footer({
  homeHref = "/",
  sections = DEFAULT_SECTIONS,
  socialLinks = DEFAULT_SOCIAL_LINKS,
}: FooterProps) {
  const [firstSection, ...restSections] = sections;

  return (
    <footer className={styles.footer}>
      <Container className={styles.inner}>
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
      </Container>
    </footer>
  );
}
