(function () {
    const bellBtn = document.getElementById('notification-bell');
    const badge = document.getElementById('notification-badge');
    const list = document.getElementById('notification-list');

    if (!bellBtn) {
        return;
    }

    let unread = 0;

    function addNotification(message) {
        unread++;
        badge.textContent = unread;
        badge.classList.remove('d-none');

        const item = document.createElement('li');
        item.className = 'dropdown-item text-wrap small py-2 border-bottom';
        item.style.maxWidth = '300px';
        item.textContent = message;
        list.prepend(item);

        const empty = list.querySelector('.text-muted');
        if (empty) {
            empty.remove();
        }
    }

    bellBtn.addEventListener('click', function () {
        unread = 0;
        badge.textContent = '';
        badge.classList.add('d-none');
    });

    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/notifications')
        .withAutomaticReconnect()
        .build();

    connection.on('ReceiveNotification', function (message) {
        addNotification(message);
    });

    connection.on('TooManyConnections', function () {
        const item = document.createElement('li');
        item.className = 'dropdown-item text-danger small py-2';
        item.textContent = 'Too many active connections. Please try again later.';
        list.innerHTML = '';
        list.appendChild(item);
    });

    connection.start().catch(function (err) {
        console.error('SignalR connection error:', err);
    });
}());
