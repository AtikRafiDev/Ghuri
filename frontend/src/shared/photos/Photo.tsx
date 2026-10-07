import type { ComponentProps } from 'react'
import { cn } from '@/lib/utils'
import type { Photo as PhotoData } from './photos'

type ImgProps = Omit<ComponentProps<'img'>, 'src' | 'srcSet' | 'sizes' | 'alt'>

/**
 * A full-cover background photo. <picture> lets the browser choose: AVIF in
 * the width that fits the screen (sizes="100vw": "as wide as the window"),
 * or the WebP if it can't read AVIF. alt="" because it's decoration - the
 * place's name is written as text next to it.
 */
export function Photo({ photo, className, style, ...img }: { photo: PhotoData } & ImgProps) {
  return (
    <picture>
      <source type="image/avif" srcSet={photo.avif} sizes="100vw" />
      <img
        src={photo.webp}
        alt=""
        className={cn('absolute inset-0 size-full object-cover', className)}
        style={{ objectPosition: photo.focus, ...style }}
        {...img}
      />
    </picture>
  )
}
