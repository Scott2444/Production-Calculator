import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: "http://localhost:5076/api/:path*", // Proxy to backend
      },
    ];
  },
  // ...other config options
};

export default nextConfig;
