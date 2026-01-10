import NavBar from '@/components/NavBar';

export default function Home() {
    return (
        <>
            <NavBar loggedIn={true} />
            <div>Home Page</div>
        </>
    );

}