function getTelegramUserId() {
    try {
        const tg = window.Telegram.WebApp;
        tg.ready();
        tg.expand();
        const user = tg.initDataUnsafe?.user;
        return user ? user.id : 0;
    } catch {
        return 0;
    }
}

function getBaseUrl() {
    return window.location.origin;
}

