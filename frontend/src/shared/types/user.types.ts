export type UserRole = "buyer" | "seller" | "moderator" | "admin";

export interface UserDto {
  id: string;
  firstName: string;
  lastName: string;
  patronymic: string | null;
  email: string | null;
  phoneNumber: string | null;
  role: UserRole;
  birthday: string | null;
  avatar: string | null;
  isVerified: boolean;
  verificationDocument: string | null;
  lastLoginAt: string | null;
  createdAt: string;
  updatedAt: string;
  isDeleted: boolean;
  deletedAt: string | null;
}



