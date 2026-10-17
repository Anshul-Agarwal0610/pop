/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "standalone",
  typescript: {
    ignoreBuildErrors: true,
  },
  images: {
    unoptimized: true,
  },
  async rewrites() {
    const apiProxyUrl = process.env.DEV_API_PROXY_URL
    return process.env.NODE_ENV === "development" && apiProxyUrl
      ? [{ source: "/api/:path*", destination: `${apiProxyUrl}/api/:path*` }]
      : []
  },
}

export default nextConfig
