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
let currentRequestId = '';

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

    const defaultCat = categories.find(c => c.IsDefault);
    const isDefault = currentCategoryId === 0 || (defaultCat && currentCategoryId === defaultCat.Id);
    const filtered = isDefault
        ? allItems
        : allItems.filter(i => i.CategoryId === currentCategoryId);

    items = filtered;

    filtered.forEach(item => {
        const card = document.createElement('div');
        card.className = 'tg-card';
        card.id = 'card-' + item.Id;
        const firstImage = item.Images && item.Images.length > 0 ? item.Images[0].Url : item.ImageUrl;
        card.innerHTML = `
    <div class="tg-card-image-wrap" onclick="openItemDetail(${item.Id})">
        ${firstImage
                ? `<img class="tg-card-image" src="${firstImage}" alt="${item.Name}" />`
                : `<div class="tg-card-image-placeholder">📦</div>`}
    </div>
    <div class="tg-card-body">
        <div class="tg-card-name" onclick="openItemDetail(${item.Id})">${item.Name}</div>
        <div class="tg-card-footer">
            <span class="tg-card-price">${item.Price.toFixed(2)} man</span>
            <div id="btn-area-${item.Id}">
                ${cart[item.Id]
                ? `<div class="item-qty-control">
                    <button class="item-qty-btn" onclick="decrease(${item.Id})">−</button>
                    <span class="item-qty-val">${cart[item.Id]}</span>
                    <button class="item-qty-btn" onclick="increase(${item.Id})">+</button>
                   </div>`
                : `<button class="tg-btn" onclick="addToCart(${item.Id})">+ Sebede goş</button>`}
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

    const allRow = document.createElement('div');
    allRow.className = 'cat-row';
    allRow.onclick = () => selectCategory(0, 'Ähli tagamlar');
    allRow.innerHTML = `
    <span class="cat-row-name">Ähli tagamlar</span>
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
        area.innerHTML = `<button class="tg-btn" onclick="addToCart(${id})">+ Sebede goş</button>`;
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
        fabGroup.style.display = 'flex';
        fabSolo.style.display = 'none';
        document.getElementById('fab-badge').textContent = count;
        document.getElementById('header-total').textContent = totalPrice.toFixed(2) + ' man';
    } else {
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
                    <div class="cart-price">${subtotal.toFixed(2)} man</div>
                </div>
                <div class="cart-controls">
                    <button class="qty-btn" onclick="cartDecrease(${id})">−</button>
                    <span class="qty-val">${qty}</span>
                    <button class="qty-btn" onclick="cartIncrease(${id})">+</button>
                    <button class="del-btn" onclick="removeFromCart(${id})">🗑</button>
                </div>
            </div>`;
    });
    document.getElementById('cart-total').textContent = total.toFixed(2) + ' man';
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
    if (appUserId === 0) { showToast('⚠️ çat bota girip yazyň'); return; }
    openDeliveryModal();
}

function openDeliveryModal() {
    currentRequestId = crypto.randomUUID();

    const modal = document.getElementById('delivery-modal');
    document.getElementById('delivery-phone').value = deliveryPhone;

    const addressList = document.getElementById('address-list');
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

    const now = new Date();
    const hh = String(now.getHours()).padStart(2, '0');
    const mm = String(now.getMinutes()).padStart(2, '0');
    document.getElementById('delivery-time').value = `${hh}:${mm}`;
    document.getElementById('time-now').checked = true;

    // Сбросить кнопку
    const btn = document.getElementById('confirm-order-btn');
    btn.disabled = false;
    btn.textContent = 'Tassyklmak';

    document.getElementById('sheet-actions').style.display = 'none';
    modal.style.display = 'flex';
}

function changeHour(delta) {
    const input = document.getElementById('delivery-time');
    const [hh, mm] = input.value.split(':').map(Number);
    let newHour = (hh + delta + 24) % 24;
    input.value = `${String(newHour).padStart(2, '0')}:${String(mm).padStart(2, '0')}`;
    document.getElementById('time-custom').checked = true;
}

function setPresetTime(value) {
    const input = document.getElementById('delivery-time');
    if (value === 'now') {
        const now = new Date();
        input.value = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;
    } else {
        input.value = value;
    }
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
            showToast('❌ ýalňyşlyk ýüze çykdy');
        }
    } catch {
        chip.style.opacity = '1'; chip.style.pointerEvents = 'auto';
        showToast('❌ Aragatnaşyk ýok');
    }
}

async function confirmOrder() {

    const btn = document.getElementById('confirm-order-btn');
    if (btn.disabled) return;
    btn.disabled = true;
    btn.textContent = '⏳ Ugratmak...';

    const phone = document.getElementById('delivery-phone').value.trim();
    const address = document.getElementById('delivery-address').value.trim();
    const deliveryTime = document.getElementById('delivery-time').value;


    if (!phone) {
        btn.disabled = false;
        btn.textContent = 'Tassyklamak';
        showToast('⚠️ Telefon belgiňizi giriziň');
        return;
    }
    if (!address) {
        btn.disabled = false;
        btn.textContent = 'Tassyklamak';
        showToast('⚠️ Adres giriziň');
        return;
    }

    const orderItems = Object.entries(cart).map(([id, qty]) => ({
        itemId: parseInt(id), quantity: qty
    }));

    try {



        const resp = await fetch(`${baseUrl}/api/orders`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                venueId, appUserId, phone, address, deliveryTime,
                requestId: currentRequestId,
                orderItems
            })
        });
        if (resp.ok) {
            if (!savedAddresses.includes(address)) savedAddresses.push(address);
            deliveryPhone = phone;
            closeDeliveryModal();
            closeCart();
            clearCart();
            showToast('✅ Sargydyňyz ugradyldy!');
        } else {
            showToast('❌ Sargyt ugratmakda ýalňyşlyk ýüze çykdy');
            btn.disabled = false;
            btn.textContent = 'Tassyklamak';
        }
    } catch {
        showToast('❌ Aragatnaşyk ýok');
        btn.disabled = false;
        btn.textContent = 'Tassyklamak';
    }
}

function showToast(msg) {
    const t = document.getElementById('toast');
    t.textContent = msg;
    t.classList.add('tg-toast-visible');
    setTimeout(() => t.classList.remove('tg-toast-visible'), 2500);
}

async function uploadItemImage() {
    const input = document.getElementById('file-input');
    if (!input?.files?.length) return null;

    const file = input.files[0];
    const formData = new FormData();
    formData.append('file', file);

    const resp = await fetch('/api/upload/item-image', {
        method: 'POST',
        body: formData
    });

    if (!resp.ok) return null;
    const data = await resp.json();
    return data.url;
}
let _lastUploadedUrl = null;
function openFilePicker() {
    const input = document.getElementById('file-input');
    input.value = '';
    input.onchange = async () => {
        if (!input.files?.length) return;
        const file = input.files[0];
        const formData = new FormData();
        formData.append('file', file);
        try {
            const resp = await fetch('/api/upload/item-image', {
                method: 'POST',
                body: formData
            });
            if (resp.ok) {
                const data = await resp.json();
                _lastUploadedUrl = data.url;
            }
        } catch { }
    };
    input.click();
}

function getLastUploadedUrl() {
    const url = _lastUploadedUrl;
    _lastUploadedUrl = null;
    return url;
}

function openItemDetail(id) {
    const item = allItems.find(i => i.Id === id);
    if (!item) return;

    const images = item.Images && item.Images.length > 0
        ? item.Images.map(img => `<img src="${img.Url}" alt="${item.Name}" />`)
        : item.ImageUrl
            ? [`<img src="${item.ImageUrl}" alt="${item.Name}" />`]
            : [`<div class="no-img">📦</div>`];

    document.getElementById('item-detail-images').innerHTML = images.join('');
    document.getElementById('item-detail-name').textContent = item.Name;
    document.getElementById('item-detail-price').textContent = item.Price.toFixed(2) + ' man';
    document.getElementById('item-detail-desc').textContent = item.Description || '';

    const btnArea = document.getElementById('item-detail-btn-area');
    if (cart[id]) {
        btnArea.innerHTML = `
            <div class="item-qty-control">
                <button class="item-qty-btn" onclick="decreaseDetail(${id})">−</button>
                <span class="item-qty-val">${cart[id]}</span>
                <button class="item-qty-btn" onclick="increaseDetail(${id})">+</button>
            </div>`;
    } else {
        btnArea.innerHTML = `<button class="tg-btn" onclick="addToCartDetail(${id})">+ Sebede goş</button>`;
    }

    document.getElementById('item-detail-overlay').style.display = 'block';
    document.getElementById('item-detail-modal').classList.add('open');
    document.body.style.overflow = 'hidden';
}

function closeItemDetail() {
    document.getElementById('item-detail-overlay').style.display = 'none';
    document.getElementById('item-detail-modal').classList.remove('open');
    document.body.style.overflow = '';
}

function addToCartDetail(id) {
    addToCart(id);
    openItemDetail(id);
}

function increaseDetail(id) {
    increase(id);
    openItemDetail(id);
}

function decreaseDetail(id) {
    if (cart[id] > 1) {
        decrease(id);
        openItemDetail(id);
    } else {
        decrease(id);
        closeItemDetail();
    }
}
