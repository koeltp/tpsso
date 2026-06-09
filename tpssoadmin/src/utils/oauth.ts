/**
 * OAuth 2.0 + PKCE 工具
 * tpssoadmin 作为标准 OAuth 客户端接入 SSO
 */

const CLIENT_ID = 'tpsso_admin_client'
const SCOPES = 'openid profile email roles'
const TOKEN_KEY = 'admin_access_token'
const REFRESH_TOKEN_KEY = 'admin_refresh_token'
const CODE_VERIFIER_KEY = 'admin_code_verifier'
const REDIRECT_KEY = 'admin_redirect'
const ROLES_KEY = 'admin_roles'

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

  // 跳转到 SSO 授权端点
  window.location.href = `/connect/authorize?${params.toString()}`
}

/** 用授权码换 token */
export async function exchangeCodeForToken(code: string): Promise<void> {
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

  const response = await fetch('/connect/token', {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: params.toString()
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.error_description || 'Token 交换失败')
  }

  const data = await response.json()
  localStorage.setItem(TOKEN_KEY, data.access_token)
  if (data.refresh_token) {
    localStorage.setItem(REFRESH_TOKEN_KEY, data.refresh_token)
  }

  // 清理临时数据
  sessionStorage.removeItem(CODE_VERIFIER_KEY)
}

/** 获取 access token */
export function getAccessToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

/** 缓存用户角色（通过 /api/account/me 接口获取后缓存） */
export function setCachedRoles(roles: string[]): void {
  localStorage.setItem(ROLES_KEY, JSON.stringify(roles))
}

/** 获取缓存的角色 */
export function getCachedRoles(): string[] {
  const raw = localStorage.getItem(ROLES_KEY)
  if (!raw) return []
  try {
    return JSON.parse(raw)
  } catch {
    return []
  }
}

/** 检查当前用户是否拥有指定角色 */
export function hasRole(role: string): boolean {
  return getCachedRoles().includes(role)
}

/** 是否已登录 */
export function isAuthenticated(): boolean {
  return !!getAccessToken()
}

/** 退出登录 */
export function logoutOAuth(): void {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(REFRESH_TOKEN_KEY)
  localStorage.removeItem(ROLES_KEY)
  sessionStorage.removeItem(CODE_VERIFIER_KEY)
  // 跳转到 SSO 登出端点（post_logout_redirect_uri 必须与种子数据中注册的一致）
  window.location.href = '/connect/logout?post_logout_redirect_uri=' + encodeURIComponent(window.location.origin)
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
