import ClientRouterApp from "@/router/ClientRouterApp";

export async function generateStaticParams() {
    return [{ slug: ["_"] }];
}

export default function Page() {
    return <ClientRouterApp />;
}
