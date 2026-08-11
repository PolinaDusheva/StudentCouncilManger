import '@testing-library/jest-dom/vitest'

import { cleanup } from '@testing-library/react'
import { afterAll, afterEach, beforeAll } from 'vitest'

import { server } from './server'

// `error` on an unhandled request is deliberate: a test that hits an endpoint nobody stubbed
// should fail loudly rather than silently receive a network error.
beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))

/**
 * jsdom does not implement `<dialog>`: `showModal`, `close` and the `cancel` event are all
 * missing. This stub only toggles the `open` attribute so components that call them can be
 * rendered at all.
 *
 * It proves nothing about real dialog behaviour — focus trapping, the top layer, backdrop
 * rendering and Esc handling all come from the browser and stay unverified here. Anything
 * relying on those needs a manual check.
 */
if (!HTMLDialogElement.prototype.showModal) {
  HTMLDialogElement.prototype.showModal = function showModal() {
    this.open = true
  }
  HTMLDialogElement.prototype.close = function close() {
    this.open = false
    this.dispatchEvent(new Event('close'))
  }
}

afterEach(() => {
  cleanup()
  server.resetHandlers()
  localStorage.clear()
})

afterAll(() => server.close())
