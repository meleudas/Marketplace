import type { Metadata } from "next";
import { PageLayout } from "@/shared/ui";
import styles from "./page.module.css";

export const metadata: Metadata = {
  title: "Зворотний зв'язок | Booktop",
  description: "Зв'яжіться зі службою підтримки Booktop — телефон, email, відділи, соціальні мережі. Швидка відповідь протягом 24 годин.",
};

export default function FeedbackPage() {
  return (
    <PageLayout headerProps={{}} footerProps={{}}>
      <div className={styles.page}>
        <h1 className={styles.title}>Зворотний зв&apos;язок</h1>

        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Як зв&apos;язатися з нами</h2>
          <p className={styles.text}>
            Ми завжди раді почути вас. Якщо у вас виникли питання щодо замовлення,
            пропозиції щодо співпраці, зауваження про роботу сайту або будь-які
            інші запитання — оберіть зручний для вас спосіб зв&apos;язку нижче.
            Наша команда працює <strong>щодня з 09:00 до 20:00</strong> (без вихідних).
          </p>
        </section>

        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Телефони підтримки</h2>
          <ul className={styles.list}>
            <li>
              <strong>Головний номер:</strong> +380 (90) 854 46 05 — загальні
              питання, консультації, допомога з замовленнями.
            </li>
            <li>
              <strong>Другий номер:</strong> +380 (67) 520 89 99 — альтернативний
              номер для зв&apos;язку (доступний у ті самі години).
            </li>
            <li>
              <strong>Години роботи:</strong> Пн–Пт: 09:00–20:00, Сб–Нд: 10:00–19:00.
            </li>
            <li>
              Середній час відповіді по телефону: <strong>до 2 хвилин</strong> у
              робочий час.
            </li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Електронна пошта</h2>
          <ul className={styles.list}>
            <li>
              <strong>Загальні питання:</strong> info@booktop.ua — для будь-яких
              запитань, які не підпадають під вказані нижче категорії.
            </li>
            <li>
              <strong>Замовлення та продажі:</strong> sales@booktop.ua — питання
              щодо наявності товарів, цін, оформлення замовлень та статусу
              відправлення.
            </li>
            <li>
              <strong>Повернення та reklamacії:</strong> returns@booktop.ua —
              повернення, обмін, рекламації, пошкоджені або неякісні товари.
            </li>
            <li>
              <strong>Партнерство та співпраця:</strong> partners@booktop.ua —
              питання для видавництв, авторів, компаній щодо співпраці
              та розміщення товарів на платформі.
            </li>
            <li>
              <strong>Прес-служба та медіа:</strong> press@booktop.ua — запити
              від журналістів, запрошення на заходи, отримання прес-матеріалів.
            </li>
            <li>
              <strong>Технічна підтримка:</strong> support@booktop.ua — проблеми
              з входом на сайт, оформленням замовлення, роботою особистого
              кабінету або мобільного додатку.
            </li>
          </ul>
          <p className={styles.text}>
            Середній час відповіді на електронну пошту: <strong>до 24 годин</strong>
            у робочі дні. У вихідні та святкові дні — до 36 годин.
          </p>
        </section>

        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Контакти за відділами</h2>
          <ul className={styles.list}>
            <li>
              <strong>Відділ продажів:</strong> sales@booktop.ua, +380 (90) 854 46 05,
              доб. 101 — консультації з вибору книг, допомога з оформленням
              замовлень, питання про ціни та наявність.
            </li>
            <li>
              <strong>Відділ повернень та рекламацій:</strong> returns@booktop.ua,
              +380 (90) 854 46 05, доб. 102 — оформлення повернень, обмін товарів,
              вирішення спірних питань з якістю.
            </li>
            <li>
              <strong>Відділ партнерства:</strong> partners@booktop.ua — співпраця
              з видавництвами, розміщення товарів, оптові закупівлі, корпоративні
              замовлення.
            </li>
            <li>
              <strong>Прес-служба:</strong> press@booktop.ua — медіа-запити,
              організація заходів, отримання інформації для публікацій.
            </li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Соціальні мережі</h2>
          <p className={styles.text}>
            Ми активно ведемо наші сторінки у соціальних мережах. Там ви можете
            дізнатися про новинки, акції, книжкові події та отримати швидку
            відповідь у приватних повідомленнях.
          </p>
          <ul className={styles.list}>
            <li>
              <strong>Instagram:</strong> @booktop.ua — новинки, рецензії, акції,
              анонси заходів. Час відповіді у DM: <strong>до 2 годин</strong> у
              робочий час.
            </li>
            <li>
              <strong>Facebook:</strong> facebook.com/booktop.ua — пости, обговорення,
              анонси. Час відповіді у повідомленнях: <strong>до 4 годин</strong>.
            </li>
            <li>
              <strong>Telegram:</strong> @booktop_ua — канал з новинками та
              ексклюзивними знижками для підписників. Підписуйтесь, щоб
              першими дізнаватися про акції.
            </li>
            <li>
              <strong>Viber-бот:</strong> швидкий доступ до відстеження
              замовлень та відповіді на поширені питання.
            </li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Часті питання (FAQ)</h2>

          <p className={styles.text}>
            <strong>Як дізнатися статус мого замовлення?</strong>
          </p>
          <p className={styles.text}>
            Увійдіть до особистого кабінету на сторінці <strong>Мої замовлення</strong>.
            Там ви побачите поточний статус, номер відправлення та орієнтовну
            дату отримання. Також ми надсилаємо сповіщення електронною поштою
            та SMS на кожному етапі.
          </p>

          <p className={styles.text}>
            <strong>Чи можна змінити або скасувати замовлення?</strong>
          </p>
          <p className={styles.text}>
            Так, поки замовлення не передано до відділу логістики (зазвичай
            протягом 2–4 годин після оформлення). Зверніться до нашої підтримки
            за телефоном або електронною поштою.
          </p>

          <p className={styles.text}>
            <strong>Як оформити повернення товару?</strong>
          </p>
          <p className={styles.text}>
            Детальна інструкція з повернення розміщена на сторінці
            <strong> Повернення та обмін</strong>. Загалом: напишіть нам на
            returns@booktop.ua, отримайте підтвердження та відправте товар
            Новою поштою або Укрпоштою.
          </p>

          <p className={styles.text}>
            <strong>Чи є безкоштовна доставка?</strong>
          </p>
          <p className={styles.text}>
            Так, при замовленні від <strong>500 грн</strong> доставка
            Новою поштою або Укрпоштою по Україні — безкоштовна.
          </p>

          <p className={styles.text}>
            <strong>Як отримати знижку?</strong>
          </p>
          <p className={styles.text}>
            Підпишіться на нашу розсилку — отримаєте знижку <strong>10% на перше
            замовлення</strong>. Також стежте за нашими акціями в соціальних
            мережах та на сторінці <strong>Акції</strong> на сайті.
          </p>

          <p className={styles.text}>
            <strong>Чи можна замовити книгу, якої немає в каталозі?</strong>
          </p>
          <p className={styles.text}>
            Напишіть нам на sales@booktop.ua з назвою книги та автора. Ми
            спробуємо знайти та замовити її для вас у нашого постачальника.
            Зазвичай це займає <strong>7–14 робочих днів</strong>.
          </p>
        </section>

        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Звернення щодо захисту персональних даних</h2>
          <p className={styles.text}>
            Якщо у вас є питання або зауваження щодо обробки ваших персональних
            даних, зверніться до нашого відповідального за захист даних:
            <strong> dpo@booktop.ua</strong>. Ми відповідаємо на такі звернення
            протягом <strong>72 годин</strong>.
          </p>
        </section>
      </div>
    </PageLayout>
  );
}
