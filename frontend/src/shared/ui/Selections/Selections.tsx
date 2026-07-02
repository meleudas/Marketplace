"use client";

import { useState } from "react";
import { Checkbox } from "../Checkbox";
import { Radio, RadioGroup } from "../Radio";
import { Typography } from "../Typography";
import styles from "./Selections.module.css";

export function Selections() {
  const [newsletter, setNewsletter] = useState(false);
  const [delivery, setDelivery] = useState("pickup");

  return (
    <div className={styles.showcase}>
      <div className={styles.block}>
        <Typography variant="body2" className={styles.caption}>
          Checkbox
        </Typography>
        <Checkbox
          name="newsletter"
          label="Отримувати розсилку"
          checked={newsletter}
          onCheckedChange={setNewsletter}
        />
        <Typography variant="body2" className={styles.status}>
          Стан: {newsletter ? "обрано" : "не обрано"}
        </Typography>
      </div>

      <div className={styles.block}>
        <Typography variant="body2" className={styles.caption}>
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
        <Typography variant="body2" className={styles.status}>
          Обрано: {delivery}
        </Typography>
      </div>
    </div>
  );
}
