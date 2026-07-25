import { redirect } from "next/navigation";

export const metadata = {
  title: "Вхід | Book Top",
};

export default function AuthPage() {
  redirect("/auth/login");
}
