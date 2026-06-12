/**
 * OAuth 2.0 + PKCE 工具
 * tpssoadmin 作为标准 OAuth 客户端接入 SSO
 *
 * 职责：仅负责 OAuth 协议交互（PKCE 生成、授权码交换、Token 刷新）
 * Token 的存储和读取由 Pinia Store 统一管理，本模块不直接操作 localStorage
 */

const CLIENT_ID = import.meta.env.VITE_OAUTH_CLIENT_ID || 'tpsso_admin_client'
const SCOPES = import.meta.env.VITE_OAUTH_SCOPE || 'openid profile email roles'
const CODE_VERIFIER_KEY = 'admin_code_verifier'
const REDIRECT_KEY = 'admin_redirect'
// 在文件顶部添加
const VITE_SSO_URL = import.meta.env.VITE_AUTH_URL || 'https://auth.taipi.top'

/** 生成随机字符串 */
function randomString(length: number): string {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~'
  const array = new Uint8Array(length)
  crypto.getRandomValues(array)
  return Array.from(array, b => chars[b % chars.length]).join('')
}

/** SHA256 哈希并 Base64url 编码 */
async function sha256Base64url(str: string): Promise<string> {
  const encoded = new TextEncoder().encode(str)
  const hash = await crypto.subtle.digest('SHA-256', encoded)
  return btoa(String.fromCharCode(...new Uint8Array(hash)))
    .replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
}

/** 生成 PKCE code_verifier 和 code_challenge */
export async function generatePKCE(): Promise<{ verifier: string; challenge: string }> {
  const verifier = randomString(64)
  const challenge = await sha256Base64url(verifier)
  return { verifier, challenge }
}

/** 发起 OAuth 授权请求 */
export async function startOAuthLogin(redirectPath?: string): Promise<void> {
  const { verifier, challenge } = await generatePKCE()
  sessionStorage.setItem(CODE_VERIFIER_KEY, verifier)
  if (redirectPath) {
    sessionStorage.setItem(REDIRECT_KEY, redirectPath)
  }

  const params = new URLSearchParams({
    client_id: CLIENT_ID,
    redirect_uri: getRedirectUri(),
    response_type: 'code',
    scope: SCOPES,
    code_challenge: challenge,
    code_challenge_method: 'S256'
  })

  window.location.href = `${VITE_SSO_URL}/connect/authorize?${params.toString()}`
}

/** 用授权码换 token，返回 Token 数据（不直接写 localStorage） */
export async function exchangeCodeForToken(code: string): Promise<{ accessToken: string; refreshToken?: string }> {
  const verifier = sessionStorage.getItem(CODE_VERIFIER_KEY)
  if (!verifier) {
    throw new Error('缺少 code_verifier，请重新登录')
  }

  const params = new URLSearchParams({
    grant_type: 'authorization_code',
    code,
    redirect_uri: getRedirectUri(),
    client_id: CLIENT_ID,
    code_verifier: verifier
  })

  const response = await fetch(`${VITE_SSO_URL}/connect/token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: params.toString()
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.error_description || 'Token 交换失败')
  }

  const data = await response.json()
  sessionStorage.removeItem(CODE_VERIFIER_KEY)

  return {
    accessToken: data.access_token,
    refreshToken: data.refresh_token
  }
}

/** 使用 Refresh Token 刷新 Access Token，返回新 Token 数据（不直接写 localStorage） */
export async function refreshAccessToken(currentRefreshToken: string): Promise<{ accessToken: string; refreshToken?: string } | null> {
  const params = new URLSearchParams({
    grant_type: 'refresh_token',
    refresh_token: currentRefreshToken,
    client_id: CLIENT_ID
  })

  const response = await fetch(`${VITE_SSO_URL}/connect/token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: params.toString()
  })

  if (!response.ok) {
    return null
  }

  const data = await response.json()
  return {
    accessToken: data.access_token,
    refreshToken: data.refresh_token
  }
}

/** 跳转到 SSO 登出页面 */
export function logoutOAuth(): void {
  window.location.href = `${VITE_SSO_URL}/connect/logout?post_logout_redirect_uri=${encodeURIComponent(window.location.origin)}`
}

/** 获取登录前保存的跳转路径 */
export function getSavedRedirect(): string | null {
  const path = sessionStorage.getItem(REDIRECT_KEY)
  sessionStorage.removeItem(REDIRECT_KEY)
  return path
}

/** 获取回调地址 */
function getRedirectUri(): string {
  return `${window.location.origin}/callback`
}
