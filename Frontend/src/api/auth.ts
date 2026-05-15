import api from './client'
import type {
  LoginRequest, AuthResponse, ChangePasswordRequest, ForcedChangePasswordRequest,
  ForgotPasswordRequest, ResetPasswordRequest, ApiResponse
} from '@/types'

export const authApi = {
  login: (data: LoginRequest) =>
    api.post<ApiResponse<AuthResponse>>('/auth/login', data),

  refresh: (refreshToken: string) =>
    api.post<ApiResponse<AuthResponse>>('/auth/refresh', { refreshToken }),

  logout: (refreshToken: string) =>
    api.post<ApiResponse<null>>('/auth/logout', { refreshToken }),

  changePassword: (data: ChangePasswordRequest) =>
    api.post<ApiResponse<null>>('/auth/change-password', data),

  forcedChangePassword: (data: ForcedChangePasswordRequest) =>
    api.post<ApiResponse<null>>('/auth/forced-change-password', data),

  forgotPassword: (data: ForgotPasswordRequest) =>
    api.post<ApiResponse<null>>('/auth/forgot-password', data),

  resetPassword: (data: ResetPasswordRequest) =>
    api.post<ApiResponse<null>>('/auth/reset-password', data),
}
