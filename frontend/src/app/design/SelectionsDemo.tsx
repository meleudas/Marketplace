"use client";

import { useState } from "react";
import { Checkbox, Radio, RadioGroup, Typography } from "@/shared/ui";
import styles from "./UiKitShowcase.module.css";

export function SelectionsDemo() {
  const [newsletter, setNewsletter] = useState(false);
  const [delivery, setDelivery] = useState("pickup");

  return (
    <div className={styles.selectionShowcase}>
      <div className={styles.selectionBlock}>
        <Typography variant="body2" className={styles.selectionCaption}>
          Checkbox
        </Typography>
        <Checkbox
          name="newsletter"
          label="Отримувати розсилку"
          checked={newsletter}
          onCheckedChange={setNewsletter}
        />
        <Typography variant="body2" className={styles.selectionStatus}>
          Стан: {newsletter ? "обрано" : "не обрано"}
        </Typography>
      </div>

      <div className={styles.selectionBlock}>
        <Typography variant="body2" className={styles.selectionCaption}>
          Radio Button
        </Typography>
        <RadioGroup
          name="delivery"
          value={delivery}
          onValueChange={setDelivery}
          label="Спосіб отримання"
        >
          <Radio value="pickup" label="Самовивіз" />
          <Radio value="courier" label="Кур'єр" />
          <Radio value="post" label="Нова пошта" />
        </RadioGroup>
        <Typography variant="body2" className={styles.selectionStatus}>
          Обрано: {delivery}
        </Typography>
      </div>
    </div>
  );
}
