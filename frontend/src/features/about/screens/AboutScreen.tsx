"use client";

import Image from "next/image";
import { useRouter } from "next/navigation";
import { ChevronLeftIcon, PageLayout } from "@/shared/ui";
import iconStyles from "@/shared/ui/icons/Icon.module.css";
import styles from "./AboutScreen.module.css";

export function AboutScreen() {
  const router = useRouter();

  return (
    <PageLayout
      headerProps={{
        homeHref: "/home",
        userHref: "/me",
        searchPlaceholder: "Пошук книг",
      }}
      footerProps={{ homeHref: "/home" }}
    >
      <div className={styles.aboutMain}>
        <div className={styles.topRow}>
          <button
            type="button"
            className={styles.backButton}
            aria-label="Назад"
            onClick={() => router.back()}
          >
            <ChevronLeftIcon className={`${iconStyles.icon} ${styles.backButtonIcon}`.trim()} />
          </button>
          <h1 className={styles.title}>Про нас</h1>
        </div>

        <div className={styles.body}>
          <p className={styles.text}>
            Ми — книжковий інтернет-магазин, створений людьми, які щиро люблять читати і вірять,
            що хороша книга може змінити день, настрій і навіть життя. Наша історія почалася з
            простої ідеї: зробити якісні книги доступними для кожного. Ми ретельно підбираємо
            асортимент — від сучасної художньої літератури до нон-фікшну, дитячих книг і видань,
            які важко знайти в звичайних магазинах.
          </p>
          <p className={styles.text}>
            Наша місія — надихати читати більше та відкривати нові історії. Ми підтримуємо
            українських авторів і видавництва, а також слідкуємо за світовими новинками, щоб ви
            завжди знаходили щось цікаве. Для нас важливо не просто продати книгу, а допомогти вам
            знайти саме «ту саму». Тому ми завжди готові порадити, підказати і зробити ваш досвід
            покупки максимально приємним. Ми віримо: книга — це найкраща інвестиція в себе.
          </p>
        </div>

        <div className={styles.catWrap}>
          <Image
            className={styles.cat}
            src="/about-cat.svg"
            alt=""
            width={127}
            height={166}
            priority
          />
        </div>
      </div>
    </PageLayout>
  );
}
