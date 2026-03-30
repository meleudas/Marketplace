export interface CurrentUser {
  id?: string | number;
  email: string;
  userName?: string;
  phoneNumber?: string | null;
  roles?: string[];
  [key: string]: unknown;
}

