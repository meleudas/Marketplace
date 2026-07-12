"use client";

import Image from "next/image";
import { useRef, useState } from "react";
import { ChevronLeftIcon, ChevronRightIcon } from "@/shared/ui";
import iconStyles from "@/shared/ui/icons/Icon.module.css";
import styles from "./ProductGallery.module.css";

interface ProductGalleryProps {
  images: string[];
  alt: string;
}

export function ProductGallery({ images, alt }: ProductGalleryProps) {
  const trackRef = useRef<HTMLDivElement>(null);
  const [activeIndex, setActiveIndex] = useState(0);

  if (images.length === 0) {
    return <div className={styles.placeholder} aria-hidden="true" />;
  }

  const scrollToIndex = (index: number) => {
    const track = trackRef.current;
    const slide = track?.children[index];
    if (slide instanceof HTMLElement) {
      slide.scrollIntoView({ behavior: "smooth", inline: "center", block: "nearest" });
    }
  };

  const handleScroll = () => {
    const track = trackRef.current;
    if (!track || track.clientWidth === 0) {
      return;
    }

    const index = Math.round(track.scrollLeft / track.clientWidth);
    setActiveIndex(Math.min(Math.max(index, 0), images.length - 1));
  };

  const goToIndex = (index: number) => {
    setActiveIndex(index);
    scrollToIndex(index);
  };

  return (
    <div className={styles.gallery}>
      <div className={styles.track} ref={trackRef} onScroll={handleScroll}>
        {images.map((imageUrl, index) => (
          <div className={styles.slide} key={`${imageUrl}-${index}`}>
            <Image
              src={imageUrl}
              alt={alt}
              fill
              sizes="100vw"
              className={styles.image}
              priority={index === 0}
            />
          </div>
        ))}
      </div>

      {images.length > 1 ? (
        <>
          <button
            type="button"
            className={`${styles.arrow} ${styles.arrowLeft}`}
            aria-label="Попереднє зображення"
            disabled={activeIndex === 0}
            onClick={() => goToIndex(Math.max(activeIndex - 1, 0))}
          >
            <ChevronLeftIcon className={iconStyles.icon} />
          </button>
          <button
            type="button"
            className={`${styles.arrow} ${styles.arrowRight}`}
            aria-label="Наступне зображення"
            disabled={activeIndex === images.length - 1}
            onClick={() => goToIndex(Math.min(activeIndex + 1, images.length - 1))}
          >
            <ChevronRightIcon className={iconStyles.icon} />
          </button>

          <div className={styles.dots}>
            {images.map((imageUrl, index) => (
              <button
                key={`${imageUrl}-dot-${index}`}
                type="button"
                className={`${styles.dot} ${index === activeIndex ? styles.dotActive : ""}`.trim()}
                aria-label={`Перейти до зображення ${index + 1}`}
                onClick={() => goToIndex(index)}
              />
            ))}
          </div>
        </>
      ) : null}
    </div>
  );
}
