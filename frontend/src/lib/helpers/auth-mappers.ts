import type { CurrentUser } from "@/types/user";

const isRecord = (value: unknown): value is Record<string, unknown> => {
  return typeof value === "object" && value !== null;
};

const pickString = (...values: unknown[]): string | null => {
  const found = values.find((value) => typeof value === "string" && value.trim().length > 0);
  return typeof found === "string" ? found : null;
};

export const extractAccessToken = (raw: unknown): string | null => {
  console.log("[AUTH][MAPPER] Trying to extract access token from response.", { raw });

  if (!isRecord(raw)) {
    console.warn("[AUTH][MAPPER] Login response is not an object. Token was not found.");
    return null;
  }

  const directToken = pickString(raw.accessToken, raw.token, raw.jwt, raw.access_token);
  if (directToken) {
    console.log("[AUTH][MAPPER] Access token extracted from top-level response field.");
    return directToken;
  }

  const data = isRecord(raw.data) ? raw.data : null;
  const nestedToken = pickString(
    data?.accessToken,
    data?.token,
    data?.jwt,
    data?.access_token,
    isRecord(data?.tokens) ? data?.tokens.accessToken : null,
    isRecord(data?.tokens) ? data?.tokens.token : null,
  );

  if (nestedToken) {
    console.log("[AUTH][MAPPER] Access token extracted from nested response field.");
    return nestedToken;
  }

  console.warn("[AUTH][MAPPER] Access token was not found. Adjust extractAccessToken mapper.", {
    raw,
  });
  return null;
};

export const mapCurrentUser = (raw: unknown): CurrentUser | null => {
  console.log("[PROFILE][MAPPER] Trying to map current user from response.", { raw });

  const source = isRecord(raw)
    ? isRecord(raw.data)
      ? (raw.data as Record<string, unknown>)
      : (raw as Record<string, unknown>)
    : null;

  if (!source) {
    console.warn("[PROFILE][MAPPER] User response is not an object. Mapper returned null.");
    return null;
  }

  const email = pickString(source.email, source.mail, source.usernameEmail);
  if (!email) {
    console.warn("[PROFILE][MAPPER] Could not find user email in response.", { source });
    return null;
  }

  const mappedUser: CurrentUser = {
    id: source.id as string | number | undefined,
    email,
    userName: pickString(source.userName, source.username, source.name) ?? undefined,
    phoneNumber:
      source.phoneNumber === null
        ? null
        : pickString(source.phoneNumber, source.phone) ?? undefined,
    roles: Array.isArray(source.roles)
      ? source.roles.filter((role): role is string => typeof role === "string")
      : undefined,
    ...source,
  };

  console.log("[PROFILE][MAPPER] Current user mapped successfully.", { mappedUser });
  return mappedUser;
};

