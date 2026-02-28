import SlugRouter from "./SlugRouter";

export async function generateStaticParams() {
    return [{ slug: ["_"] }];
}

export default function Page() {
    return <SlugRouter />;
}
