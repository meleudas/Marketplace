const ACCESS_TOKEN_KEY = "accessToken";

const canUseStorage = () => typeof window !== "undefined";

export const getAccessToken = (): string | null => {
  if (!canUseStorage()) {
    console.warn("[AUTH][STORAGE] localStorage is unavailable on server side.");
    return null;
  }

  const token = window.localStorage.getItem(ACCESS_TOKEN_KEY);
  if (token) {
    console.log("[AUTH][STORAGE] Access token loaded from localStorage.", {
      key: ACCESS_TOKEN_KEY,
      tokenPreview: `${token.slice(0, 12)}...`,
    });
  } else {
    console.warn("[AUTH][STORAGE] Access token is missing in localStorage.", {
      key: ACCESS_TOKEN_KEY,
    });
  }

  return token;
};

export const setAccessToken = (token: string): void => {
  if (!canUseStorage()) {
    console.warn("[AUTH][STORAGE] Tried to save access token on server side.");
    return;
  }

  window.localStorage.setItem(ACCESS_TOKEN_KEY, token);
  console.log("[AUTH][STORAGE] Access token saved to localStorage.", {
    key: ACCESS_TOKEN_KEY,
    tokenPreview: `${token.slice(0, 12)}...`,
  });
};

export const clearAccessToken = (): void => {
  if (!canUseStorage()) {
    console.warn("[AUTH][STORAGE] Tried to clear token on server side.");
    return;
  }

  window.localStorage.removeItem(ACCESS_TOKEN_KEY);
  console.log("[AUTH][STORAGE] Access token removed from localStorage.", {
    key: ACCESS_TOKEN_KEY,
  });
};

