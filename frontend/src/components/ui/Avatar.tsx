import { useAuthenticatedImage } from '@/lib/hooks/useAuthenticatedImage'
import { cn } from '@/lib/utils/cn'

type Size = 'sm' | 'md' | 'lg'

const SIZES: Record<Size, string> = {
  sm: 'size-8 text-xs',
  md: 'size-10 text-sm',
  lg: 'size-20 text-xl',
}

interface AvatarProps {
  /** The API's `photoUrl` (a relative path), or null when the member has no photo. */
  photoUrl: string | null | undefined
  fullName: string
  size?: Size
  className?: string
}

/** First letters of the first two words, e.g. "Иван Петров" → "ИП". */
function initials(fullName: string): string {
  return fullName
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((word) => word[0]?.toUpperCase() ?? '')
    .join('')
}

/**
 * Member photo, falling back to initials.
 *
 * The image is loaded through {@link useAuthenticatedImage} because the photo endpoint
 * requires a bearer token. Decorative for assistive tech: an avatar is always rendered next
 * to the member's name, so announcing it again would just repeat that name.
 */
export function Avatar({ photoUrl, fullName, size = 'md', className }: AvatarProps) {
  const objectUrl = useAuthenticatedImage(photoUrl)

  return (
    <span
      aria-hidden
      className={cn(
        'inline-flex shrink-0 items-center justify-center overflow-hidden rounded-full',
        'bg-brand-100 text-brand-700 font-medium select-none',
        SIZES[size],
        className,
      )}
    >
      {objectUrl ? (
        <img src={objectUrl} alt="" className="size-full object-cover" />
      ) : (
        initials(fullName)
      )}
    </span>
  )
}
