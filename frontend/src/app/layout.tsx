import type { Metadata } from "next";
import { Rubik  } from "next/font/google";
import "./globals.css";

const rubik = Rubik({
  subsets: ["latin"],
  weight: ['300', '400', '500', '600', '700'],
  variable: "--font-rubik",
});

export const metadata: Metadata = {
  title: "Production Calculator",
  description: "Logistics planning tool for automation games",
  icons: {
    icon: [
      { url: '/Small_Logo.svg', type: 'image/svg+xml' }
    ],
    shortcut: ['/Small_Logo.svg'],
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body
        className={`${rubik.variable} antialiased`}
      >
        {children}
      </body>
    </html>
  );
}
