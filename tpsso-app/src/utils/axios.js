import axios from 'axios'
import { userManager } from '@/auth/oidcConfig'

const apiClient = axios.create({
  baseURL: 'https://your-api-domain.com'
})

apiClient.interceptors.request.use(async config => {
  const user = await userManager.getUser()
  if (user?.access_token) {
    config.headers.Authorization = `Bearer ${user.access_token}`
  }
  return config
})

export default apiClient