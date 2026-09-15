import { z } from 'zod'
import { setAuthCookies } from '../../utils/authCookies'
import { callGraphql } from '../../utils/graphqlClient'
import { LOGIN_USER_MUTATION, toAuthUser, toTokenPair, type UserCredentials } from '../../utils/authOperations'

const bodySchema = z.object({
  email: z.email(),
  password: z.string().min(1),
})

export default defineEventHandler(async (event) => {
  const body = await readValidatedBody(event, bodySchema.parse)

  const data = await callGraphql<{ loginUser: UserCredentials }>(
    event,
    LOGIN_USER_MUTATION,
    { request: { email: body.email, password: body.password } },
  )

  setAuthCookies(event, toTokenPair(data.loginUser))

  return toAuthUser(data.loginUser)
})
