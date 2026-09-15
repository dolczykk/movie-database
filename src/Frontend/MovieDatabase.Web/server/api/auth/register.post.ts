import { z } from 'zod'
import { setAuthCookies } from '../../utils/authCookies'
import { callGraphql } from '../../utils/graphqlClient'
import { REGISTER_USER_MUTATION, toAuthUser, toTokenPair, type UserCredentials } from '../../utils/authOperations'

const bodySchema = z.object({
  username: z.string().min(1),
  email: z.email(),
  password: z.string().min(8),
})

export default defineEventHandler(async (event) => {
  const body = await readValidatedBody(event, bodySchema.parse)

  const data = await callGraphql<{ registerUser: UserCredentials }>(
    event,
    REGISTER_USER_MUTATION,
    { request: { username: body.username, email: body.email, password: body.password } },
  )

  setAuthCookies(event, toTokenPair(data.registerUser))

  return toAuthUser(data.registerUser)
})
