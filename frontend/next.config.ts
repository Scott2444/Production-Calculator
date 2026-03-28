import type { NextConfig } from "next";

const isProd = process.env.NODE_ENV === "production";

const nextConfig: NextConfig = {
    ...(isProd && { output: "export" }),
    ...(!isProd && {
        async rewrites() {
            return [
                {
                    source: "/:path*",
                    destination: "/",
                },
            ];
        },
    }),
    trailingSlash: true,
    images: {
        unoptimized: true,
    },
};

export default nextConfig;
