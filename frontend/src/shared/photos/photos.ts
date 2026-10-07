import coxsBazar1280 from './assets/coxs-bazar-1280.avif'
import coxsBazar1280Webp from './assets/coxs-bazar-1280.webp'
import coxsBazar1920 from './assets/coxs-bazar-1920.avif'
import coxsBazar640 from './assets/coxs-bazar-640.avif'
import kaptai1280 from './assets/kaptai-lake-1280.avif'
import kaptai1280Webp from './assets/kaptai-lake-1280.webp'
import kaptai1920 from './assets/kaptai-lake-1920.avif'
import kaptai640 from './assets/kaptai-lake-640.avif'
import ratargul1280 from './assets/ratargul-1280.avif'
import ratargul1280Webp from './assets/ratargul-1280.webp'
import ratargul1920 from './assets/ratargul-1920.avif'
import ratargul640 from './assets/ratargul-640.avif'
import sajek1280 from './assets/sajek-1280.avif'
import sajek1280Webp from './assets/sajek-1280.webp'
import sajek1920 from './assets/sajek-1920.avif'
import sajek640 from './assets/sajek-640.avif'

/**
 * The website's own photos - real Bangladesh, from Wikimedia Commons. The
 * home page's slideshow and the photo heroes of the other pages share them.
 *
 * Each photo comes in three widths of AVIF (a modern format, about half
 * the size of JPEG) and one WebP for the few browsers without AVIF. The
 * browser picks ONE file: a phone downloads the 640px one, a big screen the
 * 1920px one. They sit under a dark overlay, so they're compressed hard.
 *
 * The licences (CC BY-SA) let anyone use and resize the photos, as long as
 * the photographer and licence are credited, with links. The photos stay
 * clean on the page; every credit is on ONE page instead - "Photo credits"
 * (/photo-credits, linked from the footer), which lists `sitePhotos` below.
 * A new photo must go into that list too. (Resized from the Commons originals.)
 */
export type Photo = {
  /** For srcset: "file 640w, file 1280w, file 1920w". */
  avif: string
  /** Fallback for browsers without AVIF. */
  webp: string
  /** The CSS object-position that keeps the subject in frame when a narrow screen crops the sides. */
  focus: string
  credit: { author: string; license: string; licenseUrl: string; source: string }
}

const srcset = (w640: string, w1280: string, w1920: string) => `${w640} 640w, ${w1280} 1280w, ${w1920} 1920w`

const ccBySa3 = { license: 'CC BY-SA 3.0', licenseUrl: 'https://creativecommons.org/licenses/by-sa/3.0/' }
const ccBySa4 = { license: 'CC BY-SA 4.0', licenseUrl: 'https://creativecommons.org/licenses/by-sa/4.0/' }

/** The beach at sunset. */
export const coxsBazarPhoto: Photo = {
  avif: srcset(coxsBazar640, coxsBazar1280, coxsBazar1920),
  webp: coxsBazar1280Webp,
  focus: '62% 50%',
  credit: { author: 'Tanweer Morshed', ...ccBySa3, source: "https://commons.wikimedia.org/wiki/File:Cox's_Bazar_sea_beach_01.jpg" },
}

/** A boat in the flooded forest of Sylhet. */
export const ratargulPhoto: Photo = {
  avif: srcset(ratargul640, ratargul1280, ratargul1920),
  webp: ratargul1280Webp,
  focus: '50% 60%',
  credit: { author: 'Abdulmominbd, retouched by Aristeas & Iifar', ...ccBySa4, source: 'https://commons.wikimedia.org/wiki/File:Ratargul_785_retouched.jpg' },
}

/** Green hills and blue water in Rangamati. */
export const kaptaiLakePhoto: Photo = {
  avif: srcset(kaptai640, kaptai1280, kaptai1920),
  webp: kaptai1280Webp,
  focus: '62% 50%',
  credit: { author: 'Nasif jamil', ...ccBySa4, source: 'https://commons.wikimedia.org/wiki/File:Kaptai_lake_view.jpg' },
}

/** Clouds over Sajek Valley. */
export const sajekPhoto: Photo = {
  avif: srcset(sajek640, sajek1280, sajek1920),
  webp: sajek1280Webp,
  focus: '50% 50%',
  credit: { author: 'Sahriar ahmed tasvir', ...ccBySa4, source: 'https://commons.wikimedia.org/wiki/File:Clouds_at_Sajek_Valley_20171220.jpg' },
}

/** Every photo the website shows, with what it shows - the "Photo credits" page lists them all. */
export const sitePhotos: { title: string; photo: Photo }[] = [
  { title: "Cox's Bazar sea beach", photo: coxsBazarPhoto },
  { title: 'Ratargul Swamp Forest, Sylhet', photo: ratargulPhoto },
  { title: 'Kaptai Lake, Rangamati', photo: kaptaiLakePhoto },
  { title: 'Clouds over Sajek Valley', photo: sajekPhoto },
]
