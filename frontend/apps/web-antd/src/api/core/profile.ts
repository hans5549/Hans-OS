import { requestClient } from '#/api/request';

export interface ProfileHeaderResponse {
  avatar: string;
  realName: string;
  roles: string[];
  userId: string;
  username: string;
}

export interface ProfileBasicResponse {
  email: string;
  introduction: string;
  phoneNumber: string;
  realName: string;
  roles: string[];
  username: string;
}

export interface ProfileNotificationsResponse {
  notifyAccountPassword: boolean;
  notifySystemMessage: boolean;
  notifyTodoTask: boolean;
}

export interface ProfileSecurityResponse {
  hasEmail: boolean;
  hasPassword: boolean;
  hasPhoneNumber: boolean;
  twoFactorEnabled: boolean;
}

export interface ProfileResponse {
  basic: ProfileBasicResponse;
  header: ProfileHeaderResponse;
  notifications: ProfileNotificationsResponse;
  security: ProfileSecurityResponse;
}

export interface UpdateProfileBasicRequest {
  email: string;
  introduction: string;
  phoneNumber: string;
  realName: string;
}

export interface UpdateProfileNotificationsRequest {
  notifyAccountPassword: boolean;
  notifySystemMessage: boolean;
  notifyTodoTask: boolean;
}

export interface ChangePasswordRequest {
  confirmPassword: string;
  newPassword: string;
  oldPassword: string;
}

export async function changeProfilePasswordApi(data: ChangePasswordRequest) {
  return requestClient.post('/profile/change-password', data);
}

export async function getProfileApi() {
  return requestClient.get<ProfileResponse>('/profile');
}

export async function updateProfileBasicApi(data: UpdateProfileBasicRequest) {
  return requestClient.put<ProfileResponse>('/profile/basic', data);
}

export async function updateProfileNotificationsApi(
  data: UpdateProfileNotificationsRequest,
) {
  return requestClient.put<ProfileNotificationsResponse>(
    '/profile/notifications',
    data,
  );
}
