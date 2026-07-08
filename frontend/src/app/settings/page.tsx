import type { Metadata } from "next";
import { SettingsScreen } from "@/features/settings/screens/SettingsScreen";

export const metadata: Metadata = {
  title: "Налаштування профілю",
  description: "Налаштуйте параметри свого акаунта на Booktop: зміна пароля, двофакторна автентифікація, контакти.",
};

export default function Page() {
  return <SettingsScreen />;
}

