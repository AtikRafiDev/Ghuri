import { coxsBazarPhoto, kaptaiLakePhoto, ratargulPhoto, type Photo } from '@/shared/photos/photos'

/** One hero slide: a photo, the place it shows, and the search it opens. (The photos themselves: shared/photos.) */
export type HeroSlide = { photo: Photo; place: string; line: string; search: string }

export const heroSlides: HeroSlide[] = [
  { place: "Cox's Bazar", line: 'Sunset walks on the longest sea beach in the world', search: "Cox's Bazar", photo: coxsBazarPhoto },
  { place: 'Ratargul Swamp Forest', line: 'Row through the flooded forest of Sylhet', search: 'Sylhet', photo: ratargulPhoto },
  { place: 'Kaptai Lake', line: 'Green hills and blue water in Rangamati', search: 'Rangamati', photo: kaptaiLakePhoto },
]

/** The first slide in miniature (24 × 16 px, ~130 bytes), written into the page: a blurred preview while the real photo downloads. */
export const heroPlaceholder =
  'data:image/webp;base64,UklGRnwAAABXRUJQVlA4IHAAAADwAwCdASoYABAAPu1krU6ppaSiMAgBMB2JQBOgAs7Qez87B4ItBAaAANnLEhPU7mUYPONwmEyT6ju070elkikyuzttbAGfMOwiyfq14x8WBpl1qUByUhvJf0MHWaDtsTpqunRJRXsMwMOM0tLDgAAA'
