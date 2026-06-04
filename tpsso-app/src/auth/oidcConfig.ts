import { UserManager, WebStorageStateStore } from 'oidc-client-ts'

export const userManager = new UserManager({
  authority: 'https://localhost:7044',
  client_id: 'tpsso_spa_client',
  // SPA 是 public client，使用 PKCE 验证，无需 client_secret
  redirect_uri: 'http://localhost:3007/callback',
  response_type: 'code',
  scope: 'profile',
  userStore: new WebStorageStateStore({ store: window.localStorage }),
  automaticSilentRenew: true,
  silent_redirect_uri: 'http://localhost:3007/silent-renew.html'
})