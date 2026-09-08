import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Carpool | Development",
  description: "A carpooling learning project for daily commuters in Egypt.",
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
