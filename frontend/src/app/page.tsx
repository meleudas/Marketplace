import { redirect } from "next/navigation";

// TEMP: фронтенд тимчасово веде на нову сторінку дизайну (/design).
// Старий головний екран лишається в @/features/storefront/screens/HomeScreen.
// Щоб повернути старий вигляд — видали цей redirect і поверни <HomeScreen />.
export default function Page() {
  redirect("/design");
}
