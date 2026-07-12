"use client";

import { forwardRef, useCallback, useEffect, useState } from "react";
import { useAuth } from "@/features/auth/model/auth.store";
import {
  createProductReview,
  fetchProductReviews,
  type ProductReviewDto,
} from "../api/product-reviews.api";
import { Button, Pagination } from "@/shared/ui";
import { ReviewSubmitDialog } from "./ReviewSubmitDialog";
import { StarRating } from "./StarRating";
import styles from "./ReviewsSection.module.css";

interface ReviewsSectionProps {
  productId: number;
}

const REVIEWS_PAGE_SIZE = 3;
const REVIEWS_FETCH_SIZE = 100;
const COMMENT_PREVIEW_LENGTH = 140;

function formatReviewDate(value: string): string {
  return new Date(value).toLocaleDateString("uk-UA", {
    day: "numeric",
    month: "long",
    year: "numeric",
  });
}

function ReviewCard({ review }: { review: ProductReviewDto }) {
  const [expanded, setExpanded] = useState(false);
  const isLong = review.comment.length > COMMENT_PREVIEW_LENGTH;
  const displayedComment =
    expanded || !isLong ? review.comment : `${review.comment.slice(0, COMMENT_PREVIEW_LENGTH).trimEnd()}…`;

  return (
    <article className={styles.card}>
      <div className={styles.cardHeader}>
        <div className={styles.cardHeaderMeta}>
          <span className={styles.author}>{review.userName}</span>
          <span className={styles.date}>{formatReviewDate(review.createdAt)}</span>
        </div>
        <StarRating value={review.rating ?? review.overallRating ?? 0} />
      </div>

      <p className={styles.comment}>
        {displayedComment}
        {isLong && !expanded ? (
          <button type="button" className={styles.moreButton} onClick={() => setExpanded(true)}>
            Більше
          </button>
        ) : null}
      </p>
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

  const [dialogOpen, setDialogOpen] = useState(false);
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

  const handleSubmitReview = async ({ rating, comment }: { rating: number; comment: string }) => {
    setSubmitting(true);
    setSubmitError(null);

    try {
      await createProductReview(productId, { rating, comment });
      setDialogOpen(false);
      void loadReviews();
    } catch {
      setSubmitError("Не вдалося надіслати відгук. Спробуйте пізніше.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <section ref={ref} id="product-reviews" className={styles.section}>
      <h2 className={styles.sectionTitle}>Відгуки</h2>

      <Button
        variant="secondary"
        fullWidth
        className={styles.submitButton}
        disabled={!isAuthenticated}
        onClick={() => setDialogOpen(true)}
      >
        Надіслати відгук
      </Button>

      {loading ? (
        <p className={styles.stateText}>Завантаження відгуків...</p>
      ) : allReviews.length === 0 ? (
        <p className={styles.stateText}>Ще немає відгуків. Будьте першим!</p>
      ) : (
        <div className={styles.list}>
          {visibleReviews.map((review) => (
            <ReviewCard review={review} key={review.id} />
          ))}
        </div>
      )}

      {!loading && totalPages > 1 ? (
        <Pagination currentPage={page} totalPages={totalPages} onPageChange={handlePageChange} />
      ) : null}

      <ReviewSubmitDialog
        open={dialogOpen}
        submitting={submitting}
        error={submitError}
        onClose={() => setDialogOpen(false)}
        onSubmit={handleSubmitReview}
      />
    </section>
  );
});
