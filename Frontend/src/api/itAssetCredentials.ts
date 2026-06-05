import api from './client'
import type {
  ApiResponse,
  ItAssetCredentialDto, ItAssetCredentialSecret,
  CreateItAssetCredentialRequest, UpdateItAssetCredentialRequest,
} from '@/types'

const base = (assetId: string) => `/ti/assets/${assetId}/credentials`

export const itAssetCredentialsApi = {
  list: (assetId: string) =>
    api.get<ApiResponse<ItAssetCredentialDto[]>>(base(assetId)),

  // Revela el secreto descifrado (requiere permiso; el backend lo audita).
  reveal: (assetId: string, credentialId: string) =>
    api.get<ApiResponse<ItAssetCredentialSecret>>(`${base(assetId)}/${credentialId}/secret`),

  create: (assetId: string, data: CreateItAssetCredentialRequest) =>
    api.post<ApiResponse<ItAssetCredentialDto>>(base(assetId), data),

  update: (assetId: string, credentialId: string, data: UpdateItAssetCredentialRequest) =>
    api.put<ApiResponse<ItAssetCredentialDto>>(`${base(assetId)}/${credentialId}`, data),

  remove: (assetId: string, credentialId: string) =>
    api.delete<ApiResponse<null>>(`${base(assetId)}/${credentialId}`),
}
