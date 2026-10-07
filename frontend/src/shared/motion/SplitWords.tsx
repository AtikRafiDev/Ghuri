import { cn } from '@/lib/utils'

/**
 * Writes `text` as one <span data-word> per word, so GSAP can move each
 * word on its own (select them with '[data-word]').
 *
 * mask: each word also sits in a clipping box, so a word moved down out of
 * its box is hidden - it then "rises" into view. The small bottom padding
 * keeps letters like g and y from being clipped.
 *
 * The spaces stay real text between the spans: copy-paste and screen
 * readers get the sentence exactly as written.
 */
export function SplitWords({ text, mask = false, wordClassName }: { text: string; mask?: boolean; wordClassName?: string }) {
  const words = text.split(' ')
  return words.map((word, i) => (
    <span key={i}>
      {mask ? (
        <span className="-mb-[0.12em] inline-block overflow-hidden pb-[0.12em] align-top">
          <span data-word className={cn('inline-block', wordClassName)}>
            {word}
          </span>
        </span>
      ) : (
        <span data-word className={cn('inline-block', wordClassName)}>
          {word}
        </span>
      )}
      {i < words.length - 1 && ' '}
    </span>
  ))
}
