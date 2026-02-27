import type { NextConfig } from "next";

const nextConfig: NextConfig = {
    async rewrites() {
        if (process.env.NODE_ENV === "development") {
            return [
                {
                    source: "/api/:path*",
                    destination: "http://localhost:5076/api/:path*", // Proxy to backend
                },
            ];
        }
        return [];
    },
};

export default nextConfig;
