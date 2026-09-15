import { describe, expect, it } from 'vitest'
import * as generated from '../graphql/generated/graphql'

describe('generated graphql documents', () => {
  it('exports the film query document', () => {
    expect(generated.GetFilmsDocument).toBeDefined()
  })

  // `graphql/mutations/auth.graphql` is deliberately NOT part of the codegen
  // `documents` glob (see codegen.ts). loginUser/registerUser/revoke select
  // `token`/`refreshToken`, and server/utils/authOperations.ts already holds
  // the hand-written source of truth for them, consumed only by server/.
  // If one of these ever reappears here, it becomes callable from app/ code
  // via useGraphql and would hand the browser both tokens -- this test
  // exists to catch that regression, not to exercise real codegen output.
  it('does not export the server-only auth mutation documents', () => {
    expect('LoginUserDocument' in generated).toBe(false)
    expect('RegisterUserDocument' in generated).toBe(false)
    expect('RevokeTokenDocument' in generated).toBe(false)
  })
})
