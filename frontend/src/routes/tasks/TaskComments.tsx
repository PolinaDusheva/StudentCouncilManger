import { useForm } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { zodResolver } from '@hookform/resolvers/zod'
import { Send } from 'lucide-react'

import { Alert } from '@/components/ui/Alert'
import { Avatar } from '@/components/ui/Avatar'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import { Spinner } from '@/components/ui/Spinner'
import { errorMessage } from '@/lib/api/problem'
import { queryKeys } from '@/lib/api/queryKeys'
import { addCommentSchema, addTaskComment, getTaskComments, type AddCommentForm } from '@/lib/api/tasks'
import { formatDateTime } from '@/lib/utils/format'

/** Comments are open to anyone who can see the task — no extra permission check needed. */
export function TaskComments({ taskId }: { taskId: string }) {
  const queryClient = useQueryClient()

  const { data: comments, isPending, isError, error } = useQuery({
    queryKey: queryKeys.tasks.comments(taskId),
    queryFn: () => getTaskComments(taskId),
  })

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<AddCommentForm>({
    resolver: zodResolver(addCommentSchema),
    defaultValues: { text: '' },
  })

  const add = useMutation({
    mutationFn: (values: AddCommentForm) => addTaskComment(taskId, values),
    onSuccess: async () => {
      reset({ text: '' })
      await queryClient.invalidateQueries({ queryKey: queryKeys.tasks.comments(taskId) })
      // The list and board show a comment count that is now stale.
      await queryClient.invalidateQueries({ queryKey: queryKeys.tasks.lists() })
    },
  })

  return (
    <Card variant="panel">
      <h2 className="mb-4 font-serif text-[22px] leading-[1.3] font-normal text-ink">
        Коментари {comments && `(${comments.length})`}
      </h2>

      {isPending ? (
        <Spinner />
      ) : isError ? (
        <Alert tone="error">{errorMessage(error)}</Alert>
      ) : (
        <>
          {comments.length > 0 && (
            <ul className="mb-5 space-y-4">
              {comments.map((comment) => (
                <li key={comment.id} className="flex gap-3">
                  <Avatar
                    photoUrl={comment.author?.photoUrl}
                    fullName={comment.author?.fullName ?? '?'}
                    size="sm"
                  />
                  <div className="min-w-0 flex-1">
                    <p className="text-sm">
                      <span className="font-medium text-ink">
                        {comment.author?.fullName ?? 'Изтрит член'}
                      </span>
                      <span className="ml-2 text-xs text-muted">
                        {formatDateTime(comment.createdAtUtc)}
                      </span>
                    </p>
                    <p className="mt-0.5 text-sm whitespace-pre-wrap text-ink-soft">
                      {comment.text}
                    </p>
                  </div>
                </li>
              ))}
            </ul>
          )}

          <form
            onSubmit={handleSubmit((values) => add.mutate(values))}
            noValidate
            className="space-y-2 border-t border-divider pt-4"
          >
            {add.isError && <Alert tone="error">{errorMessage(add.error)}</Alert>}

            <label htmlFor="new-comment" className="sr-only">
              Нов коментар
            </label>
            <textarea
              id="new-comment"
              rows={3}
              placeholder="Напиши коментар…"
              aria-invalid={errors.text ? true : undefined}
              className="block w-full rounded-xl border-2 border-line bg-surface px-4 py-3 text-sm text-ink shadow-[inset_0_2px_5px_rgba(0,0,0,0.05)] placeholder:text-faint focus:border-accent focus:shadow-[0_0_0_4px_rgba(255,60,112,0.15)] focus:outline-none"
              {...register('text')}
            />

            {errors.text && (
              <p role="alert" className="text-sm text-danger">
                {errors.text.message}
              </p>
            )}

            <div className="flex justify-end">
              <Button type="submit" size="sm" loading={isSubmitting || add.isPending}>
                <Send aria-hidden className="size-4" />
                Публикувай
              </Button>
            </div>
          </form>
        </>
      )}
    </Card>
  )
}
