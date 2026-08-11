import { setupServer } from 'msw/node'

/** No default handlers: every test declares the endpoints it expects to be called. */
export const server = setupServer()
