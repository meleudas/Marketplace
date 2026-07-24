import type { Metadata } from "next";
import { PageLayout } from "@/shared/ui";
import styles from "./page.module.css";

export const metadata: Metadata = {
  title: "Програма лояльності | Booktop",
  description: "Програма лояльності Booktop — накопичуйте бали, отримуйте знижки та ексклюзивні пропозиції.",
};

export default function LoyaltyPage() {
  return (
    <PageLayout headerProps={{}} footerProps={{}}>
      <div className={styles.page}>
        <h1 className={styles.title}>Програма лояльності</h1>

        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Як це працює</h2>
          <p className={styles.text}>
            Програма лояльності Booktop — це можливість отримувати бали за кожну покупку та
            обмінювати їх на знижки, ексклюзивні пропозиції та приємні бонуси. Чим більше ви
            купуєте, тим вищим стає ваш рівень — а з ним зростають переваги.
          </p>
          <p className={styles.text}>
            Участь у програмі <strong>повністю безкоштовна</strong> і не потребує окремої
            реєстрації — достатньо мати акаунт на Booktop.
          </p>
        </section>

        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Як заробити бали</h2>
          <p className={styles.text}>
            Бали нараховуються за різну активність на платформі. Ось основні способи
            накопичення балів:
          </p>
          <ul className={styles.list}>
            <li><strong>Покупки книг</strong> — базовий спосіб. Кількість балів залежить від вашого рівня в програмі</li>
            <li><strong>Відгуки на книги</strong> — <strong>10 балів</strong> за кожен опублікований відгук із фотографією та оцінкою (не більше 5 відгуків на тиждень)</li>
            <li><strong>Реферальна програма</strong> — <strong>200 балів</strong> за кожного запрошеного друга, який зробив першу покупку</li>
            <li><strong>Участь у подіях</strong> — <strong>25 балів</strong> за відвідування заходів Booktop</li>
            <li><strong>День народження</strong> — <strong>500 балів</strong> подарунок на ваш день народження (доступний один раз на рік, протягом 30 днів від дати)</li>
            <li><strong>Перепродажі</strong> — <strong>15 балів</strong> за кожен успішний продаж вживаної книги</li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Рівні програми</h2>
          <p className={styles.text}>
            Програма лояльності Booktop має чотири рівні. Підвищення рівня відбувається
            автоматично після досягнення відповідної суми покупок за останні <strong>12 місяців</strong>.
          </p>
          <ul className={styles.list}>
            <li><strong>Бронза (Читач)</strong> — базовий рівень для всіх зареєстрованих користувачів. <strong>1 бал</strong> за кожні 100 грн покупки. Доступ до акційних пропозицій та перепродажів</li>
            <li><strong>Срібло (Книгоман)</strong> — від <strong>2 000 грн</strong> покупок. <strong>1,5 бала</strong> за кожні 100 грн. Знижка комісії на перепродажах (10%). Знижка <strong>5%</strong> на нові книги. Ранній доступ до розпродажів (24 години)</li>
            <li><strong>Золото (Бібліофіл)</strong> — від <strong>5 000 грн</strong> покупок. <strong>2 бали</strong> за кожні 100 грн. Знижка <strong>10%</strong> на нові книги. Безкоштовна доставка на всі замовлення. Пріоритетна підтримка</li>
            <li><strong>Платина (Експерт)</strong> — від <strong>15 000 грн</strong> покупок. <strong>3 бали</strong> за кожні 100 грн. Знижка <strong>15%</strong> на нові книги. Ексклюзивні пропозиції та закриті розпродажі. Запрошення на VIP-заходи. Персональні рекомендації від редакції</li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Як витратити бали</h2>
          <p className={styles.text}>
            Накопичені бали мають цінність і можуть бути використані різними способами:
          </p>
          <ul className={styles.list}>
            <li><strong>Оплата замовлення</strong> — <strong>100 балів = 10 грн</strong> знижки. Бали можна застосувати до оплати до <strong>50% вартості</strong> будь-якого замовлення</li>
            <li><strong>Безкоштовна доставка</strong> — <strong>150 балів</strong> для безкоштовної доставки замовлення (незалежно від суми)</li>
            <li><strong>Підписка Booktop Plus</strong> — <strong>1 500 балів</strong> за 1 місяць підписки, яка дає додаткові знижки та ексклюзивний контент</li>
            <li><strong>Благодійні внески</strong> — конвертація балів у благодійні внески для підтримки бібліотек та освітніх проєктів Booktop</li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Спеціальні пропозиції для учасників</h2>
          <p className={styles.text}>
            Крім базових переваг рівнів, учасники програми лояльності отримують доступ до
            ексклюзивних пропозицій, які недоступні іншим користувачам:
          </p>
          <ul className={styles.list}>
            <li><strong>Закриті розпродажі</strong> — ексклюзивні знижки до <strong>50%</strong> для учасників рівня «Золото» та «Платина»</li>
            <li><strong>Ранній доступ до новинок</strong> — можливість придбати очікувані новинки за <strong>48 годин</strong> до загального продажу</li>
            <li><strong>Подарункові набори</strong> — спеціальні цінові пропозиції на тематичні набори книг для учасників</li>
            <li><strong>Автограф-сесії</strong> — пріоритетне запрошення на закриті зустрічі з авторами</li>
            <li><strong>Подвоєння балів</strong> — періодичні акції «Подвоєння балів» для певних категорій книг</li>
          </ul>
        </section>

        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Як приєднатися</h2>
          <p className={styles.text}>
            Приєднатися до програми лояльності Booktop максимально просто:
          </p>
          <ul className={styles.list}>
            <li><strong>Крок 1.</strong> Зареєструйтесь на сайті Booktop або увійдіть в існуючий акаунт</li>
            <li><strong>Крок 2.</strong> Перейдіть у розділ <strong>«Мій рахунок» → «Програма лояльності»</strong></li>
            <li><strong>Крок 3.</strong> Погодьтеся з умовами програми</li>
            <li><strong>Крок 4.</strong> Ви автоматично стаєте учасником рівня «Бронза» та починаєте накопичувати бали</li>
          </ul>
          <p className={styles.text}>
            Після реєстрації ви зможете переглядати стан балів, історію нарахувань та
            поточний рівень у своєму особистому кабінеті. Бали дійсні протягом <strong>24 місяців</strong>
            з дати нарахування.
          </p>
        </section>
      </div>
    </PageLayout>
  );
}
