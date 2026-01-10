export default interface NavBarProps {
    loggedIn: boolean,
    accountLogoUrl?: string,
    currentPage?: 'home' | 'explore' | 'projects' | 'settings',
}