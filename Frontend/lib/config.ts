const productionApiUrl = "https://example-app-service6.azurewebsites.net"

export const API_BASE_URL = process.env.NODE_ENV === "production"
  ? productionApiUrl
  : process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5177"
