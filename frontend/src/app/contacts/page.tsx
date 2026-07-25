import type { Metadata } from "next";
import { PageLayout } from "@/shared/ui";
import styles from "./page.module.css";

export const metadata: Metadata = {
  title: "Контакти | Booktop",
  description: "Адреси, телефони та графіки роботи магазинів Booktop у Києві, Львові та Харкові. Головний офіс, відділи, соціальні мережі.",
};

export default function ContactsPage() {
  return (
    <PageLayout headerProps={{}} footerProps={{}}>
      <div className={styles.page}>
        <h1 className={styles.title}>Контакти магазинів</h1>

        {/* ===== МАГАЗИН №1 — КИЇВ, ХРЕЩАТИК ===== */}
        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Магазин №1 — Київ, вул. Хрещатик, 22</h2>
          <p className={styles.text}>
            Наш флагманський магазин у самому серці столиці. Тут представлено
            понад 10 000 найменувань — від бестселерів до рідкісних видань.
            В магазині є читальний зал та простір для проведення книжкових
            подій — презентацій, зустрічей з авторами та літературних
            вечорів.
          </p>
          <ul className={styles.list}>
            <li><strong>Адреса:</strong> м. Київ, вул. Хрещатик, 22 (вхід з боку вулиці, 2 поверх)</li>
            <li><strong>Телефон:</strong> +380 (90) 854 46 05</li>
            <li><strong>Метро:</strong> Хрещатик (3 хв пішки), Контрактова площа (8 хв пішки)</li>
            <li><strong>Графік роботи:</strong> Пн–Пт: 09:00–20:00, Сб–Нд: 10:00–19:00</li>
            <li><strong>Спеціалізація:</strong> повний асортимент, художня література, нон-фікшн, подарункові видання, читальний зал</li>
          </ul>
        </section>

        {/* ===== МАГАЗИН №2 — КИЇВ, ТРОЄЩИНА ===== */}
        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Магазин №2 — Київ, вул. Драйзера, 22</h2>
          <p className={styles.text}>
            Сімейний магазин на Троєщині з розширеним дитячим відділом та
            зоною для проведення майстер-класів та дитячих читань. Ідеальне
            місце для сімейного походу за книгами. Щосуботи тут проходять
            безкоштовні дитячі читання.
          </p>
          <ul className={styles.list}>
            <li><strong>Адреса:</strong> м. Київ, вул. Драйзера, 22, ТРЦ «Проспект», 3 поверх</li>
            <li><strong>Телефон:</strong> +380 (67) 520 89 99</li>
            <li><strong>Метро:</strong> Троєщина (5 хв пішки)</li>
            <li><strong>Графік роботи:</strong> Пн–Нд: 10:00–21:00</li>
            <li><strong>Спеціалізація:</strong> дитяча література, підліткові книги, настільні ігри, навчальна література, зона для дітей</li>
          </ul>
        </section>

        {/* ===== МАГАЗИН №3 — ЛЬВІВ ===== */}
        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Магазин №3 — Львів, просп. Свободи, 15</h2>
          <p className={styles.text}>
            Наш магазин у культурній столиці України з акцентом на українську
            та європейську літературу. Тут ви знайдете видання львівських
            та західноукраїнських видавництв, класику європейської літератури
            в українському перекладі, а також місцеві карти, путівники та
            сувенірну продукцію.
          </p>
          <ul className={styles.list}>
            <li><strong>Адреса:</strong> м. Львів, просп. Свободи, 15 (вхід з боку вул. Театральної)</li>
            <li><strong>Телефон:</strong> +380 (32) 253 44 55</li>
            <li><strong>Громадський транспорт:</strong> зупинка «Оперний театр»</li>
            <li><strong>Графік роботи:</strong> Пн–Нд: 10:00–20:00</li>
            <li><strong>Спеціалізація:</strong> українська література, європейська класика, переклади, мистецтво та архітектура, львівські видання</li>
          </ul>
        </section>

        {/* ===== МАГАЗИН №4 — ХАРКІВ ===== */}
        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Магазин №4 — Харків, вул. Сумська, 67</h2>
          <p className={styles.text}>
            Магазин для читачів східної України з широким вибором
            нон-фікшну та науково-популярної літератури. Особлива
            увага приділяється технічній, науковій та бізнес-літературі,
            а також виданням з IT та технологій.
          </p>
          <ul className={styles.list}>
            <li><strong>Адреса:</strong> м. Харків, вул. Сумська, 67, БЦ «Парус», 1 поверх</li>
            <li><strong>Телефон:</strong> +380 (57) 700 12 34</li>
            <li><strong>Метро:</strong> Історичний музей (4 хв пішки)</li>
            <li><strong>Графік роботи:</strong> Пн–Нд: 10:00–19:00</li>
            <li><strong>Спеціалізація:</strong> нон-фікшн, наука, IT та технології, бізнес, саморозвиток, технічна література</li>
          </ul>
        </section>

        {/* ===== ГОЛОВНИЙ ОФІС ===== */}
        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Головний офіс</h2>
          <p className={styles.text}>
            Головний офіс Booktop розташований у Києві. Тут працюють
            відділи логістики, закупівель, маркетингу та технічна команда.
            Для візиту в офіс, будь ласка, попередньо домовтеся про зустріч
            телефоном або електронною поштою.
          </p>
          <ul className={styles.list}>
            <li><strong>Адреса:</strong> м. Київ, вул. Хрещатик, 22, офіс 412</li>
            <li><strong>Телефон:</strong> +380 (90) 854 46 05</li>
            <li><strong>Графік роботи офісу:</strong> Пн–Пт: 09:00–18:00, Сб–Нд: вихідні</li>
          </ul>
        </section>

        {/* ===== КОНТАКТИ ЗА ВІДДІЛАМИ ===== */}
        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Контакти за відділами</h2>
          <ul className={styles.list}>
            <li><strong>Загальний email:</strong> info@booktop.ua</li>
            <li><strong>Продажі та консультації:</strong> sales@booktop.ua</li>
            <li><strong>Повернення та рекламації:</strong> returns@booktop.ua</li>
            <li><strong>Партнерство та співпраця:</strong> partners@booktop.ua</li>
            <li><strong>Прес-служба та медіа:</strong> press@booktop.ua</li>
            <li><strong>Технічна підтримка:</strong> support@booktop.ua</li>
            <li><strong>Захист даних (DPO):</strong> dpo@booktop.ua</li>
          </ul>
        </section>

        {/* ===== СОЦІАЛЬНІ МЕРЕЖІ ===== */}
        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Соціальні мережі</h2>
          <p className={styles.text}>
            Слідкуйте за новинами Booktop у соціальних мережах — там ми
            публікуємо анонси новинок, акції, рецензії, відеоогляди та
            інформацію про книжкові події.
          </p>
          <ul className={styles.list}>
            <li><strong>Instagram:</strong> @booktop.ua — новинки, рецензії, Stories з магазинів</li>
            <li><strong>Facebook:</strong> facebook.com/booktop.ua — пости, обговорення, анонси заходів</li>
            <li><strong>Telegram:</strong> @booktop_ua — канал з ексклюзивними знижками та анонсами</li>
          </ul>
        </section>

        {/* ===== КОНТАКТИ ДЛЯ ЗМІСТОВНИХ ЗАПИТІВ ===== */}
        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Загальні контакти</h2>
          <ul className={styles.list}>
            <li><strong>Телефон підтримки:</strong> +380 (90) 854 46 05 (Пн–Пт: 09:00–20:00, Сб–Нд: 10:00–19:00)</li>
            <li><strong>Другий телефон:</strong> +380 (67) 520 89 99</li>
            <li><strong>Email (загальний):</strong> info@booktop.ua</li>
          </ul>
        </section>
      </div>
    </PageLayout>
  );
}
