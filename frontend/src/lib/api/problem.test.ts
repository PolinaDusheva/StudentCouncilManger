import { describe, expect, it } from 'vitest'

import { ApiError, errorMessage, NetworkError } from './problem'

describe('ApiError', () => {
  it('взима code от тялото на ProblemDetails', () => {
    const error = new ApiError(401, { code: 'invalid_credentials', title: 'Invalid' })
    expect(error.code).toBe('invalid_credentials')
  })

  it('извежда code от статуса, когато тялото няма такъв', () => {
    expect(new ApiError(404, {}).code).toBe('not_found')
    expect(new ApiError(409, {}).code).toBe('conflict')
    expect(new ApiError(423, {}).code).toBe('account_locked')
    expect(new ApiError(429, {}).code).toBe('rate_limited')
    expect(new ApiError(500, {}).code).toBe('internal_error')
  })

  it('разпознава валидационна грешка по наличието на errors', () => {
    const withErrors = new ApiError(400, { errors: { email: ['Задължително'] } })
    expect(withErrors.isValidation).toBe(true)
    expect(withErrors.fieldErrors.email).toEqual(['Задължително'])

    expect(new ApiError(400, {}).isValidation).toBe(false)
  })
})

describe('errorMessage', () => {
  it('превежда познат code на български', () => {
    const error = new ApiError(401, { code: 'invalid_credentials', title: 'Invalid' })
    expect(errorMessage(error)).toBe('Грешен имейл или парола.')
  })

  it('пада обратно към title при непознат code', () => {
    const error = new ApiError(400, { code: 'some_new_code', title: 'Нещо се обърка' })
    expect(errorMessage(error)).toBe('Нещо се обърка')
  })

  it('обработва мрежова грешка', () => {
    expect(errorMessage(new NetworkError())).toContain('Няма връзка със сървъра')
  })

  it('не се спъва в хвърлена стойност, която не е Error', () => {
    expect(errorMessage('нещо')).toBe('Възникна неочаквана грешка.')
  })
})
