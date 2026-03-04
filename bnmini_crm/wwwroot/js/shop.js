let cart = {};
let items = [];
let allItems = [];
let appUserId = 0;
let venueId = 0;
let baseUrl = '';
let deliveryPhone = '';
let savedAddresses = [];
let categories = [];
let currentCategoryId = 0;

function initShop(itemsJson, firstName, userId, vId, phone, addressesJson, categoriesJson, defaultCategoryId) {
    allItems = JSON.parse(itemsJson);
    items = [...allItems];
    appUserId = userId;
    venueId = vId;
    baseUrl = window.location.origin;
    deliveryPhone = phone;
    savedAddresses = JSON.parse(addressesJson);
    categories = JSON.parse(categoriesJson);
    currentCategoryId = defaultCategoryId;

    console.log('initShop:', { userId, vId, phone, addresses: savedAddresses, categories });

    if (firstName) document.getElementById('user-name').textContent = '👤 ' + firstName;

    document.getElementById('loading').style.display = 'none';
    document.getElementById('items-list').style.display = 'grid';

    renderItems();
    updateFab();
}

function renderItems() {
    const list = document.getElementById('items-list');
    list.innerHTML = '';

    const filtered = currentCategoryId === 0
        ? allItems
        : allItems.filter(i => i.CategoryId === currentCategoryId);

    items = filtered;

    filtered.forEach(item => {
        const card = document.createElement('div');
        card.className = 'tg-card';
        card.id = 'card-' + item.Id;
        card.innerHTML = `
            <div class="tg-card-image-wrap">
                ${item.ImageUrl
                ? `<img class="tg-card-image" src="${item.ImageUrl}" alt="${item.Name}" />`
                : `<div class="tg-card-image-placeholder">📦</div>`}
            </div>
            <div class="tg-card-body">
                <div class="tg-card-name">${item.Name}</div>
                <div class="tg-card-desc">${item.Description || ''}</div>
                <div class="tg-card-footer">
                    <span class="tg-card-price">${item.Price.toFixed(2)} ₽</span>
                    <div id="btn-area-${item.Id}">
                        ${cart[item.Id]
                ? `<div class="item-qty-control">
                                <button class="item-qty-btn" onclick="decrease(${item.Id})">−</button>
                                <span class="item-qty-val">${cart[item.Id]}</span>
                                <button class="item-qty-btn" onclick="increase(${item.Id})">+</button>
                               </div>`
                : `<button class="tg-btn" onclick="addToCart(${item.Id})">+ В корзину</button>`}
                    </div>
                </div>
            </div>`;
        list.appendChild(card);
    });
}

// Категории
function openCategories() {
    const body = document.getElementById('cat-body');
    body.innerHTML = '';

    // "Все" категория
    const allRow = document.createElement('div');
    allRow.className = 'cat-row';
    allRow.onclick = () => selectCategory(0, 'Все меню');
    allRow.innerHTML = `
        ${currentCategoryId === 0 ? '<span class="cat-row-check">✓</span>' : ''}`;
    body.appendChild(allRow);

    categories.forEach(cat => {
        const row = document.createElement('div');
        row.className = 'cat-row';
        row.onclick = () => selectCategory(cat.Id, cat.Name);
        row.innerHTML = `
            <span class="cat-row-name">${cat.Name}</span>
            ${currentCategoryId === cat.Id ? '<span class="cat-row-check">✓</span>' : ''}`;
        body.appendChild(row);
    });

    document.getElementById('cat-overlay').style.display = 'block';
    document.getElementById('cat-sheet').style.display = 'flex';
}

function closeCategories() {
    document.getElementById('cat-overlay').style.display = 'none';
    document.getElementById('cat-sheet').style.display = 'none';
}

function selectCategory(id, name) {
    currentCategoryId = id;
    closeCategories();
    renderItems();
}

// Корзина
function addToCart(id) { cart[id] = 1; updateBtnArea(id); updateFab(); }
function increase(id) { cart[id]++; updateBtnArea(id); updateFab(); }
function decrease(id) {
    if (cart[id] > 1) cart[id]--;
    else delete cart[id];
    updateBtnArea(id);
    updateFab();
}

function updateBtnArea(id) {
    const area = document.getElementById('btn-area-' + id);
    if (!area) return;
    if (cart[id]) {
        area.innerHTML = `
            <div class="item-qty-control">
                <button class="item-qty-btn" onclick="decrease(${id})">−</button>
                <span class="item-qty-val">${cart[id]}</span>
                <button class="item-qty-btn" onclick="increase(${id})">+</button>
            </div>`;
    } else {
        area.innerHTML = `<button class="tg-btn" onclick="addToCart(${id})">+ В корзину</button>`;
    }
}

function updateFab() {
    const fabGroup = document.getElementById('fab-group');
    const fabSolo = document.getElementById('fab-cat-solo');
    const count = Object.values(cart).reduce((a, b) => a + b, 0);
    const totalPrice = Object.entries(cart).reduce((sum, [id, qty]) => {
        const item = allItems.find(i => i.Id == id);
        return sum + (item ? item.Price * qty : 0);
    }, 0);

    if (count > 0) {
        // Показать группу (категории + корзина)
        fabGroup.style.display = 'flex';
        fabSolo.style.display = 'none';
        document.getElementById('fab-badge').textContent = count;
        document.getElementById('header-total').textContent = totalPrice.toFixed(2) + ' ₽';
    } else {
        // Только кнопка категорий
        fabGroup.style.display = 'none';
        fabSolo.style.display = 'flex';
        document.getElementById('header-total').textContent = '';
    }
}

