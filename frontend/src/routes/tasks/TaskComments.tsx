import { useForm } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { zodResolver } from '@hookform/resolvers/zod'
import { Send } from 'lucide-react'

import { Alert } from '@/components/ui/Alert'
import { Avatar } from '@/components/ui/Avatar'
import { Button } from '@/components/ui/Button'
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
    <section className="rounded-xl bg-white p-6 shadow-sm ring-1 ring-slate-200">
      <h2 className="mb-4 text-sm font-semibold text-slate-900">
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
                      <span className="font-medium text-slate-900">
                        {comment.author?.fullName ?? 'Изтрит член'}
                      </span>
                      <span className="ml-2 text-xs text-slate-500">
                        {formatDateTime(comment.createdAtUtc)}
                      </span>
                    </p>
                    <p className="mt-0.5 text-sm whitespace-pre-wrap text-slate-700">
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
            className="space-y-2 border-t border-slate-200 pt-4"
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
              className="focus:ring-brand-500 block w-full rounded-lg border-0 px-3 py-2 text-sm text-slate-900 shadow-sm ring-1 ring-slate-300 ring-inset placeholder:text-slate-400 focus:ring-2 focus:ring-inset focus:outline-none"
              {...register('text')}
            />

            {errors.text && (
              <p role="alert" className="text-sm text-red-600">
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
    </section>
  )
}
