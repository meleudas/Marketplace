"use client";

import { forwardRef, useCallback, useEffect, useState } from "react";
import { useAuth } from "@/features/auth/model/auth.store";
import {
  createProductReview,
  fetchProductReviews,
  type ProductReviewDto,
} from "../api/product-reviews.api";
import { Button, Spinner } from "@/shared/ui";
import { StarRating } from "./StarRating";
import styles from "./ReviewsSection.module.css";

interface ReviewsSectionProps {
  productId: number;
}

const REVIEWS_PAGE_SIZE = 5;
const REVIEWS_FETCH_SIZE = 20;
const COMMENT_EXPAND_THRESHOLD = 500;

function formatReviewDate(value: string): string {
  return new Date(value).toLocaleDateString("uk-UA", {
    day: "numeric",
    month: "long",
    year: "numeric",
  });
}

function ReviewCard({ review }: { review: ProductReviewDto }) {
  const [expanded, setExpanded] = useState(false);
  const isLong = review.comment.length > COMMENT_EXPAND_THRESHOLD;

  return (
    <article
      className={styles.card}
      data-testid="product-review-item"
      data-review-id={String(review.id)}
    >
      <div className={styles.cardHeader}>
        <div className={styles.cardHeaderMeta}>
          <span className={styles.author} data-testid="product-review-author">
            {review.userName}
          </span>
          <span className={styles.date} data-testid="product-review-date">
            {formatReviewDate(review.createdAt)}
          </span>
        </div>
        <StarRating value={review.rating ?? review.overallRating ?? 0} size="md" />
      </div>

      <div className={`${styles.commentWrap} ${!expanded && isLong ? styles.commentCollapsed : ""}`}>
        <p className={styles.comment} data-testid="product-review-text">
          {review.comment}
        </p>
        {!expanded && isLong ? (
          <div className={styles.commentFade} />
        ) : null}
      </div>

      {isLong ? (
        <button
          type="button"
          className={styles.expandToggle}
          onClick={() => setExpanded((prev) => !prev)}
        >
          {expanded ? "Згорнути" : "Розгорнути"}
        </button>
      ) : null}
    </article>
  );
}

export const ReviewsSection = forwardRef<HTMLElement, ReviewsSectionProps>(function ReviewsSection(
  { productId },
  ref,
) {
  const { isAuthenticated } = useAuth();

  const [allReviews, setAllReviews] = useState<ProductReviewDto[]>([]);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);

  // Inline form state
  const [formRating, setFormRating] = useState(0);
  const [formComment, setFormComment] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const loadReviews = useCallback(async () => {
    setLoading(true);
    try {
      const result = await fetchProductReviews(productId, { page: 1, size: REVIEWS_FETCH_SIZE });
      setAllReviews(result.items);
      setPage(1);
    } catch {
      setAllReviews([]);
    } finally {
      setLoading(false);
    }
  }, [productId]);

  useEffect(() => {
    void loadReviews();
  }, [loadReviews]);

  const totalPages = Math.max(1, Math.ceil(allReviews.length / REVIEWS_PAGE_SIZE));
  const visibleReviews = allReviews.slice((page - 1) * REVIEWS_PAGE_SIZE, page * REVIEWS_PAGE_SIZE);

  const handlePageChange = (nextPage: number) => {
    setPage(Math.min(Math.max(nextPage, 1), totalPages));
  };

  const handleSubmit = async () => {
    if (formRating === 0) {
      setSubmitError("Будь ласка, поставте оцінку");
      return;
    }
    if (!formComment.trim()) {
      setSubmitError("Будь ласка, напишіть відгук");
      return;
    }

    setSubmitting(true);
    setSubmitError(null);

    try {
      await createProductReview(productId, { rating: formRating, comment: formComment.trim() });
      setFormRating(0);
      setFormComment("");
      await loadReviews();
    } catch {
      setSubmitError("Не вдалося надіслати відгук. Спробуйте пізніше.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <section ref={ref} id="product-reviews" className={styles.section} data-testid="product-reviews">
      <h2 className={styles.sectionTitle}>Відгуки</h2>

      <div className={styles.layout}>
        <div className={styles.formCol}>
          <div className={styles.formCard} data-testid="product-review-form">
            <h3 className={styles.formTitle} data-testid="product-review-form-title">
              {isAuthenticated ? "Залишити відгук" : "Увійдіть, щоб залишити відгук"}
            </h3>

            <div className={styles.formRatingRow}>
              <span className={styles.formLabel}>Оцінка</span>
              <StarRating
                value={formRating}
                size="md"
                interactive
                onChange={isAuthenticated ? setFormRating : undefined}
              />
            </div>

            <div className={styles.formField}>
              <span className={styles.formLabel}>Коментар</span>
              <textarea
                className={styles.formTextarea}
                data-testid="product-review-comment"
                placeholder="Поділіться враженнями про книгу..."
                value={formComment}
                onChange={(e) => setFormComment(e.target.value)}
                disabled={!isAuthenticated || submitting}
                rows={5}
              />
            </div>

            {submitError ? (
              <p className={styles.formError} data-testid="product-review-error" role="alert">
                {submitError}
              </p>
            ) : null}

            <Button
              variant="primary"
              fullWidth
              data-testid="product-review-submit"
              disabled={!isAuthenticated || submitting}
              onClick={handleSubmit}
            >
              {submitting ? "Надсилання..." : "Надіслати відгук"}
            </Button>
          </div>
        </div>

        <div className={styles.reviewsCol} data-testid="product-reviews-list">
          {loading ? (
            <div className={styles.loadingState}>
              <Spinner />
              <span className={styles.loadingText}>Завантаження відгуків...</span>
            </div>
          ) : allReviews.length === 0 ? (
            <p className={styles.emptyState}>Ще немає відгуків. Будьте першим!</p>
          ) : (
            <>
              <div className={styles.list}>
                {visibleReviews.map((review) => (
                  <ReviewCard review={review} key={review.id} />
                ))}
              </div>

              {totalPages > 1 ? (
                <div className={styles.pagination}>
                  {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
                    <button
                      key={p}
                      type="button"
                      className={`${styles.pageBtn} ${p === page ? styles.pageBtnActive : ""}`}
                      onClick={() => handlePageChange(p)}
                    >
                      {p}
                    </button>
                  ))}
                </div>
              ) : null}
            </>
          )}
        </div>
      </div>
    </section>
  );
});