function openCart() {
    renderCartBody();
    document.getElementById('overlay').style.display = 'block';
    document.getElementById('sheet').style.display = 'flex';
}

function closeCart() {
    document.getElementById('overlay').style.display = 'none';
    document.getElementById('sheet').style.display = 'none';
}

function renderCartBody() {
    const body = document.getElementById('cart-body');
    body.innerHTML = '';
    let total = 0;
    Object.entries(cart).forEach(([id, qty]) => {
        const item = allItems.find(i => i.Id == id);
        if (!item) return;
        const subtotal = item.Price * qty;
        total += subtotal;
        body.innerHTML += `
            <div class="cart-row">
                <div class="cart-info">
                    <div class="cart-name">${item.Name}</div>
                    <div class="cart-price">${subtotal.toFixed(2)} ₽</div>
                </div>
                <div class="cart-controls">
                    <button class="qty-btn" onclick="cartDecrease(${id})">−</button>
                    <span class="qty-val">${qty}</span>
                    <button class="qty-btn" onclick="cartIncrease(${id})">+</button>
                    <button class="del-btn" onclick="removeFromCart(${id})">🗑</button>
                </div>
            </div>`;
    });
    document.getElementById('cart-total').textContent = total.toFixed(2) + ' ₽';
}

function cartIncrease(id) { cart[id]++; updateBtnArea(id); updateFab(); renderCartBody(); }
function cartDecrease(id) {
    if (cart[id] > 1) cart[id]--;
    else delete cart[id];
    updateBtnArea(id); updateFab(); renderCartBody();
    if (Object.keys(cart).length === 0) closeCart();
}
function removeFromCart(id) {
    delete cart[id];
    updateBtnArea(id); updateFab(); renderCartBody();
    if (Object.keys(cart).length === 0) closeCart();
}
function clearCart() {
    Object.keys(cart).forEach(id => { delete cart[id]; updateBtnArea(id); });
    cart = {}; updateFab(); closeCart();
}

// Доставка
async function submitOrder() {
    if (appUserId === 0) { showToast('⚠️ Сначала зарегистрируйтесь через бот'); return; }
    openDeliveryModal();
}

function openDeliveryModal() {
    const modal = document.getElementById('delivery-modal');
    document.getElementById('delivery-phone').value = deliveryPhone;

    const addressList = document.getElementById('address-list');
    const addressInput = document.getElementById('delivery-address');
    addressList.innerHTML = '';

    savedAddresses.forEach((addr, index) => {
        const chip = document.createElement('div');
        chip.className = 'address-chip';
        chip.dataset.addr = addr;
        chip.innerHTML = `
            <span class="chip-text" onclick="selectAddress('${addr.replace(/'/g, "\\'")}')">${addr.length > 50 ? addr.substring(0, 50) + '...' : addr}</span>
            <button class="chip-delete" onclick="deleteAddress(${index}, '${addr.replace(/'/g, "\\'")}', this)">✕</button>`;
        addressList.appendChild(chip);
    });

    document.getElementById('sheet-actions').style.display = 'none';
    modal.style.display = 'flex';
}

function closeDeliveryModal() {
    document.getElementById('delivery-modal').style.display = 'none';
    document.getElementById('sheet-actions').style.display = 'flex';
}

function selectAddress(addr) {
    document.getElementById('delivery-address').value = addr;
    document.querySelectorAll('.address-chip').forEach(c => c.classList.remove('active'));
    event.target.closest('.address-chip').classList.add('active');
}

async function deleteAddress(index, addr, btn) {
    const chip = btn.closest('.address-chip');
    chip.style.opacity = '0.4';
    chip.style.pointerEvents = 'none';
    try {
        const resp = await fetch(`${baseUrl}/api/orders/delete-address`, {
            method: 'DELETE',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ appUserId, address: addr })
        });
        if (resp.ok) {
            chip.remove();
            savedAddresses.splice(index, 1);
            if (document.getElementById('delivery-address').value === addr)
                document.getElementById('delivery-address').value = '';
        } else {
            chip.style.opacity = '1'; chip.style.pointerEvents = 'auto';
            showToast('❌ Ошибка удаления');
        }
    } catch {
        chip.style.opacity = '1'; chip.style.pointerEvents = 'auto';
        showToast('❌ Нет соединения');
    }
}

async function confirmOrder() {
    const phone = document.getElementById('delivery-phone').value.trim();
    const address = document.getElementById('delivery-address').value.trim();
    if (!phone) { showToast('⚠️ Введите номер телефона'); return; }
    if (!address) { showToast('⚠️ Введите адрес доставки'); return; }

    const orderItems = Object.entries(cart).map(([id, qty]) => ({ itemId: parseInt(id), quantity: qty }));

    try {
        const resp = await fetch(`${baseUrl}/api/orders`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ venueId, appUserId, phone, address, orderItems })
        });
        if (resp.ok) {
            // Сохранить новый адрес локально
            if (!savedAddresses.includes(address)) savedAddresses.push(address);
            deliveryPhone = phone;
            closeDeliveryModal();
            closeCart();
            clearCart();
            showToast('✅ Заказ отправлен!');
        } else {
            showToast('❌ Ошибка при отправке');
        }
    } catch {
        showToast('❌ Нет соединения');
    }
}

function showToast(msg) {
    const t = document.getElementById('toast');
    t.textContent = msg;
    t.classList.add('tg-toast-visible');
    setTimeout(() => t.classList.remove('tg-toast-visible'), 2500);
}