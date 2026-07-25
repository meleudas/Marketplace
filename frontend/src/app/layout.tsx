import type { Metadata } from "next";
import "@fontsource/inter/400.css";
import "@fontsource/inter/500.css";
import "@fontsource/inter/600.css";
import "@fontsource/montserrat/400.css";
import "@fontsource/montserrat/500.css";
import "@fontsource/montserrat/600.css";
import "./globals.css";
import styles from "./layout.module.css";

const siteUrl = "https://booktop.ua";

export const metadata: Metadata = {
  metadataBase: new URL(siteUrl),
  title: {
    default: "Booktop — купуй та продавай книги легко",
    template: "%s | Booktop",
  },
  description:
    "Booktop — найбільший книжковий маркетплейс України. Купуй нові та вживані книги, продавай власні, знаходь улюблених авторів. Швидка доставка по всій Україні.",
  keywords: [
    "книги",
    "книжковий маркетплейс",
    "купити книги онлайн",
    "книжковий магазин Україна",
    "букіністика",
    "українські книги",
    "книги з доставкою",
    "продаж книг",
    "книжкова платформа",
    "нові книги",
    "вживані книги",
    "Booktop",
  ],
  authors: [{ name: "Booktop" }],
  creator: "Booktop",
  publisher: "Booktop",
  icons: {
    icon: "/icon.png",
    shortcut: "/favicon.png",
    apple: "/icon.png",
  },
  openGraph: {
    title: "Booktop — купуй та продавай книги легко",
    description:
      "Найбільший книжковий маркетплейс України. Тисячі книг за вигідними цінами, швидка доставка у будь-яке місто. Приєднуйся до спільноти книголюбів!",
    type: "website",
    locale: "uk_UA",
    url: siteUrl,
    siteName: "Booktop",
    countryName: "Україна",
    images: [
      {
        url: "/images/og-image.svg",
        width: 1200,
        height: 630,
        alt: "Booktop — книжковий маркетплейс України",
      },
    ],
  },
  twitter: {
    card: "summary_large_image",
    title: "Booktop — купуй та продавай книги легко",
    description:
      "Найбільший книжковий маркетплейс України. Тисячі книг за вигідними цінами, швидка доставка у будь-яке місто.",
    images: ["/images/og-image.svg"],
    creator: "@booktop_ua",
  },
  robots: {
    index: true,
    follow: true,
    googleBot: {
      index: true,
      follow: true,
      "max-video-preview": -1,
      "max-image-preview": "large",
      "max-snippet": -1,
    },
  },
  alternates: {
    canonical: siteUrl,
    languages: {
      "uk-UA": siteUrl,
    },
  },
  category: "books",
  classification: "Книжковий маркетплейс",
  verification: {
    // Add your Google Search Console verification code here
    // google: "your-verification-code",
  },
  other: {
    "format-detection": "telephone=no",
    "apple-mobile-web-app-capable": "yes",
    "apple-mobile-web-app-status-bar-style": "black-translucent",
    "apple-mobile-web-app-title": "Booktop",
  },
};

const jsonLd = {
  "@context": "https://schema.org",
  "@type": "WebSite",
  name: "Booktop",
  alternateName: "Буктоп",
  url: siteUrl,
  description:
    "Booktop — книжковий маркетплейс України. Купуйте та продавайте книги онлайн з доставкою по всій Україні.",
  inLanguage: "uk",
  author: {
    "@type": "Organization",
    name: "Booktop",
    url: siteUrl,
    logo: `${siteUrl}/logo.svg`,
  },
  potentialAction: {
    "@type": "SearchAction",
    target: {
      "@type": "EntryPoint",
      urlTemplate: `${siteUrl}/search?q={search_term_string}`,
    },
    "query-input": "required name=search_term_string",
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="uk" className={styles.html}>
      <head>
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
        />
      </head>
      <body className={styles.body}>
        {children}
      </body>
    </html>
  );
}
