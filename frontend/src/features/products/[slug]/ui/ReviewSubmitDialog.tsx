"use client";

import { useEffect, useState } from "react";
import { Button, CloseIcon, StarIcon } from "@/shared/ui";
import iconStyles from "@/shared/ui/icons/Icon.module.css";
import styles from "./ReviewSubmitDialog.module.css";

interface ReviewSubmitDialogProps {
  open: boolean;
  submitting: boolean;
  error: string | null;
  onClose: () => void;
  onSubmit: (payload: { rating: number; comment: string }) => void;
}

const STAR_VALUES = [1, 2, 3, 4, 5];

function ReviewSubmitDialogContent({ submitting, error, onClose, onSubmit }: Omit<ReviewSubmitDialogProps, "open">) {
  const [rating, setRating] = useState(5);
  const [hoveredRating, setHoveredRating] = useState<number | null>(null);
  const [comment, setComment] = useState("");

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };

    const previousBodyOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    window.addEventListener("keydown", handleKeyDown);

    return () => {
      document.body.style.overflow = previousBodyOverflow;
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [onClose]);

  const displayedRating = hoveredRating ?? rating;

  const handleSubmit = () => {
    if (!comment.trim()) {
      return;
    }

    onSubmit({ rating, comment: comment.trim() });
  };

  return (
    <div className={styles.overlay} onClick={onClose}>
      <div
        className={styles.dialog}
        role="dialog"
        aria-modal="true"
        aria-labelledby="review-submit-dialog-title"
        onClick={(event) => event.stopPropagation()}
      >
        <button type="button" className={styles.closeButton} aria-label="Закрити" onClick={onClose}>
          <CloseIcon className={iconStyles.icon} />
        </button>

        <h2 id="review-submit-dialog-title" className={styles.title}>
          Надіслати відгук
        </h2>

        <div className={styles.starsRow} role="radiogroup" aria-label="Оцінка">
          {STAR_VALUES.map((star) => (
            <button
              key={star}
              type="button"
              className={styles.starButton}
              role="radio"
              aria-checked={rating === star}
              aria-label={`${star} з 5`}
              onMouseEnter={() => setHoveredRating(star)}
              onMouseLeave={() => setHoveredRating(null)}
              onClick={() => setRating(star)}
            >
              <StarIcon className={star <= displayedRating ? styles.starFilled : styles.starEmpty} />
            </button>
          ))}
        </div>

        <textarea
          className={styles.textarea}
          placeholder="Поділіться враженнями про магазин та товар"
          value={comment}
          onChange={(event) => setComment(event.target.value)}
          rows={5}
        />

        {error ? <p className={styles.error}>{error}</p> : null}

        <div className={styles.actions}>
          <Button
            variant="primary"
            size="lg"
            fullWidth
            disabled={submitting || !comment.trim()}
            onClick={handleSubmit}
          >
            {submitting ? "Надсилання..." : "Надіслати"}
          </Button>
        </div>
      </div>
    </div>
  );
}

export function ReviewSubmitDialog({ open, submitting, error, onClose, onSubmit }: ReviewSubmitDialogProps) {
  if (!open) {
    return null;
  }

  return (
    <ReviewSubmitDialogContent submitting={submitting} error={error} onClose={onClose} onSubmit={onSubmit} />
  );
}
