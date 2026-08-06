export function getSafeRedirect(value: string | null) {
  return value?.startsWith("/") && !value.startsWith("//") ? value : "/"
}
