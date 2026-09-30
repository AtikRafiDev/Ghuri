import { TagIcon } from 'lucide-react'
import { categoryIcons } from '../categoryIcons'

/** Draws a category's icon by its stored name; a plain tag if it has none (or an unknown one). */
export function CategoryIcon({ name, className }: { name: string | null; className?: string }) {
  const Icon = (name && categoryIcons[name]) || TagIcon
  return <Icon className={className} aria-hidden />
}
