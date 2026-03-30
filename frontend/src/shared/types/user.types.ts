export interface CurrentUser {
  id: string;
  firstName: string;
  lastName: string;
  role: string;
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


