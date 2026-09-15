import type { TokenPair } from './authCookies'

export interface UserCredentials {
  id: string
  username: string
  email: string
  role: string | null
  token: string
  expireTime: string
  refreshToken: string
  refreshTokenExpireTime: string
}

export interface AuthUser {
  id: string
  username: string
  email: string
  role: string | null
}

export function toTokenPair(credentials: UserCredentials): TokenPair {
  return {
    accessToken: credentials.token,
    accessTokenExpiresAt: credentials.expireTime,
    refreshToken: credentials.refreshToken,
    refreshTokenExpiresAt: credentials.refreshTokenExpireTime,
  }
}

export function toAuthUser(credentials: UserCredentials): AuthUser {
  return {
    id: credentials.id,
    username: credentials.username,
    email: credentials.email,
    role: credentials.role,
  }
}

export const LOGIN_USER_MUTATION = `
  mutation LoginUser($request: AuthenticateUserRequestInput!) {
    loginUser(request: $request) {
      id username email role
      token expireTime refreshToken refreshTokenExpireTime
    }
  }
`

export const REGISTER_USER_MUTATION = `
  mutation RegisterUser($request: CreateUserRequestInput!) {
    registerUser(request: $request) {
      id username email role
      token expireTime refreshToken refreshTokenExpireTime
    }
  }
`

export const REVOKE_TOKEN_MUTATION = `
  mutation RevokeToken($input: RevokeTokenRequestInput!) {
    revoke(input: $input) {
      accessToken { value expiresAt }
      refreshToken { value expiresAt }
    }
  }
`
